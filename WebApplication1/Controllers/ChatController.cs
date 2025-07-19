using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;
using WebApplication1.DTO;
using WebApplication1.Models;
using WebApplication1.Service.Interface;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController(
        IChatService _chatService,
        IConversationService _conversationService,
        ILogger<ChatController> _logger)
        : ControllerBase
    {
        /// <summary>
        /// Send a message to the AI chatbot
        /// </summary>
        [HttpPost("send-message")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User not authenticated");
                }

                if (string.IsNullOrEmpty(request.UserMessage) || string.IsNullOrEmpty(request.ModelName))
                {
                    return BadRequest("Message and model name are required");
                }

                // Get or create conversation
                var conversationResult = await _conversationService.GetOrCreateActiveConversationAsync(userId, IsPaidUser());
                if (!conversationResult.IsSuccess)
                {
                    return BadRequest(conversationResult.Message);
                }

                var conversationId = request.ConversationId ?? conversationResult.Data.ConversationId;

                // Send message
                var messageResult = await _chatService.SendMessageAsync(userId, conversationId, request.UserMessage, request.ModelName);
                if (!messageResult.IsSuccess)
                {
                    return BadRequest(messageResult.Message);
                }

                var response = new SendMessageResponse
                {
                    MessageId = messageResult.Data.MessageId,
                    UserMessage = request.UserMessage, // Use request data since it's the user input
                    AiResponse = messageResult.Data.Content, // Use Content property from new model
                    ModelUsed = messageResult.Data.ModelUsed ?? "",
                    MessageTimestamp = messageResult.Data.CreatedAt, // Use CreatedAt instead of MessageTimestamp
                    ConversationId = conversationId
                };

                return Ok(ServiceResult<SendMessageResponse>.Success(response));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get conversation history
        /// </summary>
        [HttpGet("conversation/{conversationId}/messages")]
        public async Task<IActionResult> GetConversationMessages(Guid conversationId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User not authenticated");
                }

                var messagesResult = await _chatService.GetConversationMessagesAsync(conversationId, userId);
                if (!messagesResult.IsSuccess)
                {
                    return BadRequest(messagesResult.Message);
                }

                var messageHistory = messagesResult.Data.Select(m => new MessageHistory
                {
                    MessageId = m.MessageId,
                    UserMessage = m.Role == "user" ? m.Content : "", // Extract user content from Role/Content structure
                    AiResponse = m.Role == "assistant" ? m.Content : "", // Extract AI content from Role/Content structure
                    ModelUsed = m.ModelUsed ?? "",
                    MessageTimestamp = m.CreatedAt // Use CreatedAt instead of MessageTimestamp
                }).ToList();

                return Ok(ServiceResult<IEnumerable<MessageHistory>>.Success(messageHistory));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation messages");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get paginated conversation messages
        /// </summary>
        [HttpGet("branch/{branchId}/messages/paginated")]
        public async Task<IActionResult> GetPaginatedMessages(Guid branchId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User not authenticated");
                }

                if (pageNumber < 1 || pageSize < 1 || pageSize > 100)
                {
                    return BadRequest("Invalid pagination parameters");
                }

                var messagesResult = await _chatService.GetPaginatedMessagesAsync(branchId, userId, pageNumber, pageSize);
                if (!messagesResult.IsSuccess)
                {
                    return BadRequest(messagesResult.Message);
                }

                var messageHistory = messagesResult.Data.Select(m => new MessageHistory
                {
                    MessageId = m.MessageId,
                    UserMessage = m.Role == "user" ? m.Content : "",
                    AiResponse = m.Role == "assistant" ? m.Content : "",
                    ModelUsed = m.ModelUsed ?? "",
                    MessageTimestamp = m.CreatedAt
                }).ToList();

                return Ok(ServiceResult<IEnumerable<MessageHistory>>.Success(messageHistory));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paginated messages");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get user's conversations
        /// </summary>
        [HttpGet("conversations")]
        public async Task<IActionResult> GetUserConversations([FromQuery] bool includeBranches = false)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User not authenticated");
                }

                var conversationsResult = await _conversationService.GetUserConversationsAsync(userId, includeBranches);
                if (!conversationsResult.IsSuccess)
                {
                    return BadRequest(conversationsResult.Message);
                }

                var conversations = conversationsResult.Data ?? Enumerable.Empty<Conversation>();
                var conversationSummaries = conversations.Select(c => new ConversationSummary
                {
                    ConversationId = c.ConversationId,
                    StartedAt = c.StartedAt,
                    EndedAt = c.EndedAt,
                    MessageCount = c.Branches?.SelectMany(b => b.Messages).Count() ?? 0, // Count messages from all branches
                    LastMessage = c.Branches?.SelectMany(b => b.Messages)
                        .OrderByDescending(m => m.CreatedAt)
                        .FirstOrDefault()?.Content, // Get last message content
                    IsActive = c.IsActive
                }).ToList();

                return Ok(ServiceResult<IEnumerable<ConversationSummary>>.Success(conversationSummaries));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user conversations");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Start a new conversation
        /// </summary>
        [HttpPost("conversations/start")]
        public async Task<IActionResult> StartNewConversation()
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User not authenticated");
                }

                var conversationResult = await _conversationService.StartNewConversationAsync(userId, IsPaidUser());
                if (!conversationResult.IsSuccess)
                {
                    return BadRequest(conversationResult.Message);
                }

                return Ok(ServiceResult<object>.Success(new { ConversationId = conversationResult.Data.ConversationId }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting new conversation");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// End a conversation
        /// </summary>
        [HttpPost("conversations/{conversationId}/end")]
        public async Task<IActionResult> EndConversation(Guid conversationId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User not authenticated");
                }

                var result = await _conversationService.EndConversationAsync(conversationId, userId);
                if (!result.IsSuccess)
                {
                    return BadRequest(result.Message);
                }

                return Ok(ServiceResult<bool>.Success(true, "Conversation ended successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ending conversation");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Delete a message
        /// </summary>
        [HttpDelete("messages/{messageId}")]
        public async Task<IActionResult> DeleteMessage(Guid messageId)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User not authenticated");
                }

                var result = await _chatService.DeleteMessageAsync(messageId, userId);
                if (!result.IsSuccess)
                {
                    return BadRequest(result.Message);
                }

                return Ok(ServiceResult<bool>.Success(true, "Message deleted successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting message");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Regenerate AI response for a message
        /// </summary>
        [HttpPost("messages/{messageId}/regenerate")]
        public async Task<IActionResult> RegenerateResponse(Guid messageId, [FromBody] RegenerateResponseRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User not authenticated");
                }

                var result = await _chatService.RegenerateResponseAsync(messageId, userId, request.NewModelName);
                if (!result.IsSuccess)
                {
                    return BadRequest(result.Message);
                }

                if (result.Data == null)
                {
                    return StatusCode(500, "Failed to regenerate response");
                }

                var response = new SendMessageResponse
                {
                    MessageId = result.Data.MessageId,
                    UserMessage = result.Data.Role == "user" ? result.Data.Content : "", // Extract from Role/Content
                    AiResponse = result.Data.Role == "assistant" ? result.Data.Content : "", // Extract from Role/Content
                    ModelUsed = result.Data.ModelUsed ?? string.Empty,
                    MessageTimestamp = result.Data.CreatedAt, // Use CreatedAt
                    ConversationId = Guid.Empty // Will need to get from branch if needed
                };

                return Ok(ServiceResult<SendMessageResponse>.Success(response));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error regenerating response");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get available AI models for the user
        /// </summary>
        [HttpGet("models")]
        public async Task<IActionResult> GetAvailableModels()
        {
            try
            {
                var result = await _chatService.GetAvailableModelsAsync(IsPaidUser());
                if (!result.IsSuccess)
                {
                    return BadRequest(result.Message);
                }

                return Ok(ServiceResult<IEnumerable<string>>.Success(result.Data));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available models");
                return StatusCode(500, "Internal server error");
            }
        }

        private string GetCurrentUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
        }

        private bool IsPaidUser()
        {
            // You can implement logic to determine if user is paid based on claims or database lookup
            var isPaidClaim = User.FindFirst("IsPaid")?.Value;
            return bool.TryParse(isPaidClaim, out bool isPaid) && isPaid;
        }
    }
}
