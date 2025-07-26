using WebApplication1.DTO;
using WebApplication1.DTO.ApiKey;
using WebApplication1.Models;
using WebApplication1.Repository.Interface;
using WebApplication1.Service.Interface;
using WebApplication1.Service.MappingService;

namespace WebApplication1.Service
{
    public class ApiKeyService : IApiKeyService
    {
        private readonly ILogger<ApiKeyService> _logger;
        private readonly IApiKeyRepository _apiKeyRepository;

        public ApiKeyService(ILogger<ApiKeyService> logger, IApiKeyRepository apiKeyRepository)
        {
            _logger = logger;
            _apiKeyRepository = apiKeyRepository;
        }

        #region IReadService / IWriteService Implementation

        public async Task<ServiceResult<ApiKeyResponseDTO>> CreateAsync(ApiKeyCreateDTO createDto)
        {
            if (createDto == null)
            {
                _logger.LogError("ApiKeyCreateDTO is null");
                return ServiceResult<ApiKeyResponseDTO>.Failure("API key data is null");
            }

            return await ServiceResult<ApiKeyResponseDTO>.ExecuteWithTransactionAsync(async () =>
            {
                // Use repository method instead of in-memory filtering
                var isUnique = await _apiKeyRepository.IsApiKeyUniqueAsync(createDto.ApiKey);
                if (!isUnique)
                {
                    return ServiceResult<ApiKeyResponseDTO>.Failure("API key already exists");
                }

                var entity = GenericMappingService.MapToEntity<ApiKeyCreateDTO, ChatBotApiKey>(createDto);
                await _apiKeyRepository.AddAsync(entity);
                
                var responseDto = GenericMappingService.MapToResponseDTO<ChatBotApiKey, ApiKeyResponseDTO>(entity);
                // Map navigation properties manually
                responseDto.ChatbotModelName = entity.ChatbotModel?.ModelName;
                responseDto.UserName = entity.User?.UserName;
                
                return ServiceResult<ApiKeyResponseDTO>.Success(responseDto, "API key created successfully");
            }, null, _logger, "Error creating API key");
        }

        public async Task<ServiceResult<IEnumerable<ApiKeyResponseDTO>>> GetAllAsync()
        {
            return await ServiceResult<IEnumerable<ApiKeyResponseDTO>>.ExecuteWithErrorHandlingAsync(async () =>
            {
                var entities = await _apiKeyRepository.GetAllAsync();
                var dtos = entities.Select(e =>
                {
                    var dto = GenericMappingService.MapToResponseDTO<ChatBotApiKey, ApiKeyResponseDTO>(e);
                    dto.ChatbotModelName = e.ChatbotModel?.ModelName;
                    dto.UserName = e.User?.UserName;
                    return dto;
                }).ToList();
                return ServiceResult<IEnumerable<ApiKeyResponseDTO>>.Success(dtos);
            }, null, _logger, "Error retrieving all API keys");
        }

        public async Task<ServiceResult<ApiKeyResponseDTO>> GetByIdAsync(Guid id)
        {
            return await ServiceResult<ApiKeyResponseDTO>.ExecuteWithErrorHandlingAsync(async () =>
            {
                var entity = await _apiKeyRepository.GetByIdAsync(id);
                if (entity == null)
                    return ServiceResult<ApiKeyResponseDTO>.Failure("API key not found");

                var responseDto = GenericMappingService.MapToResponseDTO<ChatBotApiKey, ApiKeyResponseDTO>(entity);
                responseDto.ChatbotModelName = entity.ChatbotModel?.ModelName;
                responseDto.UserName = entity.User?.UserName;
                
                return ServiceResult<ApiKeyResponseDTO>.Success(responseDto);
            }, null, _logger, $"Error getting API key {id}");
        }

        public async Task<ServiceResult<ApiKeyResponseDTO>> UpdateAsync(Guid id, ApiKeyUpdateDTO updateDto)
        {
            if (updateDto == null)
            {
                return ServiceResult<ApiKeyResponseDTO>.Failure("API key data is null");
            }

            updateDto.ApiKeyId = id; // Ensure consistency

            return await ServiceResult<ApiKeyResponseDTO>.ExecuteWithTransactionAsync(async () =>
            {
                var existingEntity = await _apiKeyRepository.GetByIdAsync(id);
                if (existingEntity == null)
                {
                    return ServiceResult<ApiKeyResponseDTO>.Failure("API key not found");
                }

                // Use repository method instead of in-memory filtering
                var isUnique = await _apiKeyRepository.IsApiKeyUniqueAsync(updateDto.ApiKey, id);
                if (!isUnique)
                {
                    return ServiceResult<ApiKeyResponseDTO>.Failure("API key already exists");
                }

                GenericMappingService.UpdateEntityFromDTO(updateDto, existingEntity);
                await _apiKeyRepository.UpdateAsync(existingEntity);
                
                var responseDto = GenericMappingService.MapToResponseDTO<ChatBotApiKey, ApiKeyResponseDTO>(existingEntity);
                responseDto.ChatbotModelName = existingEntity.ChatbotModel?.ModelName;
                responseDto.UserName = existingEntity.User?.UserName;
                
                return ServiceResult<ApiKeyResponseDTO>.Success(responseDto, "API key updated successfully");
            }, null, _logger, $"Error updating API key {id}");
        }

        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            return await ServiceResult<bool>.ExecuteWithTransactionAsync(async () =>
            {
                var result = await _apiKeyRepository.DeleteAsync(id);
                if (!result)
                {
                    return ServiceResult<bool>.Failure("API key not found");
                }

                return ServiceResult<bool>.Success(true, "API key deleted successfully");
            }, null, _logger, $"Error deleting API key {id}");
        }

