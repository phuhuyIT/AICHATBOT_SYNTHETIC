using WebApplication1.DTO;
using WebApplication1.DTO.ChatbotModel;
using WebApplication1.Models;
using WebApplication1.Repository.Interface;
using WebApplication1.Service.Interface;

namespace WebApplication1.Service
{
    public class ApiKeyService : IApiKeyService
    {
        private readonly ILogger<ApiKeyService> _logger;
        private readonly IGenericRepository<ChatBotApiKey> _apiKeyRepository;

        public ApiKeyService(ILogger<ApiKeyService> logger, IGenericRepository<ChatBotApiKey> apiKeyRepository)
        {
            _logger = logger;
            _apiKeyRepository = apiKeyRepository;
        }

        #region IService<ChatBotApiKey> Implementation

        public async Task<ServiceResult<ChatBotApiKey>> AddAsync(ChatBotApiKey entity)
        {
            if (entity == null)
            {
                _logger.LogError("ChatBotApiKey is null");
                return ServiceResult<ChatBotApiKey>.Failure("API key data is null");
            }

            if (string.IsNullOrEmpty(entity.ApiKey))
            {
                _logger.LogError("API key value is null or empty");
                return ServiceResult<ChatBotApiKey>.Failure("API key value is required");
            }

            try
            {
                entity.CreatedAt = DateTime.UtcNow;
                await _apiKeyRepository.AddAsync(entity);
                return ServiceResult<ChatBotApiKey>.Success(entity, "API key added successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding API key");
                return ServiceResult<ChatBotApiKey>.Failure($"Error adding API key: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> DeleteAsync(int id)
        {
            try
            {
                var result = await _apiKeyRepository.DeleteAsync(id);
                if (result)
                    return ServiceResult<bool>.Success(true, "API key deleted successfully");
                else
                    return ServiceResult<bool>.Failure("API key not found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting API key with ID {Id}", id);
                return ServiceResult<bool>.Failure($"Error deleting API key: {ex.Message}");
            }
        }

        public async Task<ServiceResult<IEnumerable<ChatBotApiKey>>> GetAllAsync()
        {
            try
            {
                var apiKeys = await _apiKeyRepository.GetAllAsync();
                return ServiceResult<IEnumerable<ChatBotApiKey>>.Success(apiKeys);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all API keys");
                return ServiceResult<IEnumerable<ChatBotApiKey>>.Failure($"Error retrieving API keys: {ex.Message}");
            }
        }

        public async Task<ServiceResult<ChatBotApiKey>> GetByIdAsync(int id)
        {
            try
            {
                var apiKey = await _apiKeyRepository.GetByIdAsync(id);
                if (apiKey == null)
                    return ServiceResult<ChatBotApiKey>.Failure("API key not found");

                return ServiceResult<ChatBotApiKey>.Success(apiKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting API key {ApiKeyId}", id);
                return ServiceResult<ChatBotApiKey>.Failure($"Error getting API key: {ex.Message}");
            }
        }

        public async Task<ServiceResult<ChatBotApiKey>> UpdateAsync(ChatBotApiKey entity)
        {
            if (entity == null)
            {
                _logger.LogError("ChatBotApiKey is null");
                return ServiceResult<ChatBotApiKey>.Failure("API key data is null");
            }

            if (string.IsNullOrEmpty(entity.ApiKey))
            {
                _logger.LogError("API key value is null or empty");
                return ServiceResult<ChatBotApiKey>.Failure("API key value is required");
            }

            try
            {
                entity.UpdatedAt = DateTime.UtcNow;
                await _apiKeyRepository.UpdateAsync(entity);
                return ServiceResult<ChatBotApiKey>.Success(entity, "API key updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating API key");
                return ServiceResult<ChatBotApiKey>.Failure($"Error updating API key: {ex.Message}");
            }
        }

        #endregion

        #region Custom API Key Methods

        public async Task<ServiceResult<List<ChatBotApiKey>>> CreateApiKeysAsync(int chatbotModelId, List<ApiKeyCreateDTO> apiKeyDtos)
        {
            if (apiKeyDtos == null || !apiKeyDtos.Any())
            {
                return ServiceResult<List<ChatBotApiKey>>.Success(new List<ChatBotApiKey>(), "No API keys to create");
            }

            try
            {
                var createdApiKeys = new List<ChatBotApiKey>();

                foreach (var apiKeyDto in apiKeyDtos)
                {
                    var apiKey = new ChatBotApiKey
                    {
                        ApiKey = apiKeyDto.ApiKeyValue,
                        Setting = apiKeyDto.Description,
                        IsActive = apiKeyDto.IsActive,
                        ChatbotModelId = chatbotModelId,
                        CreatedAt = DateTime.UtcNow
                    };

                    var result = await AddAsync(apiKey);
                    if (result.IsSuccess)
                    {
                        createdApiKeys.Add(result.Data);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to create API key: {Message}", result.Message);
                    }
                }

                return ServiceResult<List<ChatBotApiKey>>.Success(createdApiKeys, "API keys created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating API keys for ChatbotModel {ChatbotModelId}", chatbotModelId);
                return ServiceResult<List<ChatBotApiKey>>.Failure($"Error creating API keys: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> UpdateApiKeysAsync(int chatbotModelId, List<ApiKeyUpdateDTO> apiKeyDtos)
        {
            if (apiKeyDtos == null || !apiKeyDtos.Any())
            {
                return ServiceResult<bool>.Success(true, "No API keys to update");
            }

            try
            {
                foreach (var apiKeyDto in apiKeyDtos)
                {
                    if (apiKeyDto.IsDeleted && apiKeyDto.Id.HasValue)
                    {
                        // Delete existing API key
                        await DeleteAsync(apiKeyDto.Id.Value);
                    }
                    else if (apiKeyDto.Id.HasValue)
                    {
                        // Update existing API key
                        var existingApiKey = await GetByIdAsync(apiKeyDto.Id.Value);
                        if (existingApiKey.IsSuccess)
                        {
                            existingApiKey.Data.ApiKey = apiKeyDto.ApiKeyValue;
                            existingApiKey.Data.Setting = apiKeyDto.Description;
                            existingApiKey.Data.IsActive = apiKeyDto.IsActive;

                            await UpdateAsync(existingApiKey.Data);
                        }
                    }
                    else
                    {
                        // Add new API key
                        var newApiKey = new ChatBotApiKey
                        {
                            ApiKey = apiKeyDto.ApiKeyValue,
                            Setting = apiKeyDto.Description,
                            IsActive = apiKeyDto.IsActive,
                            ChatbotModelId = chatbotModelId,
                            CreatedAt = DateTime.UtcNow
                        };

                        await AddAsync(newApiKey);
                    }
                }

                return ServiceResult<bool>.Success(true, "API keys updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating API keys for ChatbotModel {ChatbotModelId}", chatbotModelId);
                return ServiceResult<bool>.Failure($"Error updating API keys: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> DeleteApiKeysByModelIdAsync(int chatbotModelId)
        {
            try
            {
                var apiKeys = await GetApiKeysByModelIdAsync(chatbotModelId);
                if (apiKeys.IsSuccess && apiKeys.Data.Any())
                {
                    foreach (var apiKey in apiKeys.Data)
                    {
                        await DeleteAsync(apiKey.ApiKeyId);
                    }
                }

                return ServiceResult<bool>.Success(true, "API keys deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting API keys for ChatbotModel {ChatbotModelId}", chatbotModelId);
                return ServiceResult<bool>.Failure($"Error deleting API keys: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<ChatBotApiKey>>> GetApiKeysByModelIdAsync(int chatbotModelId)
        {
            try
            {
                var allApiKeys = await GetAllAsync();
                if (allApiKeys.IsSuccess)
                {
                    var modelApiKeys = allApiKeys.Data.Where(k => k.ChatbotModelId == chatbotModelId).ToList();
                    return ServiceResult<List<ChatBotApiKey>>.Success(modelApiKeys);
                }

                return ServiceResult<List<ChatBotApiKey>>.Failure("Failed to retrieve API keys");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting API keys for ChatbotModel {ChatbotModelId}", chatbotModelId);
                return ServiceResult<List<ChatBotApiKey>>.Failure($"Error getting API keys: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> ValidateApiKeyAsync(string apiKeyValue)
        {
            try
            {
                var allApiKeys = await GetAllAsync();
                if (allApiKeys.IsSuccess)
                {
                    var exists = allApiKeys.Data.Any(k => k.ApiKey == apiKeyValue && k.IsActive);
                    return ServiceResult<bool>.Success(exists, exists ? "API key is valid" : "API key is invalid or inactive");
                }

                return ServiceResult<bool>.Failure("Failed to validate API key");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating API key");
                return ServiceResult<bool>.Failure($"Error validating API key: {ex.Message}");
            }
        }

        public async Task<ServiceResult<string>> GetApiKeyForModelAsync(string modelName)
        {
            try
            {
                var allApiKeys = await GetAllAsync();
                if (allApiKeys.IsSuccess)
                {
                    var apiKey = allApiKeys.Data
                        .Where(k => k.IsActive && k.ChatbotModel != null && k.ChatbotModel.ModelName == modelName)
                        .Select(k => k.ApiKey)
                        .FirstOrDefault();

                    if (!string.IsNullOrEmpty(apiKey))
                    {
                        return ServiceResult<string>.Success(apiKey);
                    }
                    
                    return ServiceResult<string>.Failure($"No active API key found for model: {modelName}");
                }

                return ServiceResult<string>.Failure("Failed to retrieve API keys");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting API key for model {ModelName}", modelName);
                return ServiceResult<string>.Failure($"Error getting API key for model: {ex.Message}");
            }
        }

        #endregion
    }
}
