using WebApplication1.Models;
using WebApplication1.Repository.Interface;
using WebApplication1.Service.Interface;
using WebApplication1.Service.Models;

namespace WebApplication1.Service;

public class ChatbotModelsService : IService<ChatbotModel>
{
    private readonly ILogger<ChatbotModelsService> _logger;
    private readonly IGenericRepository<ChatbotModel> _chatbotModelsRepository;

    public ChatbotModelsService(ILogger<ChatbotModelsService> logger, IGenericRepository<ChatbotModel> chatbotModelsRepository)
    {
        _logger = logger;
        _chatbotModelsRepository = chatbotModelsRepository;
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
}