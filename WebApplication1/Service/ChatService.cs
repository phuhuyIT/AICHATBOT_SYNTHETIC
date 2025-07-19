using System.Text.Json;
using WebApplication1.DTO;
using WebApplication1.DTO.ChatbotModel;
using WebApplication1.Models;
using WebApplication1.Repository.Interface;
using WebApplication1.Service.Interface;

namespace WebApplication1.Service
{
    public class ChatService : IChatService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IChatbotModelsService _chatbotModelsService;
        private readonly IApiKeyService _apiKeyService;
        private readonly ILogger<ChatService> _logger;
        private readonly HttpClient _httpClient;

        // AI Provider endpoints
        private readonly Dictionary<string, string> _aiProviderEndpoints = new()
        {
            { "gpt-3.5-turbo", "https://api.openai.com/v1/chat/completions" },
            { "gpt-4", "https://api.openai.com/v1/chat/completions" },
            { "gpt-4o", "https://api.openai.com/v1/chat/completions" },
            { "gemini-pro", "https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent" },
            { "claude-3-sonnet", "https://api.anthropic.com/v1/messages" },
            { "claude-3-haiku", "https://api.anthropic.com/v1/messages" },
            { "grok-beta", "https://api.x.ai/v1/chat/completions" }
        };

        public ChatService(
            IUnitOfWork unitOfWork,
            IChatbotModelsService chatbotModelsService,
            IApiKeyService apiKeyService,
            ILogger<ChatService> logger,
            HttpClient httpClient)
        {
            _unitOfWork = unitOfWork;
            _chatbotModelsService = chatbotModelsService;
            _apiKeyService = apiKeyService;
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<ServiceResult<Message>> SendMessageAsync(string userId, Guid conversationId, string userMessage, string modelName)
        {
            try
            {
                // Validate conversation access
                var hasAccess = await _unitOfWork.ConversationRepository.IsConversationOwnedByUserAsync(conversationId, userId);
                if (!hasAccess)
                {
                    return ServiceResult<Message>.Failure("Access denied to conversation");
                }

                // Get or create main branch for this conversation
                var conversation = await _unitOfWork.ConversationRepository.GetByIdAsync(conversationId);
                var mainBranch = conversation?.Branches?.FirstOrDefault() ?? 
                    new ConversationBranch { ConversationId = conversationId };

                if (mainBranch.BranchId == Guid.Empty)
                {
                    await _unitOfWork.ConversationBranchRepository.AddAsync(mainBranch);
                    await _unitOfWork.SaveChangesAsync();
                }

                // Get conversation history for context
                var conversationHistory = await _unitOfWork.MessageRepository.GetBranchMessagesAsync(mainBranch.BranchId);
                var historyList = conversationHistory.ToList();

                // Get AI response
                var aiResponseResult = await GetAIResponseAsync(userMessage, modelName, historyList);
                if (!aiResponseResult.IsSuccess)
                {
                    return ServiceResult<Message>.Failure(aiResponseResult.Message);
                }

                // Create and save user message
                var userMsg = new Message
                {
                    BranchId = mainBranch.BranchId,
                    Role = "user",
                    Content = userMessage,
                    ModelUsed = modelName,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.MessageRepository.AddAsync(userMsg);
                await _unitOfWork.SaveChangesAsync();

                // Create and save AI response message
                var aiMessage = new Message
                {
                    BranchId = mainBranch.BranchId,
                    Role = "assistant",
                    Content = aiResponseResult.Data,
                    ModelUsed = modelName,
                    ParentMessageId = userMsg.MessageId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.MessageRepository.AddAsync(aiMessage);
                await _unitOfWork.SaveChangesAsync();
                
                _logger.LogInformation("Message sent successfully in conversation {ConversationId} using model {ModelName}", 
                    conversationId, modelName);
                
                return ServiceResult<Message>.Success(aiMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message in conversation {ConversationId}", conversationId);
                return ServiceResult<Message>.Failure("Failed to send message");
            }
        }

        public async Task<ServiceResult<string>> GetAIResponseAsync(string userMessage, string modelName, List<Message> conversationHistory)
        {
            try
            {
                // Get API key for the model
                var apiKeyResult = await _apiKeyService.GetApiKeyForModelAsync(modelName);
                if (!apiKeyResult.IsSuccess)
                {
                    return ServiceResult<string>.Failure("API key not found for model: " + modelName);
                }

                var apiKey = apiKeyResult.Data;

                // Route to appropriate AI provider
                return modelName.ToLower() switch
                {
                    var model when model.StartsWith("gpt") => await GetOpenAIResponse(userMessage, modelName, conversationHistory, apiKey),
                    var model when model.StartsWith("gemini") => await GetGeminiResponse(userMessage, modelName, conversationHistory, apiKey),
                    var model when model.StartsWith("claude") => await GetClaudeResponse(userMessage, modelName, conversationHistory, apiKey),
                    var model when model.StartsWith("grok") => await GetGrokResponse(userMessage, modelName, conversationHistory, apiKey),
                    _ => ServiceResult<string>.Failure("Unsupported model: " + modelName)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting AI response for model {ModelName}", modelName);
                return ServiceResult<string>.Failure("Failed to get AI response");
            }
        }

        private async Task<ServiceResult<string>> GetOpenAIResponse(string userMessage, string modelName, List<Message> conversationHistory, string apiKey)
        {
            try
            {
                var messages = new List<object>
                {
                    new { role = "system", content = "You are a helpful assistant." }
                };

                // Add conversation history using new Role/Content structure
                foreach (var msg in conversationHistory.TakeLast(10)) // Limit context
                {
                    messages.Add(new { role = msg.Role, content = msg.Content });
                }

                messages.Add(new { role = "user", content = userMessage });

                var requestBody = new
                {
                    model = modelName,
                    messages = messages,
                    max_tokens = 1000,
                    temperature = 0.7
                };

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var response = await _httpClient.PostAsJsonAsync(_aiProviderEndpoints[modelName], requestBody);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("OpenAI API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                    return ServiceResult<string>.Failure("AI service error");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
                var aiResponse = jsonResponse.GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                return ServiceResult<string>.Success(aiResponse ?? "No response generated");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling OpenAI API");
                return ServiceResult<string>.Failure("Failed to get response from OpenAI");
            }
        }

        private async Task<ServiceResult<string>> GetGeminiResponse(string userMessage, string modelName, List<Message> conversationHistory, string apiKey)
        {
            try
            {
                var context = string.Join("\n", conversationHistory.TakeLast(5).Select(m => $"{m.Role}: {m.Content}"));
                var fullPrompt = string.IsNullOrEmpty(context) ? userMessage : $"{context}\nUser: {userMessage}";

                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = fullPrompt }
                            }
                        }
                    }
                };

                var url = $"{_aiProviderEndpoints[modelName]}?key={apiKey}";
                var response = await _httpClient.PostAsJsonAsync(url, requestBody);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Gemini API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                    return ServiceResult<string>.Failure("AI service error");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
                var aiResponse = jsonResponse.GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                return ServiceResult<string>.Success(aiResponse ?? "No response generated");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Gemini API");
                return ServiceResult<string>.Failure("Failed to get response from Gemini");
            }
        }

        private async Task<ServiceResult<string>> GetClaudeResponse(string userMessage, string modelName, List<Message> conversationHistory, string apiKey)
        {
            try
            {
                var messages = new List<object>();

                // Add conversation history using new Role/Content structure
                foreach (var msg in conversationHistory.TakeLast(10))
                {
                    messages.Add(new { role = msg.Role, content = msg.Content });
                }

                messages.Add(new { role = "user", content = userMessage });

                var requestBody = new
                {
                    model = modelName,
                    max_tokens = 1000,
                    messages = messages
                };

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
                _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

                var response = await _httpClient.PostAsJsonAsync(_aiProviderEndpoints[modelName], requestBody);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Claude API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                    return ServiceResult<string>.Failure("AI service error");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
                var aiResponse = jsonResponse.GetProperty("content")[0]
                    .GetProperty("text")
                    .GetString();

                return ServiceResult<string>.Success(aiResponse ?? "No response generated");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Claude API");
                return ServiceResult<string>.Failure("Failed to get response from Claude");
            }
        }

        private async Task<ServiceResult<string>> GetGrokResponse(string userMessage, string modelName, List<Message> conversationHistory, string apiKey)
        {
            try
            {
                var messages = new List<object>
                {
                    new { role = "system", content = "You are Grok, a helpful AI assistant." }
                };

                // Add conversation history using new Role/Content structure
                foreach (var msg in conversationHistory.TakeLast(10))
                {
                    messages.Add(new { role = msg.Role, content = msg.Content });
                }

                messages.Add(new { role = "user", content = userMessage });

                var requestBody = new
                {
                    model = modelName,
                    messages = messages,
                    max_tokens = 1000,
                    temperature = 0.7
                };

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var response = await _httpClient.PostAsJsonAsync(_aiProviderEndpoints[modelName], requestBody);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Grok API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                    return ServiceResult<string>.Failure("AI service error");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                
                var aiResponse = jsonResponse.GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                return ServiceResult<string>.Success(aiResponse ?? "No response generated");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Grok API");
                return ServiceResult<string>.Failure("Failed to get response from Grok");
            }
        }

        public async Task<ServiceResult<IEnumerable<Message>>> GetConversationMessagesAsync(Guid conversationId, string userId)
        {
            try
            {
                var hasAccess = await _unitOfWork.ConversationRepository.IsConversationOwnedByUserAsync(conversationId, userId);
                if (!hasAccess)
                {
                    return ServiceResult<IEnumerable<Message>>.Failure("Access denied to conversation");
                }

                var messages = await _unitOfWork.MessageRepository.GetConversationMessagesAsync(conversationId);
                return ServiceResult<IEnumerable<Message>>.Success(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting messages for conversation {ConversationId}", conversationId);
                return ServiceResult<IEnumerable<Message>>.Failure("Failed to retrieve messages");
            }
        }

        public async Task<ServiceResult<IEnumerable<Message>>> GetPaginatedMessagesAsync(Guid branchId, string userId, int pageNumber, int pageSize)
        {
            try
            {
                // Get conversation ID from branch to validate access
                var branch = await _unitOfWork.ConversationBranchRepository.GetByIdAsync(branchId);
                if (branch == null)
                {
                    return ServiceResult<IEnumerable<Message>>.Failure("Branch not found");
                }

                var hasAccess = await _unitOfWork.ConversationRepository.IsConversationOwnedByUserAsync(branch.ConversationId, userId);
                if (!hasAccess)
                {
                    return ServiceResult<IEnumerable<Message>>.Failure("Access denied to conversation");
                }

                var messages = await _unitOfWork.MessageRepository.GetPaginatedMessagesAsync(branchId, pageNumber, pageSize);
                return ServiceResult<IEnumerable<Message>>.Success(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paginated messages for branch {BranchId}", branchId);
                return ServiceResult<IEnumerable<Message>>.Failure("Failed to retrieve messages");
            }
        }

        public async Task<ServiceResult<bool>> DeleteMessageAsync(Guid messageId, string userId)
        {
            try
            {
                var message = await _unitOfWork.MessageRepository.GetByIdAsync(messageId);
                if (message == null)
                {
                    return ServiceResult<bool>.Failure("Message not found");
                }

                // Get conversation through branch to validate access
                var branch = await _unitOfWork.ConversationBranchRepository.GetByIdAsync(message.BranchId);
                if (branch == null)
                {
                    return ServiceResult<bool>.Failure("Branch not found");
                }

                var hasAccess = await _unitOfWork.ConversationRepository.IsConversationOwnedByUserAsync(branch.ConversationId, userId);
                if (!hasAccess)
                {
                    return ServiceResult<bool>.Failure("Access denied to message");
                }

                // Soft delete by updating timestamp (no IsActive in new model)
                message.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.MessageRepository.UpdateAsync(message);
                await _unitOfWork.SaveChangesAsync();

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting message {MessageId}", messageId);
                return ServiceResult<bool>.Failure("Failed to delete message");
            }
        }

        public async Task<ServiceResult<Message>> RegenerateResponseAsync(Guid messageId, string userId, string? newModelName = null)
        {
            try
            {
                var message = await _unitOfWork.MessageRepository.GetByIdAsync(messageId);
                if (message == null)
                {
                    return ServiceResult<Message>.Failure("Message not found");
                }

                // Get conversation through branch to validate access
                var branch = await _unitOfWork.ConversationBranchRepository.GetByIdAsync(message.BranchId);
                if (branch == null)
                {
                    return ServiceResult<Message>.Failure("Branch not found");
                }

                var hasAccess = await _unitOfWork.ConversationRepository.IsConversationOwnedByUserAsync(branch.ConversationId, userId);
                if (!hasAccess)
                {
                    return ServiceResult<Message>.Failure("Access denied to message");
                }

                var modelToUse = newModelName ?? message.ModelUsed ?? "gpt-3.5-turbo";
                var conversationHistory = await _unitOfWork.MessageRepository.GetBranchMessagesAsync(message.BranchId);
                
                // Get messages before the current one for context
                var contextMessages = conversationHistory.Where(m => m.CreatedAt < message.CreatedAt).ToList();
                
                // Find the user message content to regenerate response for
                var userContent = message.Role == "user" ? message.Content : 
                    contextMessages.LastOrDefault(m => m.Role == "user")?.Content ?? "Hello";
                
                var aiResponseResult = await GetAIResponseAsync(userContent, modelToUse, contextMessages);
                if (!aiResponseResult.IsSuccess)
                {
                    return ServiceResult<Message>.Failure(aiResponseResult.Message);
                }

                message.Content = aiResponseResult.Data;
                message.ModelUsed = modelToUse;
                message.UpdatedAt = DateTime.UtcNow;
                
                await _unitOfWork.MessageRepository.UpdateAsync(message);
                await _unitOfWork.SaveChangesAsync();

                return ServiceResult<Message>.Success(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error regenerating response for message {MessageId}", messageId);
                return ServiceResult<Message>.Failure("Failed to regenerate response");
            }
        }

        public async Task<ServiceResult<IEnumerable<string>>> GetAvailableModelsAsync(bool isPaidUser)
        {
            try
            {
                var modelsResult = await _chatbotModelsService.GetAllAsync();
                if (!modelsResult.IsSuccess)
                {
                    return ServiceResult<IEnumerable<string>>.Failure("Failed to retrieve available models");
                }
                
                var modelDtos = modelsResult.Data ?? [];
                var availableModels = modelDtos
                    .Where(m => m.IsActive && (isPaidUser || !m.IsAvailableForPaidUsers))
                    .Select(m => m.ModelName ?? string.Empty)
                    .ToList();

                return ServiceResult<IEnumerable<string>>.Success(availableModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available models");
                return ServiceResult<IEnumerable<string>>.Failure("Failed to retrieve available models");
            }
        }
    }
}
