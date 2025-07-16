using WebApplication1.DTO;
using WebApplication1.DTO.ChatbotModel;
using WebApplication1.Models;

namespace WebApplication1.Service.Interface
{
    public interface IApiKeyService : IService<ChatBotApiKey>
    {
        Task<ServiceResult<List<ChatBotApiKey>>> CreateApiKeysAsync(int chatbotModelId, List<ApiKeyCreateDTO> apiKeyDtos);
        Task<ServiceResult<bool>> UpdateApiKeysAsync(int chatbotModelId, List<ApiKeyUpdateDTO> apiKeyDtos);
        Task<ServiceResult<bool>> DeleteApiKeysByModelIdAsync(int chatbotModelId);
        Task<ServiceResult<List<ChatBotApiKey>>> GetApiKeysByModelIdAsync(int chatbotModelId);
        Task<ServiceResult<bool>> ValidateApiKeyAsync(string apiKeyValue);
        Task<ServiceResult<string>> GetApiKeyForModelAsync(string modelName);
    }
}
