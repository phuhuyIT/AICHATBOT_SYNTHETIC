using WebApplication1.DTO;
using WebApplication1.DTO.ChatbotModel;
using WebApplication1.Models;
using WebApplication1.Repository.Interface;
using WebApplication1.Service.Interface;

namespace WebApplication1.Service;

public class ChatbotModelsService : IChatbotModelsService
{
    private readonly ILogger<ChatbotModelsService> _logger;
    private readonly IGenericRepository<ChatbotModel> _chatbotModelsRepository;
    private readonly IApiKeyService _apiKeyService;

    public ChatbotModelsService(ILogger<ChatbotModelsService> logger, 
        IGenericRepository<ChatbotModel> chatbotModelsRepository,
        IApiKeyService apiKeyService)
    {
        _logger = logger;
        _chatbotModelsRepository = chatbotModelsRepository;
        _apiKeyService = apiKeyService;
    }

    public async Task<ServiceResult<ChatbotModel>> AddAsync(ChatbotModel entity)
    {
        if (entity == null)
        {
            _logger.LogError("ChatbotModel is null");
            return ServiceResult<ChatbotModel>.Failure("ChatbotModel is null");
        }
        
        if (string.IsNullOrEmpty(entity.ModelName))
        {
            _logger.LogError("ChatbotModel name is null or empty");
            return ServiceResult<ChatbotModel>.Failure("ChatbotModel name is null or empty");
        }

        try
        {
            await _chatbotModelsRepository.AddAsync(entity);
            return ServiceResult<ChatbotModel>.Success(entity, "ChatbotModel added successfully");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error adding ChatbotModel");
            return ServiceResult<ChatbotModel>.Failure($"Error adding ChatbotModel: {e.Message}");
        }
    }

    public async Task<ServiceResult<bool>> DeleteAsync(int id)
    {
        try
        {
            // First delete associated API keys
            var apiKeyDeleteResult = await _apiKeyService.DeleteApiKeysByModelIdAsync(id);
            if (!apiKeyDeleteResult.IsSuccess)
            {
                _logger.LogWarning("Failed to delete API keys for ChatbotModel {ModelId}: {Message}", 
                    id, apiKeyDeleteResult.Message);
            }

            // Then delete the ChatbotModel
            var result = await _chatbotModelsRepository.DeleteAsync(id);
            if (result)
                return ServiceResult<bool>.Success(true, "ChatbotModel deleted successfully");
            else
                return ServiceResult<bool>.Failure("ChatbotModel not found");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error deleting ChatbotModel with ID {Id}", id);
            return ServiceResult<bool>.Failure($"Error deleting ChatbotModel: {e.Message}");
        }
    }

    public async Task<ServiceResult<IEnumerable<ChatbotModel>>> GetAllAsync()
    {
        try
        {
            var models = await _chatbotModelsRepository.GetAllAsync();
            return ServiceResult<IEnumerable<ChatbotModel>>.Success(models);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error retrieving all ChatbotModels");
            return ServiceResult<IEnumerable<ChatbotModel>>.Failure($"Error retrieving ChatbotModels: {e.Message}");
        }
    }

    public async Task<ServiceResult<ChatbotModel>> GetByIdAsync(int id)
    {
        try
        {
            var model = await _chatbotModelsRepository.GetByIdAsync(id);
            if (model == null)
                return ServiceResult<ChatbotModel>.Failure("ChatbotModel not found");
            
            return ServiceResult<ChatbotModel>.Success(model);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error retrieving ChatbotModel with ID {Id}", id);
            return ServiceResult<ChatbotModel>.Failure($"Error retrieving ChatbotModel: {e.Message}");
        }
    }

    public async Task<ServiceResult<ChatbotModel>> UpdateAsync(ChatbotModel entity)
    {
        if (entity == null)
        {
            _logger.LogError("ChatbotModel is null");
            return ServiceResult<ChatbotModel>.Failure("ChatbotModel is null");
        }
        
        if (string.IsNullOrEmpty(entity.ModelName))
        {
            _logger.LogError("ChatbotModel name is null or empty");
            return ServiceResult<ChatbotModel>.Failure("ChatbotModel name is null or empty");
        }

        try
        {
            await _chatbotModelsRepository.UpdateAsync(entity);
            return ServiceResult<ChatbotModel>.Success(entity, "ChatbotModel updated successfully");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error updating ChatbotModel");
            return ServiceResult<ChatbotModel>.Failure($"Error updating ChatbotModel: {e.Message}");
        }
    }

    public async Task<ServiceResult<ChatbotModel>> AddWithApiKeysAsync(ChatbotModelCreateDTO dto)
    {
        if (dto == null)
        {
            _logger.LogError("ChatbotModelCreateDTO is null");
            return ServiceResult<ChatbotModel>.Failure("ChatbotModel data is null");
        }

        try
        {
            var chatbotModel = new ChatbotModel
            {
                ModelName = dto.ModelName,
                PricingTier = dto.PricingTier,
                IsAvailableForPaidUsers = dto.IsAvailableForPaidUsers
            };

            await _chatbotModelsRepository.AddAsync(chatbotModel);

            // Use ApiKeyService to handle API keys
            if (dto.ApiKeys != null && dto.ApiKeys.Any())
            {
                var apiKeyResult = await _apiKeyService.CreateApiKeysAsync(chatbotModel.Id, dto.ApiKeys);
                if (!apiKeyResult.IsSuccess)
                {
                    _logger.LogWarning("Failed to create API keys for ChatbotModel {ModelId}: {Message}", 
                        chatbotModel.Id, apiKeyResult.Message);
                }
            }

            return ServiceResult<ChatbotModel>.Success(chatbotModel, "ChatbotModel with API keys added successfully");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error adding ChatbotModel with API keys");
            return ServiceResult<ChatbotModel>.Failure($"Error adding ChatbotModel: {e.Message}");
        }
    }

    public async Task<ServiceResult<ChatbotModel>> UpdateWithApiKeysAsync(ChatbotModelUpdateDTO dto)
    {
        if (dto == null)
        {
            _logger.LogError("ChatbotModelUpdateDTO is null");
            return ServiceResult<ChatbotModel>.Failure("ChatbotModel data is null");
        }

        try
        {
            var existingModel = await _chatbotModelsRepository.GetByIdAsync(dto.Id);
            if (existingModel == null)
            {
                return ServiceResult<ChatbotModel>.Failure("ChatbotModel not found");
            }

            // Update ChatbotModel properties
            existingModel.ModelName = dto.ModelName;
            existingModel.PricingTier = dto.PricingTier;
            existingModel.IsAvailableForPaidUsers = dto.IsAvailableForPaidUsers;

            await _chatbotModelsRepository.UpdateAsync(existingModel);

            // Use ApiKeyService to handle API keys
            if (dto.ApiKeys != null)
            {
                var apiKeyResult = await _apiKeyService.UpdateApiKeysAsync(existingModel.Id, dto.ApiKeys);
                if (!apiKeyResult.IsSuccess)
                {
                    _logger.LogWarning("Failed to update API keys for ChatbotModel {ModelId}: {Message}", 
                        existingModel.Id, apiKeyResult.Message);
                }
            }

            return ServiceResult<ChatbotModel>.Success(existingModel, "ChatbotModel with API keys updated successfully");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error updating ChatbotModel with API keys");
            return ServiceResult<ChatbotModel>.Failure($"Error updating ChatbotModel: {e.Message}");
        }
    }
}