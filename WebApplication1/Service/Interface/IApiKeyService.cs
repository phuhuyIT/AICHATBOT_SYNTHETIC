using WebApplication1.DTO;
using WebApplication1.DTO.ApiKey;
using WebApplication1.DTO.ChatbotModel;
using WebApplication1.Models;

namespace WebApplication1.Service.Interface
{
    public interface IApiKeyService : IReadService<ApiKeyResponseDTO>, IWriteService<ApiKeyCreateDTO, ApiKeyUpdateDTO, ApiKeyResponseDTO>
    {
        // Existing methods with original signatures
        Task<ServiceResult<List<ChatBotApiKey>>> CreateApiKeysAsync(int chatbotModelId, List<ApiKeyCreateDTO> apiKeyDtos);
        Task<ServiceResult<bool>> UpdateApiKeysAsync(int chatbotModelId, List<ApiKeyUpdateDTO> apiKeyDtos);
        Task<ServiceResult<bool>> DeleteApiKeysByModelIdAsync(int chatbotModelId);
        Task<ServiceResult<List<ChatBotApiKey>>> GetApiKeysByModelIdAsync(int chatbotModelId);
        Task<ServiceResult<bool>> ValidateApiKeyAsync(string apiKeyValue);
        Task<ServiceResult<string>> GetApiKeyForModelAsync(string modelName);
        Task<ServiceResult<bool>> IsApiKeyUniqueAsync(string apiKeyValue, int? excludeId = null);
        
        // DTO convenience overloads were removed after full migration.
    }
}
