using WebApplication1.DTO;
using WebApplication1.DTO.ApiKey;
using WebApplication1.DTO.ChatbotModel;
using WebApplication1.Models;

namespace WebApplication1.Service.Interface
{
    public interface IApiKeyService : IReadService<ApiKeyResponseDTO>, IWriteService<ApiKeyCreateDTO, ApiKeyUpdateDTO, ApiKeyResponseDTO>
    {
        // Updated methods with Guid signatures
        Task<ServiceResult<List<ChatBotApiKey>>> CreateApiKeysAsync(Guid chatbotModelId, List<ApiKeyCreateDTO> apiKeyDtos);
        Task<ServiceResult<bool>> UpdateApiKeysAsync(Guid chatbotModelId, List<ApiKeyUpdateDTO> apiKeyDtos);
        Task<ServiceResult<bool>> DeleteApiKeysByModelIdAsync(Guid chatbotModelId);
        Task<ServiceResult<List<ChatBotApiKey>>> GetApiKeysByModelIdAsync(Guid chatbotModelId);
        Task<ServiceResult<bool>> ValidateApiKeyAsync(string apiKeyValue);
        Task<ServiceResult<string>> GetApiKeyForModelAsync(string modelName);
        Task<ServiceResult<bool>> IsApiKeyUniqueAsync(string apiKeyValue, Guid? excludeId = null);
        
        // DTO convenience overloads were removed after full migration.
    }
}