        #endregion

        #region Soft Delete Methods

        public async Task<ServiceResult<bool>> SoftDeleteAsync(Guid id)
        {
            return await ServiceResult<bool>.ExecuteWithTransactionAsync(async () =>
            {
                var existingEntity = await _apiKeyRepository.GetByIdIncludingDeletedAsync(id);
                if (existingEntity == null)
                {
                    return ServiceResult<bool>.Failure("API key not found");
                }

                var result = await _apiKeyRepository.SoftDeleteAsync(id);
                if (!result)
                {
                    return ServiceResult<bool>.Failure("Failed to soft delete API key");
                }

                _logger.LogInformation("API key {ApiKeyId} soft deleted successfully", id);
                return ServiceResult<bool>.Success(true, "API key soft deleted successfully");
            }, null, _logger, $"Error soft deleting API key {id}");
        }

        public async Task<ServiceResult<bool>> RestoreAsync(Guid id)
        {
            return await ServiceResult<bool>.ExecuteWithTransactionAsync(async () =>
            {
                var existingEntity = await _apiKeyRepository.GetByIdIncludingDeletedAsync(id);
                if (existingEntity == null)
                {
                    return ServiceResult<bool>.Failure("API key not found");
                }

                var result = await _apiKeyRepository.RestoreAsync(id);
                if (!result)
                {
                    return ServiceResult<bool>.Failure("Failed to restore API key");
                }

                _logger.LogInformation("API key {ApiKeyId} restored successfully", id);
                return ServiceResult<bool>.Success(true, "API key restored successfully");
            }, null, _logger, $"Error restoring API key {id}");
        }

        public async Task<ServiceResult<IEnumerable<ApiKeyResponseDTO>>> GetDeletedAsync()
        {
            return await ServiceResult<IEnumerable<ApiKeyResponseDTO>>.ExecuteWithErrorHandlingAsync(async () =>
            {
                // Use repository method instead of in-memory filtering
                var deletedEntities = await _apiKeyRepository.GetDeletedApiKeysAsync();
                var dtos = deletedEntities.Select(e =>
                {
                    var dto = GenericMappingService.MapToResponseDTO<ChatBotApiKey, ApiKeyResponseDTO>(e);
                    dto.ChatbotModelName = e.ChatbotModel?.ModelName;
                    dto.UserName = e.User?.UserName;
                    return dto;
                }).ToList();
                return ServiceResult<IEnumerable<ApiKeyResponseDTO>>.Success(dtos);
            }, null, _logger, "Error retrieving deleted API keys");
        }

        #endregion

        #region Model-Specific Operations

        public async Task<ServiceResult<List<ChatBotApiKey>>> GetApiKeysByModelIdAsync(Guid modelId)
        {
            return await ServiceResult<List<ChatBotApiKey>>.ExecuteWithErrorHandlingAsync(async () =>
            {
                // Use repository method instead of in-memory filtering
                var modelEntities = await _apiKeyRepository.GetApiKeysByModelIdAsync(modelId);
                return ServiceResult<List<ChatBotApiKey>>.Success(modelEntities.ToList());
            }, null, _logger, $"Error getting API keys for ChatbotModel {modelId}");
        }

        public async Task<ServiceResult<List<ChatBotApiKey>>> CreateApiKeysAsync(Guid chatbotModelId, List<ApiKeyCreateDTO> apiKeyDtos)
        {
            if (apiKeyDtos == null || !apiKeyDtos.Any())
            {
                return ServiceResult<List<ChatBotApiKey>>.Success(new List<ChatBotApiKey>(), "No API keys to create");
            }

            return await ServiceResult<List<ChatBotApiKey>>.ExecuteWithTransactionAsync(async () =>
            {
                var createdEntities = new List<ChatBotApiKey>();

                foreach (var apiKeyDto in apiKeyDtos)
                {
                    // Set the model ID for each API key
                    apiKeyDto.ChatbotModelId = chatbotModelId;
                    
                    // Use repository method instead of in-memory filtering
                    var isUnique = await _apiKeyRepository.IsApiKeyUniqueAsync(apiKeyDto.ApiKey);
                    if (!isUnique)
                    {
                        _logger.LogWarning("Skipping duplicate API key: {ApiKey}", apiKeyDto.ApiKey);
                        continue;
                    }
                    
                    var entity = GenericMappingService.MapToEntity<ApiKeyCreateDTO, ChatBotApiKey>(apiKeyDto);
                    await _apiKeyRepository.AddAsync(entity);
                    createdEntities.Add(entity);
                }

                return ServiceResult<List<ChatBotApiKey>>.Success(createdEntities, $"Created {createdEntities.Count} API keys successfully");
            }, null, _logger, $"Error creating API keys for ChatbotModel {chatbotModelId}");
        }

