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
    private readonly IChatbotModelsRepository _chatbotModelsRepository;
    private readonly IApiKeyService _apiKeyService;

    public ChatbotModelsService(ILogger<ChatbotModelsService> logger, 
        IChatbotModelsRepository chatbotModelsRepository,
        IApiKeyService apiKeyService)
    {
        _logger = logger;
        _chatbotModelsRepository = chatbotModelsRepository;
        _apiKeyService = apiKeyService;
    }

    #region IReadService / IWriteService Implementation

    public async Task<ServiceResult<ChatbotModelResponseDTO>> CreateAsync(ChatbotModelCreateDTO createDto)
    {
        if (createDto == null)
        {
            _logger.LogError("ChatbotModelCreateDTO is null");
            return ServiceResult<ChatbotModelResponseDTO>.Failure("ChatbotModel data is null");
        }

        try
        {
            var chatbotModel = new ChatbotModel
            {
                ModelName = createDto.ModelName,
                PricingTier = createDto.PricingTier,
                IsAvailableForPaidUsers = createDto.IsAvailableForPaidUsers
            };

            await _chatbotModelsRepository.AddAsync(chatbotModel);

            // Handle API keys if provided
            if (createDto.ApiKeys?.Any() == true)
            {
                var apiKeyResult = await _apiKeyService.CreateApiKeysAsync(chatbotModel.Id, createDto.ApiKeys);
                if (!apiKeyResult.IsSuccess)
                {
                    _logger.LogWarning("Failed to create API keys for ChatbotModel {ModelId}: {Message}", 
                        chatbotModel.Id, apiKeyResult.Message);
                }
            }

            var response = ChatbotModelMappingService.ToResponseDTO(chatbotModel);
            return response != null 
                ? ServiceResult<ChatbotModelResponseDTO>.Success(response, "ChatbotModel created successfully")
                : ServiceResult<ChatbotModelResponseDTO>.Failure("Failed to map chatbot model");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error creating ChatbotModel");
            return ServiceResult<ChatbotModelResponseDTO>.Failure($"Error creating ChatbotModel: {e.Message}");
        }
    }

    public async Task<ServiceResult<IEnumerable<ChatbotModelResponseDTO>>> GetAllAsync()
    {
        try
        {
            var models = await _chatbotModelsRepository.GetAllAsync();
            var dtos = ChatbotModelMappingService.ToResponseDTOList(models);
            return ServiceResult<IEnumerable<ChatbotModelResponseDTO>>.Success(dtos ?? new List<ChatbotModelResponseDTO>());
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error retrieving all ChatbotModels");
            return ServiceResult<IEnumerable<ChatbotModelResponseDTO>>.Failure($"Error: {e.Message}");
        }
    }

    public async Task<ServiceResult<ChatbotModelResponseDTO>> GetByIdAsync(Guid id)
    {
        try
        {
            var model = await _chatbotModelsRepository.GetByIdAsync(id);
            if (model == null)
                return ServiceResult<ChatbotModelResponseDTO>.Failure("Chatbot model not found");

            var dto = ChatbotModelMappingService.ToResponseDTO(model);
            return dto != null 
                ? ServiceResult<ChatbotModelResponseDTO>.Success(dto)
                : ServiceResult<ChatbotModelResponseDTO>.Failure("Failed to map chatbot model");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error retrieving ChatbotModel with ID {Id}", id);
            return ServiceResult<ChatbotModelResponseDTO>.Failure($"Error: {e.Message}");
        }
    }

    public async Task<ServiceResult<ChatbotModelResponseDTO>> UpdateAsync(Guid id, ChatbotModelUpdateDTO updateDto)
    {
        if (updateDto == null)
        {
            _logger.LogError("ChatbotModelUpdateDTO is null");
            return ServiceResult<ChatbotModelResponseDTO>.Failure("ChatbotModel data is null");
        }

        try
        {
            var existingModel = await _chatbotModelsRepository.GetByIdAsync(id);
            if (existingModel == null)
                return ServiceResult<ChatbotModelResponseDTO>.Failure("ChatbotModel not found");

            // Update model properties
            existingModel.ModelName = updateDto.ModelName;
            existingModel.PricingTier = updateDto.PricingTier;
            existingModel.IsAvailableForPaidUsers = updateDto.IsAvailableForPaidUsers;

            await _chatbotModelsRepository.UpdateAsync(existingModel);

            // Handle API keys if provided
            if (updateDto.ApiKeys != null)
            {
                var apiKeyResult = await _apiKeyService.UpdateApiKeysAsync(existingModel.Id, updateDto.ApiKeys);
                if (!apiKeyResult.IsSuccess)
                {
                    _logger.LogWarning("Failed to update API keys for ChatbotModel {ModelId}: {Message}", 
                        existingModel.Id, apiKeyResult.Message);
                }
            }

            var dto = ChatbotModelMappingService.ToResponseDTO(existingModel);
            return dto != null 
                ? ServiceResult<ChatbotModelResponseDTO>.Success(dto, "ChatbotModel updated successfully")
                : ServiceResult<ChatbotModelResponseDTO>.Failure("Failed to map chatbot model");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error updating ChatbotModel with ID {Id}", id);
            return ServiceResult<ChatbotModelResponseDTO>.Failure($"Error updating ChatbotModel: {e.Message}");
        }
    }

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
    {
        try
        {
            // Delete associated API keys first
            var apiKeyDeleteResult = await _apiKeyService.DeleteApiKeysByModelIdAsync(id);
            if (!apiKeyDeleteResult.IsSuccess)
            {
                _logger.LogWarning("Failed to delete API keys for ChatbotModel {ModelId}: {Message}", id, apiKeyDeleteResult.Message);
            }

            // Delete the ChatbotModel
            var result = await _chatbotModelsRepository.DeleteAsync(id);
            return result 
                ? ServiceResult<bool>.Success(true, "ChatbotModel deleted successfully")
                : ServiceResult<bool>.Failure("ChatbotModel not found");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error deleting ChatbotModel with ID {Id}", id);
            return ServiceResult<bool>.Failure($"Error deleting ChatbotModel: {e.Message}");
        }
    }

    public async Task<ServiceResult<ChatbotModelResponseDTO>> GetPaidChatbotModel()
    {
        try
        {
            var models = await _chatbotModelsRepository.GetPaidUserModelsAsync();
            var paidModel = models.FirstOrDefault(m => m.IsAvailableForPaidUsers == true);
            
            if (paidModel == null)
                return ServiceResult<ChatbotModelResponseDTO>.Failure("PaidChatbotModel not found");

            var response = ChatbotModelMappingService.ToResponseDTO(paidModel);
            return response != null 
                ? ServiceResult<ChatbotModelResponseDTO>.Success(response)
                : ServiceResult<ChatbotModelResponseDTO>.Failure("Failed to map paid chatbot model");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error getting PaidChatbotModel");
            return ServiceResult<ChatbotModelResponseDTO>.Failure($"Error getting PaidChatbotModel: {e.Message}");
        }
    }

    #endregion
}