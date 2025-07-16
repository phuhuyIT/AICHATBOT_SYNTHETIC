using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApplication1.DTO;
using WebApplication1.Service.Interface;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly IConversationService _conversationService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(
            IChatService chatService,
            IConversationService conversationService,
            ILogger<ChatController> logger)
        {
            _chatService = chatService;
            _conversationService = conversationService;
            _logger = logger;
        }

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
                    UserMessage = messageResult.Data.UserMessage,
                    AiResponse = messageResult.Data.AiResponse,
                    ModelUsed = messageResult.Data.ModelUsed ?? "",
                    MessageTimestamp = messageResult.Data.MessageTimestamp,
                    ConversationId = messageResult.Data.ConversationId ?? 0
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
        public async Task<IActionResult> GetConversationMessages(int conversationId)
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
                    UserMessage = m.UserMessage,
                    AiResponse = m.AiResponse,
                    ModelUsed = m.ModelUsed ?? "",
                    MessageTimestamp = m.MessageTimestamp
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
        [HttpGet("conversation/{conversationId}/messages/paginated")]
        public async Task<IActionResult> GetPaginatedMessages(int conversationId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
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

                var messagesResult = await _chatService.GetPaginatedMessagesAsync(conversationId, userId, pageNumber, pageSize);
                if (!messagesResult.IsSuccess)
                {
                    return BadRequest(messagesResult.Message);
                }

                var messageHistory = messagesResult.Data.Select(m => new MessageHistory
                {
                    MessageId = m.MessageId,
                    UserMessage = m.UserMessage,
                    AiResponse = m.AiResponse,
                    ModelUsed = m.ModelUsed ?? "",
                    MessageTimestamp = m.MessageTimestamp
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
        public async Task<IActionResult> GetUserConversations([FromQuery] bool includeMessages = false)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User not authenticated");
                }

                var conversationsResult = await _conversationService.GetUserConversationsAsync(userId, includeMessages);
                if (!conversationsResult.IsSuccess)
                {
                    return BadRequest(conversationsResult.Message);
                }

                var conversationSummaries = conversationsResult.Data.Select(c => new ConversationSummary
                {
                    ConversationId = c.ConversationId,
                    StartedAt = c.StartedAt,
                    EndedAt = c.EndedAt,
                    MessageCount = c.Messages?.Count ?? 0,
                    LastMessage = c.Messages?.LastOrDefault()?.UserMessage,
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
        public async Task<IActionResult> EndConversation(int conversationId)
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
        public async Task<IActionResult> DeleteMessage(int messageId)
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
        public async Task<IActionResult> RegenerateResponse(int messageId, [FromBody] RegenerateResponseRequest request)
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

                var response = new SendMessageResponse
                {
                    MessageId = result.Data.MessageId,
                    UserMessage = result.Data.UserMessage,
                    AiResponse = result.Data.AiResponse,
                    ModelUsed = result.Data.ModelUsed ?? "",
                    MessageTimestamp = result.Data.MessageTimestamp,
                    ConversationId = result.Data.ConversationId ?? 0
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
