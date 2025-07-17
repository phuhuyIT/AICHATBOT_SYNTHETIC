using WebApplication1.Models;

namespace WebApplication1.Repository.Interface
{
    public interface IChatbotModelsRepository : IGenericRepository<ChatbotModel>
    {
        Task<ChatbotModel?> GetByModelNameAsync(string modelName);
        Task<IEnumerable<ChatbotModel>> GetActiveModelsAsync();
        Task<IEnumerable<ChatbotModel>> GetModelsByPricingTierAsync(string pricingTier);
        Task<bool> IsModelActiveAsync(string modelName);
        Task<ChatbotModel?> GetDefaultModelAsync();
        Task<IEnumerable<ChatbotModel>> GetPaidUserModelsAsync();
        Task<IEnumerable<ChatbotModel>> GetFreeUserModelsAsync();
    }
}