        public async Task<ServiceResult<bool>> UpdateApiKeysAsync(Guid chatbotModelId, List<ApiKeyUpdateDTO> apiKeyDtos)
        {
            if (apiKeyDtos == null || !apiKeyDtos.Any())
            {
                return ServiceResult<bool>.Success(true, "No API keys to update");
            }

            return await ServiceResult<bool>.ExecuteWithTransactionAsync(async () =>
            {
                foreach (var apiKeyDto in apiKeyDtos)
                {
                    apiKeyDto.ChatbotModelId = chatbotModelId;
                    
                    var existingEntity = await _apiKeyRepository.GetByIdAsync(apiKeyDto.ApiKeyId);
                    if (existingEntity != null)
                    {
                        // Use repository method instead of in-memory filtering
                        var isUnique = await _apiKeyRepository.IsApiKeyUniqueAsync(apiKeyDto.ApiKey, apiKeyDto.ApiKeyId);
                        if (isUnique)
                        {
                            GenericMappingService.UpdateEntityFromDTO(apiKeyDto, existingEntity);
                            await _apiKeyRepository.UpdateAsync(existingEntity);
                        }
                        else
                        {
                            _logger.LogWarning("Skipping duplicate API key update: {ApiKey}", apiKeyDto.ApiKey);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("API key with ID {ApiKeyId} not found for update", apiKeyDto.ApiKeyId);
                    }
                }

                return ServiceResult<bool>.Success(true, "API keys updated successfully");
            }, null, _logger, $"Error updating API keys for ChatbotModel {chatbotModelId}");
        }

        public async Task<ServiceResult<bool>> DeleteApiKeysByModelIdAsync(Guid chatbotModelId)
        {
            return await ServiceResult<bool>.ExecuteWithTransactionAsync(async () =>
            {
                var apiKeysResult = await GetApiKeysByModelIdAsync(chatbotModelId);
                if (apiKeysResult.IsSuccess && apiKeysResult.Data?.Any() == true)
                {
                    foreach (var apiKey in apiKeysResult.Data)
                    {
                        await _apiKeyRepository.DeleteAsync(apiKey.ApiKeyId);
                    }
                }

                return ServiceResult<bool>.Success(true, "API keys deleted successfully");
            }, null, _logger, $"Error deleting API keys for ChatbotModel {chatbotModelId}");
        }

        #endregion

        #region Validation and Utility Operations

        public async Task<ServiceResult<bool>> ValidateApiKeyAsync(string apiKeyValue)
        {
            return await ServiceResult<bool>.ExecuteWithErrorHandlingAsync(async () =>
            {
                if (string.IsNullOrEmpty(apiKeyValue))
                {
                    return ServiceResult<bool>.Success(false, "API key value is empty");
                }

                // Use repository method instead of in-memory filtering
                var exists = await _apiKeyRepository.ValidateApiKeyExistsAndActiveAsync(apiKeyValue);
                return ServiceResult<bool>.Success(exists, exists ? "API key is valid" : "API key is invalid or inactive");
            }, null, _logger, "Error validating API key");
        }

        public async Task<ServiceResult<string>> GetApiKeyForModelAsync(string modelName)
        {
            return await ServiceResult<string>.ExecuteWithErrorHandlingAsync(async () =>
            {
                // Use repository method instead of in-memory filtering
                var apiKeyEntity = await _apiKeyRepository.GetActiveApiKeyForModelAsync(modelName);
                
                if (apiKeyEntity != null)
                {
                    return ServiceResult<string>.Success(apiKeyEntity.ApiKey);
                }
                
                return ServiceResult<string>.Failure($"No active API key found for model: {modelName}");
            }, null, _logger, $"Error getting API key for model {modelName}");
        }

        public async Task<ServiceResult<bool>> IsApiKeyUniqueAsync(string apiKeyValue, Guid? excludeId = null)
        {
            return await ServiceResult<bool>.ExecuteWithErrorHandlingAsync(async () =>
            {
                // Use repository method instead of in-memory filtering
                var isUnique = await _apiKeyRepository.IsApiKeyUniqueAsync(apiKeyValue, excludeId);
                return ServiceResult<bool>.Success(isUnique);
            }, null, _logger, "Error checking API key uniqueness");
        }

        #endregion
    }
}
