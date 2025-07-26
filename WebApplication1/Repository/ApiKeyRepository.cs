using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Repository.Interface;

namespace WebApplication1.Repository
{
    public class ApiKeyRepository : GenericRepository<ChatBotApiKey>, IApiKeyRepository
    {
        public ApiKeyRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<ChatBotApiKey>> GetDeletedApiKeysAsync()
        {
            return await _context.Set<ChatBotApiKey>()
                .IgnoreQueryFilters() // Include soft-deleted entities
                .Where(k => !k.IsActive)
                .ToListAsync();
        }

        public async Task<IEnumerable<ChatBotApiKey>> GetApiKeysByModelIdAsync(Guid modelId)
        {
            return await _context.Set<ChatBotApiKey>()
                .Where(k => k.ChatbotModelId == modelId && k.IsActive)
                .ToListAsync();
        }

        public async Task<bool> IsApiKeyUniqueAsync(string apiKeyValue, Guid? excludeId = null)
        {
            if (string.IsNullOrEmpty(apiKeyValue))
                return false;

            var query = _context.Set<ChatBotApiKey>()
                .Where(k => k.ApiKey == apiKeyValue && k.IsActive);

            if (excludeId.HasValue)
            {
                query = query.Where(k => k.ApiKeyId != excludeId.Value);
            }

            return !await query.AnyAsync();
        }

        public async Task<bool> ValidateApiKeyExistsAndActiveAsync(string apiKeyValue)
        {
            if (string.IsNullOrEmpty(apiKeyValue))
                return false;

            return await _context.Set<ChatBotApiKey>()
                .AnyAsync(k => k.ApiKey == apiKeyValue && k.IsActive);
        }

        public async Task<ChatBotApiKey?> GetActiveApiKeyForModelAsync(string modelName)
        {
            return await _context.Set<ChatBotApiKey>()
                .Include(k => k.ChatbotModel)
                .Where(k => k.IsActive && k.ChatbotModel != null && k.ChatbotModel.ModelName == modelName)
                .FirstOrDefaultAsync();
        }
    }
}
