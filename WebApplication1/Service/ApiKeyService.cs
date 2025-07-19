using WebApplication1.DTO;
using WebApplication1.DTO.ApiKey;
using WebApplication1.DTO.ChatbotModel;
using WebApplication1.Models;
using WebApplication1.Repository.Interface;
using WebApplication1.Service.Interface;
using ApiKeyCreateDTO = WebApplication1.DTO.ApiKey.ApiKeyCreateDTO;

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

        #region IReadService / IWriteService Implementation

        // CREATE
        public async Task<ServiceResult<ApiKeyResponseDTO>> CreateAsync(ApiKeyCreateDTO createDto)
        {
            return await CreateInternalAsync(createDto);
        }

        // READ ALL
        public async Task<ServiceResult<IEnumerable<ApiKeyResponseDTO>>> GetAllAsync()
        {
            try
            {
                var entities = await _apiKeyRepository.GetAllAsync();
                var dtos = ApiKeyMappingService.ToResponseDTOList(entities);
                return ServiceResult<IEnumerable<ApiKeyResponseDTO>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all API keys");
                return ServiceResult<IEnumerable<ApiKeyResponseDTO>>.Failure("Failed to retrieve API keys");
            }
        }

        // READ SINGLE
        public async Task<ServiceResult<ApiKeyResponseDTO>> GetByIdAsync(Guid id)
        {
            return await GetByIdInternalAsync(id);
        }

        // UPDATE
        public async Task<ServiceResult<ApiKeyResponseDTO>> UpdateAsync(Guid id, ApiKeyUpdateDTO updateDto)
        {
            if (updateDto == null)
            {
                return ServiceResult<ApiKeyResponseDTO>.Failure("API key data is null");
            }
            // Ensure path param and DTO id match
            updateDto.ApiKeyId = id;
            return await UpdateInternalAsync(updateDto);
        }

        // DELETE
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
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

        #endregion

        #region DTO-Based CRUD Operations

        private async Task<ServiceResult<ApiKeyResponseDTO>> CreateInternalAsync(ApiKeyCreateDTO dto)
        {
            if (dto == null)
            {
                _logger.LogError("ApiKeyCreateDTO is null");
                return ServiceResult<ApiKeyResponseDTO>.Failure("API key data is null");
            }

            try
            {
                // Check if API key is unique
                var isUniqueResult = await IsApiKeyUniqueAsync(dto.ApiKey);
                if (!isUniqueResult.IsSuccess || !isUniqueResult.Data)
                {
                    return ServiceResult<ApiKeyResponseDTO>.Failure("API key already exists");
                }

                var entity = ApiKeyMappingService.ToEntity(dto);
                await _apiKeyRepository.AddAsync(entity);
                
                var responseDto = ApiKeyMappingService.ToResponseDTO(entity);
                if (responseDto == null)
                    return ServiceResult<ApiKeyResponseDTO>.Failure("Failed to map API key");
                
                return ServiceResult<ApiKeyResponseDTO>.Success(responseDto, "API key created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating API key");
                return ServiceResult<ApiKeyResponseDTO>.Failure($"Error creating API key: {ex.Message}");
            }
        }

        private async Task<ServiceResult<ApiKeyResponseDTO>> GetByIdInternalAsync(Guid id)
        {
            try
            {
                var entity = await _apiKeyRepository.GetByIdAsync(id);
                if (entity == null)
                    return ServiceResult<ApiKeyResponseDTO>.Failure("API key not found");

                var responseDto = ApiKeyMappingService.ToResponseDTO(entity);
                if (responseDto == null)
                    return ServiceResult<ApiKeyResponseDTO>.Failure("Failed to map API key");
                
                return ServiceResult<ApiKeyResponseDTO>.Success(responseDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting API key {ApiKeyId}", id);
                return ServiceResult<ApiKeyResponseDTO>.Failure($"Error getting API key: {ex.Message}");
            }
        }

        private async Task<ServiceResult<List<ApiKeyListDTO>>> GetAllInternalListDtoAsync()
        {
            try
            {
                var entities = await _apiKeyRepository.GetAllAsync();
                var listDtos = ApiKeyMappingService.ToListDTOList(entities);
                return ServiceResult<List<ApiKeyListDTO>>.Success(listDtos ?? new List<ApiKeyListDTO>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all API keys");
                return ServiceResult<List<ApiKeyListDTO>>.Failure($"Error retrieving API keys: {ex.Message}");
            }
        }

        private async Task<ServiceResult<ApiKeyResponseDTO>> UpdateInternalAsync(ApiKeyUpdateDTO dto)
        {
            if (dto == null)
            {
                _logger.LogError("ApiKeyUpdateDTO is null");
                return ServiceResult<ApiKeyResponseDTO>.Failure("API key data is null");
            }

            try
            {
                var existingEntity = await _apiKeyRepository.GetByIdAsync(dto.ApiKeyId);
                if (existingEntity == null)
                {
                    return ServiceResult<ApiKeyResponseDTO>.Failure("API key not found");
                }

                // Check if API key is unique (excluding current record)
                var isUniqueResult = await IsApiKeyUniqueAsync(dto.ApiKey, dto.ApiKeyId);
                if (!isUniqueResult.IsSuccess || !isUniqueResult.Data)
                {
                    return ServiceResult<ApiKeyResponseDTO>.Failure("API key already exists");
                }

                ApiKeyMappingService.UpdateEntity(existingEntity, dto);
                await _apiKeyRepository.UpdateAsync(existingEntity);
                
                var responseDto = ApiKeyMappingService.ToResponseDTO(existingEntity);
                if (responseDto == null)
                    return ServiceResult<ApiKeyResponseDTO>.Failure("Failed to map API key");
                
                return ServiceResult<ApiKeyResponseDTO>.Success(responseDto, "API key updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating API key");
                return ServiceResult<ApiKeyResponseDTO>.Failure($"Error updating API key: {ex.Message}");
            }
        }

        #endregion

        #region Model-Specific Operations

        public async Task<ServiceResult<List<ChatBotApiKey>>> GetApiKeysByModelIdAsync(Guid modelId)
        {
            try
            {
                var allEntities = await _apiKeyRepository.GetAllAsync();
                var modelEntities = allEntities.Where(k => k.ChatbotModelId == modelId).ToList();
                return ServiceResult<List<ChatBotApiKey>>.Success(modelEntities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting API keys for ChatbotModel {ChatbotModelId}", modelId);
                return ServiceResult<List<ChatBotApiKey>>.Failure($"Error getting API keys: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<List<ApiKeyResponseDTO>>> GetApiKeysByModelIdWithDtoAsync(Guid modelId)
        {
            try
            {
                var result = await GetApiKeysByModelIdAsync(modelId);
                if (!result.IsSuccess)
                {
                    return ServiceResult<List<ApiKeyResponseDTO>>.Failure(result.Message);
                }
                
                var responseDtos = ApiKeyMappingService.ToResponseDTOList(result.Data);
                return ServiceResult<List<ApiKeyResponseDTO>>.Success(responseDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting API keys with DTOs for ChatbotModel {ChatbotModelId}", modelId);
                return ServiceResult<List<ApiKeyResponseDTO>>.Failure($"Error getting API keys: {ex.Message}");
            }
        }

        public async Task<ServiceResult<List<ChatBotApiKey>>> CreateApiKeysAsync(Guid chatbotModelId, List<ApiKeyCreateDTO> apiKeyDtos)
        {
            if (apiKeyDtos == null || !apiKeyDtos.Any())
            {
                return ServiceResult<List<ChatBotApiKey>>.Success(new List<ChatBotApiKey>(), "No API keys to create");
            }

            try
            {
                var createdEntities = new List<ChatBotApiKey>();

                foreach (var apiKeyDto in apiKeyDtos)
                {
                    // Set the model ID for each API key
                    apiKeyDto.ChatbotModelId = chatbotModelId;
                    
                    var entity = ApiKeyMappingService.ToEntity(apiKeyDto);
                    await _apiKeyRepository.AddAsync(entity);
                    createdEntities.Add(entity);
                }

                return ServiceResult<List<ChatBotApiKey>>.Success(createdEntities, "API keys created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating API keys for ChatbotModel {ChatbotModelId}", chatbotModelId);
                return ServiceResult<List<ChatBotApiKey>>.Failure($"Error creating API keys: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<List<ApiKeyResponseDTO>>> CreateApiKeysWithDtoAsync(Guid chatbotModelId, List<ApiKeyCreateDTO> apiKeyDtos)
        {
            if (apiKeyDtos == null || !apiKeyDtos.Any())
            {
                return ServiceResult<List<ApiKeyResponseDTO>>.Success(new List<ApiKeyResponseDTO>(), "No API keys to create");
            }

            try
            {
                var createdResponseDtos = new List<ApiKeyResponseDTO>();

                foreach (var apiKeyDto in apiKeyDtos)
                {
                    // Set the model ID for each API key
                    apiKeyDto.ChatbotModelId = chatbotModelId;
                    
                    var result = await CreateInternalAsync(apiKeyDto);
                    if (result.IsSuccess && result.Data != null)
                    {
                        createdResponseDtos.Add(result.Data);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to create API key: {Message}", result.Message);
                    }
                }

                return ServiceResult<List<ApiKeyResponseDTO>>.Success(createdResponseDtos, "API keys created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating API keys for ChatbotModel {ChatbotModelId}", chatbotModelId);
                return ServiceResult<List<ApiKeyResponseDTO>>.Failure($"Error creating API keys: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> UpdateApiKeysAsync(Guid chatbotModelId, List<ApiKeyUpdateDTO> apiKeyDtos)
        {
            if (apiKeyDtos == null || !apiKeyDtos.Any())
            {
                return ServiceResult<bool>.Success(true, "No API keys to update");
            }

            try
            {
                foreach (var apiKeyDto in apiKeyDtos)
                {
                    // Set the model ID for each API key
                    apiKeyDto.ChatbotModelId = chatbotModelId;
                    
                    var existingEntity = await _apiKeyRepository.GetByIdAsync(apiKeyDto.ApiKeyId);
                    if (existingEntity != null)
                    {
                        ApiKeyMappingService.UpdateEntity(existingEntity, apiKeyDto);
                        await _apiKeyRepository.UpdateAsync(existingEntity);
                    }
                    else
                    {
                        _logger.LogWarning("API key with ID {ApiKeyId} not found for update", apiKeyDto.ApiKeyId);
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

        public async Task<ServiceResult<bool>> DeleteApiKeysByModelIdAsync(Guid chatbotModelId)
        {
            try
            {
                var apiKeysResult = await GetApiKeysByModelIdAsync(chatbotModelId);
                if (apiKeysResult.IsSuccess && apiKeysResult.Data != null && apiKeysResult.Data.Any())
                {
                    foreach (var apiKey in apiKeysResult.Data)
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

        #endregion

        #region Validation and Utility Operations

        public async Task<ServiceResult<bool>> ValidateApiKeyAsync(string apiKeyValue)
        {
            try
            {
                if (string.IsNullOrEmpty(apiKeyValue))
                {
                    return ServiceResult<bool>.Success(false, "API key value is empty");
                }

                var allEntities = await _apiKeyRepository.GetAllAsync();
                var exists = allEntities.Any(k => k.ApiKey == apiKeyValue && k.IsActive);
                return ServiceResult<bool>.Success(exists, exists ? "API key is valid" : "API key is invalid or inactive");
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
                var allEntities = await _apiKeyRepository.GetAllAsync();
                var apiKey = allEntities
                    .Where(k => k.IsActive && k.ChatbotModel != null && k.ChatbotModel.ModelName == modelName)
                    .Select(k => k.ApiKey)
                    .FirstOrDefault();

                if (!string.IsNullOrEmpty(apiKey))
                {
                    return ServiceResult<string>.Success(apiKey);
                }
                
                return ServiceResult<string>.Failure($"No active API key found for model: {modelName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting API key for model {ModelName}", modelName);
                return ServiceResult<string>.Failure($"Error getting API key for model: {ex.Message}");
            }
        }

        public async Task<ServiceResult<bool>> IsApiKeyUniqueAsync(string apiKeyValue, Guid? excludeId = null)
        {
            try
            {
                if (string.IsNullOrEmpty(apiKeyValue))
                {
                    return ServiceResult<bool>.Success(false, "API key value is empty");
                }
                
                var allEntities = await _apiKeyRepository.GetAllAsync();
                var exists = allEntities.Any(k => k.ApiKey == apiKeyValue && (!excludeId.HasValue || k.ApiKeyId != excludeId.Value));
                return ServiceResult<bool>.Success(!exists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking API key uniqueness");
                return ServiceResult<bool>.Failure($"Error checking API key uniqueness: {ex.Message}");
            }
        }

        #endregion
    }
}
