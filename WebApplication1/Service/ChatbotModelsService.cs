using WebApplication1.DTO;
using WebApplication1.DTO.ChatbotModel;
using WebApplication1.Models;
using WebApplication1.Repository.Interface;
using WebApplication1.Service.Interface;
using WebApplication1.Service.MappingService;

namespace WebApplication1.Service;

public class ChatbotModelsService : IChatbotModelsService
{
    private readonly ILogger<ChatbotModelsService> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApiKeyService _apiKeyService;

    public ChatbotModelsService(ILogger<ChatbotModelsService> logger, 
        IUnitOfWork unitOfWork,
        IApiKeyService apiKeyService)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
        _apiKeyService = apiKeyService;
    }

    #region IReadService / IWriteService Implementation

    public async Task<ServiceResult<ChatbotModelResponseDTO>> CreateAsync(ChatbotModelCreateDTO createDto)
    {
        if (createDto is null)
        {
            _logger.LogError("ChatbotModelCreateDTO is null");
            return ServiceResult<ChatbotModelResponseDTO>.Failure("ChatbotModel data is null");
        }

        return await ServiceResult<ChatbotModelResponseDTO>.ExecuteWithTransactionAsync(async () =>
        {
            var chatbotModel = new ChatbotModel
            {
                ModelName = createDto.ModelName,
                PricingTier = createDto.PricingTier,
                IsAvailableForPaidUsers = createDto.IsAvailableForPaidUsers
            };

            await _unitOfWork.ChatbotModelsRepository.AddAsync(chatbotModel);

            // Handle API keys if provided
            if (createDto.ApiKeys?.Any() == true)
            {
                var apiKeyResult = await _apiKeyService.CreateApiKeysAsync(chatbotModel.Id, createDto.ApiKeys);
                if (!apiKeyResult.IsSuccess)
                {
                    _logger.LogWarning("Failed to create API keys for ChatbotModel {ModelId}: {Message}", 
                        chatbotModel.Id, apiKeyResult.Message);
                    return ServiceResult<ChatbotModelResponseDTO>.Failure($"Failed to create API keys: {apiKeyResult.Message}");
                }
            }

            return CreateSuccessResponse(chatbotModel, "ChatbotModel created successfully");
        }, _unitOfWork, _logger, "Error creating ChatbotModel");
    }

    public async Task<ServiceResult<IEnumerable<ChatbotModelResponseDTO>>> GetAllAsync()
    {
        return await ServiceResult<IEnumerable<ChatbotModelResponseDTO>>.ExecuteWithErrorHandlingAsync(async () =>
        {
            var models = await _unitOfWork.ChatbotModelsRepository.GetAllAsync();
            var dtos = GenericMappingService.MapToList<ChatbotModel, ChatbotModelResponseDTO>(models);
            return ServiceResult<IEnumerable<ChatbotModelResponseDTO>>.Success(dtos);
        },_unitOfWork ,_logger, "Error retrieving all ChatbotModels");
    }

    public async Task<ServiceResult<ChatbotModelResponseDTO>> GetByIdAsync(Guid id)
    {
        return await ServiceResult<ChatbotModelResponseDTO>.ExecuteWithErrorHandlingAsync(async () =>
        {
            var model = await _unitOfWork.ChatbotModelsRepository.GetByIdAsync(id);
            if (model is null)
                return ServiceResult<ChatbotModelResponseDTO>.Failure("Chatbot model not found");

            return CreateSuccessResponse(model);
        }, _unitOfWork,_logger, $"Error retrieving ChatbotModel with ID {id}");
    }

    public async Task<ServiceResult<ChatbotModelResponseDTO>> UpdateAsync(Guid id, ChatbotModelUpdateDTO updateDto)
    {
        if (updateDto is null)
        {
            _logger.LogError("ChatbotModelUpdateDTO is null");
            return ServiceResult<ChatbotModelResponseDTO>.Failure("ChatbotModel data is null");
        }

        return await ServiceResult<ChatbotModelResponseDTO>.ExecuteWithTransactionAsync(async () =>
        {
            var existingModel = await _unitOfWork.ChatbotModelsRepository.GetByIdAsync(id);
            if (existingModel is null)
            {
                return ServiceResult<ChatbotModelResponseDTO>.Failure("ChatbotModel not found");
            }

            // Update model properties
            UpdateModelProperties(existingModel, updateDto);
            await _unitOfWork.ChatbotModelsRepository.UpdateAsync(existingModel);

            // Handle API keys if provided
            if (updateDto.ApiKeys != null)
            {
                var apiKeyResult = await _apiKeyService.UpdateApiKeysAsync(existingModel.Id, updateDto.ApiKeys);
                if (!apiKeyResult.IsSuccess)
                {
                    _logger.LogWarning("Failed to update API keys for ChatbotModel {ModelId}: {Message}", 
                        existingModel.Id, apiKeyResult.Message);
                    return ServiceResult<ChatbotModelResponseDTO>.Failure($"Failed to update API keys: {apiKeyResult.Message}");
                }
            }

            return CreateSuccessResponse(existingModel, "ChatbotModel updated successfully");
        }, _unitOfWork, _logger, $"Error updating ChatbotModel with ID {id}");
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
    {
        return await ServiceResult<bool>.ExecuteWithTransactionAsync(async () =>
        {
            // Check if model exists
            var existingModel = await _unitOfWork.ChatbotModelsRepository.GetByIdAsync(id);
            if (existingModel == null)
            {
                return ServiceResult<bool>.Failure("ChatbotModel not found");
            }

            // Delete associated API keys first
            var apiKeyDeleteResult = await _apiKeyService.DeleteApiKeysByModelIdAsync(id);
            if (!apiKeyDeleteResult.IsSuccess)
            {
                _logger.LogWarning("Failed to delete API keys for ChatbotModel {ModelId}: {Message}", id, apiKeyDeleteResult.Message);
                return ServiceResult<bool>.Failure($"Failed to delete API keys: {apiKeyDeleteResult.Message}");
            }

            // Delete the ChatbotModel
            var result = await _unitOfWork.ChatbotModelsRepository.DeleteAsync(id);
            if (!result)
            {
                return ServiceResult<bool>.Failure("Failed to delete ChatbotModel");
            }

            return ServiceResult<bool>.Success(true, "ChatbotModel deleted successfully");
        }, _unitOfWork, _logger, $"Error deleting ChatbotModel with ID {id}");
    }

    public async Task<ServiceResult<ChatbotModelResponseDTO>> GetPaidChatbotModel()
    {
        return await ServiceResult<ChatbotModelResponseDTO>.ExecuteWithErrorHandlingAsync(async () =>
        {
            var models = await _unitOfWork.ChatbotModelsRepository.GetAllAsync();
            var paidModel = models.FirstOrDefault(m => m.IsAvailableForPaidUsers);
            
            if (paidModel == null)
                return ServiceResult<ChatbotModelResponseDTO>.Failure("PaidChatbotModel not found");

            return CreateSuccessResponse(paidModel);
        }, _unitOfWork,_logger, "Error getting PaidChatbotModel");
    }
    // get

    #endregion

    #region Private Helper Methods

    private static void UpdateModelProperties(ChatbotModel existingModel, ChatbotModelUpdateDTO updateDto)
    {
        existingModel.ModelName = updateDto.ModelName;
        existingModel.PricingTier = updateDto.PricingTier;
        existingModel.IsAvailableForPaidUsers = updateDto.IsAvailableForPaidUsers;
    }

    private static ServiceResult<ChatbotModelResponseDTO> CreateSuccessResponse(ChatbotModel model, string message = "")
    {
        var dto = GenericMappingService.MapToResponseDTO<ChatbotModel, ChatbotModelResponseDTO>(model);
        return dto != null 
            ? ServiceResult<ChatbotModelResponseDTO>.Success(dto, message)
            : ServiceResult<ChatbotModelResponseDTO>.Failure("Failed to map chatbot model");
    }


    #endregion

    #region Soft Delete Methods

    public async Task<ServiceResult<bool>> SoftDeleteAsync(Guid id)
    {
        return await ServiceResult<bool>.ExecuteWithTransactionAsync(async () =>
        {
            var existingModel = await _unitOfWork.ChatbotModelsRepository.GetByIdIncludingDeletedAsync(id);
            if (existingModel == null)
            {
                return ServiceResult<bool>.Failure("ChatbotModel not found");
            }

            var result = await _unitOfWork.ChatbotModelsRepository.SoftDeleteAsync(id);
            if (!result)
            {
                return ServiceResult<bool>.Failure("Failed to soft delete ChatbotModel");
            }

            _logger.LogInformation("ChatbotModel {ModelId} soft deleted successfully", id);
            return ServiceResult<bool>.Success(true, "ChatbotModel soft deleted successfully");
        }, _unitOfWork, _logger, $"Error soft deleting ChatbotModel with ID {id}");
    }

    public async Task<ServiceResult<bool>> RestoreAsync(Guid id)
    {
        return await ServiceResult<bool>.ExecuteWithTransactionAsync(async () =>
        {
            var existingModel = await _unitOfWork.ChatbotModelsRepository.GetByIdIncludingDeletedAsync(id);
            if (existingModel == null)
            {
                return ServiceResult<bool>.Failure("ChatbotModel not found");
            }

            var result = await _unitOfWork.ChatbotModelsRepository.RestoreAsync(id);
            if (!result)
            {
                return ServiceResult<bool>.Failure("Failed to restore ChatbotModel");
            }

            _logger.LogInformation("ChatbotModel {ModelId} restored successfully", id);
            return ServiceResult<bool>.Success(true, "ChatbotModel restored successfully");
        }, _unitOfWork, _logger, $"Error restoring ChatbotModel with ID {id}");
    }

    public async Task<ServiceResult<IEnumerable<ChatbotModelResponseDTO>>> GetDeletedAsync()
    {
        return await ServiceResult<IEnumerable<ChatbotModelResponseDTO>>.ExecuteWithErrorHandlingAsync(async () =>
        {
            // Use GetAllIncludingDeletedAsync and filter for deleted items
            var allModels = await _unitOfWork.ChatbotModelsRepository.GetAllIncludingDeletedAsync();
            var deletedModels = allModels.Where(m => !m.IsActive);
            var dtos = GenericMappingService.MapToList<ChatbotModel, ChatbotModelResponseDTO>(deletedModels);
            return ServiceResult<IEnumerable<ChatbotModelResponseDTO>>.Success(dtos);
        }, _unitOfWork, _logger, "Error retrieving deleted ChatbotModels");
    }

    #endregion
}