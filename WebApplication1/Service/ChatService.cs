using System.Text.Json;
using WebApplication1.DTO;
using WebApplication1.Models;
using WebApplication1.Repository.Interface;
using WebApplication1.Service.Interface;

namespace WebApplication1.Service
{
    public class ChatService : IChatService
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IConversationRepository _conversationRepository;
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
            IMessageRepository messageRepository,
            IConversationRepository conversationRepository,
            IChatbotModelsService chatbotModelsService,
            IApiKeyService apiKeyService,
            ILogger<ChatService> logger,
            HttpClient httpClient)
        {
            _messageRepository = messageRepository;
            _conversationRepository = conversationRepository;
            _chatbotModelsService = chatbotModelsService;
            _apiKeyService = apiKeyService;
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<ServiceResult<Message>> SendMessageAsync(string userId, int conversationId, string userMessage, string modelName)
        {
            try
            {
                // Validate conversation access
                var hasAccess = await _conversationRepository.IsConversationOwnedByUserAsync(conversationId, userId);
                if (!hasAccess)
                {
                    return ServiceResult<Message>.Failure("Access denied to conversation");
                }

                // Get conversation history for context
                var conversationHistory = await _messageRepository.GetConversationMessagesAsync(conversationId);
                var historyList = conversationHistory.ToList();

                // Get AI response
                var aiResponseResult = await GetAIResponseAsync(userMessage, modelName, historyList);
                if (!aiResponseResult.IsSuccess)
                {
                    return ServiceResult<Message>.Failure(aiResponseResult.Message);
                }

                // Create and save message
                var message = new Message
                {
                    ConversationId = conversationId,
                    UserMessage = userMessage,
                    AiResponse = aiResponseResult.Data,
                    ModelUsed = modelName,
                    MessageTimestamp = DateTime.UtcNow,
                    IsActive = true,
                    UpdatedAt = DateTime.UtcNow
                };

                await _messageRepository.AddAsync(message);
                
                _logger.LogInformation("Message sent successfully in conversation {ConversationId} using model {ModelName}", 
                    conversationId, modelName);
                
                return ServiceResult<Message>.Success(message);
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

                // Add conversation history
                foreach (var msg in conversationHistory.TakeLast(10)) // Limit context
                {
                    messages.Add(new { role = "user", content = msg.UserMessage });
                    messages.Add(new { role = "assistant", content = msg.AiResponse });
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
                var context = string.Join("\n", conversationHistory.TakeLast(5).Select(m => $"User: {m.UserMessage}\nAssistant: {m.AiResponse}"));
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

                // Add conversation history
                foreach (var msg in conversationHistory.TakeLast(10))
                {
                    messages.Add(new { role = "user", content = msg.UserMessage });
                    messages.Add(new { role = "assistant", content = msg.AiResponse });
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

                // Add conversation history
                foreach (var msg in conversationHistory.TakeLast(10))
                {
                    messages.Add(new { role = "user", content = msg.UserMessage });
                    messages.Add(new { role = "assistant", content = msg.AiResponse });
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

        public async Task<ServiceResult<IEnumerable<Message>>> GetConversationMessagesAsync(int conversationId, string userId)
        {
            try
            {
                var hasAccess = await _conversationRepository.IsConversationOwnedByUserAsync(conversationId, userId);
                if (!hasAccess)
                {
                    return ServiceResult<IEnumerable<Message>>.Failure("Access denied to conversation");
                }

                var messages = await _messageRepository.GetConversationMessagesAsync(conversationId);
                return ServiceResult<IEnumerable<Message>>.Success(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting messages for conversation {ConversationId}", conversationId);
                return ServiceResult<IEnumerable<Message>>.Failure("Failed to retrieve messages");
            }
        }

        public async Task<ServiceResult<IEnumerable<Message>>> GetPaginatedMessagesAsync(int conversationId, string userId, int pageNumber, int pageSize)
        {
            try
            {
                var hasAccess = await _conversationRepository.IsConversationOwnedByUserAsync(conversationId, userId);
                if (!hasAccess)
                {
                    return ServiceResult<IEnumerable<Message>>.Failure("Access denied to conversation");
                }

                var messages = await _messageRepository.GetPaginatedMessagesAsync(conversationId, pageNumber, pageSize);
                return ServiceResult<IEnumerable<Message>>.Success(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting paginated messages for conversation {ConversationId}", conversationId);
                return ServiceResult<IEnumerable<Message>>.Failure("Failed to retrieve messages");
            }
        }

        public async Task<ServiceResult<bool>> DeleteMessageAsync(int messageId, string userId)
        {
            try
            {
                var message = await _messageRepository.GetByIdAsync(messageId);
                if (message == null)
                {
                    return ServiceResult<bool>.Failure("Message not found");
                }

                // Validate user access through conversation ownership
                var hasAccess = await _conversationRepository.IsConversationOwnedByUserAsync(message.ConversationId ?? 0, userId);
                if (!hasAccess)
                {
                    return ServiceResult<bool>.Failure("Access denied to message");
                }

                message.IsActive = false;
                message.UpdatedAt = DateTime.UtcNow;
                await _messageRepository.UpdateAsync(message);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting message {MessageId}", messageId);
                return ServiceResult<bool>.Failure("Failed to delete message");
            }
        }

        public async Task<ServiceResult<Message>> RegenerateResponseAsync(int messageId, string userId, string? newModelName = null)
        {
            try
            {
                var message = await _messageRepository.GetByIdAsync(messageId);
                if (message == null)
                {
                    return ServiceResult<Message>.Failure("Message not found");
                }

                var hasAccess = await _conversationRepository.IsConversationOwnedByUserAsync(message.ConversationId ?? 0, userId);
                if (!hasAccess)
                {
                    return ServiceResult<Message>.Failure("Access denied to message");
                }

                var modelToUse = newModelName ?? message.ModelUsed ?? "gpt-3.5-turbo";
                var conversationHistory = await _messageRepository.GetConversationMessagesAsync(message.ConversationId ?? 0);
                
                // Get messages before the current one for context
                var contextMessages = conversationHistory.Where(m => m.MessageTimestamp < message.MessageTimestamp).ToList();
                
                var aiResponseResult = await GetAIResponseAsync(message.UserMessage, modelToUse, contextMessages);
                if (!aiResponseResult.IsSuccess)
                {
                    return ServiceResult<Message>.Failure(aiResponseResult.Message);
                }

                message.AiResponse = aiResponseResult.Data;
                message.ModelUsed = modelToUse;
                message.UpdatedAt = DateTime.UtcNow;
                
                await _messageRepository.UpdateAsync(message);

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

                var availableModels = modelsResult.Data
                    .Where(m => m.IsActive && (isPaidUser || !m.IsAvailableForPaidUsers))
                    .Select(m => m.ModelName)
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
