using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Repository.Interface;

namespace WebApplication1.Repository
{
    public class ChatbotModelsRepository : GenericRepository<ChatbotModel>, IChatbotModelsRepository
    {
        public ChatbotModelsRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<ChatbotModel?> GetByModelNameAsync(string modelName)
        {
            return await _dbSet
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ModelName == modelName);
        }

        public async Task<IEnumerable<ChatbotModel>> GetActiveModelsAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .Where(m => m.IsActive)
                .OrderBy(m => m.ModelName)
                .ToListAsync();
        }

        public async Task<IEnumerable<ChatbotModel>> GetModelsByPricingTierAsync(string pricingTier)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(m => m.PricingTier == pricingTier && m.IsActive)
                .OrderBy(m => m.ModelName)
                .ToListAsync();
        }

        public async Task<bool> IsModelActiveAsync(string modelName)
        {
            return await _dbSet
                .AsNoTracking()
                .AnyAsync(m => m.ModelName == modelName && m.IsActive);
        }

        public async Task<ChatbotModel?> GetDefaultModelAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .Where(m => m.IsActive)
                .OrderBy(m => m.ModelName)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<ChatbotModel>> GetPaidUserModelsAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .Where(m => m.IsActive && m.IsAvailableForPaidUsers)
                .OrderBy(m => m.ModelName)
                .ToListAsync();
        }

        public async Task<IEnumerable<ChatbotModel>> GetFreeUserModelsAsync()
        {
            return await _dbSet
                .AsNoTracking()
                .Where(m => m.IsActive && !m.IsAvailableForPaidUsers)
                .OrderBy(m => m.ModelName)
                .ToListAsync();
        }
    }
}