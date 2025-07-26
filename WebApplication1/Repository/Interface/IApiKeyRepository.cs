using WebApplication1.Models;

namespace WebApplication1.Repository.Interface
{
    public interface IApiKeyRepository : IGenericRepository<ChatBotApiKey>
    {
        // Specific query methods to replace in-memory filtering
        Task<IEnumerable<ChatBotApiKey>> GetDeletedApiKeysAsync();
        Task<IEnumerable<ChatBotApiKey>> GetApiKeysByModelIdAsync(Guid modelId);
        Task<bool> IsApiKeyUniqueAsync(string apiKeyValue, Guid? excludeId = null);
        Task<bool> ValidateApiKeyExistsAndActiveAsync(string apiKeyValue);
        Task<ChatBotApiKey?> GetActiveApiKeyForModelAsync(string modelName);
    }
}
