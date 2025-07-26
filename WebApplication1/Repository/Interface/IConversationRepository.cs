using WebApplication1.Models;

namespace WebApplication1.Repository.Interface
{
    public interface IConversationRepository : IGenericRepository<Conversation>
    {
        Task<IEnumerable<Conversation>> GetUserConversationsAsync(string userId, bool includeBranches = false);
        Task<Conversation?> GetConversationWithBranchesAsync(Guid conversationId);
        Task<IEnumerable<Conversation>> GetActiveConversationsAsync(string userId);
        Task<bool> DeactivateConversationAsync(Guid conversationId);
        Task<Conversation?> GetLatestConversationAsync(string userId);
        Task<bool> IsConversationOwnedByUserAsync(Guid conversationId, string userId);
        
        // New optimized methods with Guid support
        Task<IEnumerable<Conversation>> GetPaginatedUserConversationsAsync(
            string userId, int pageNumber, int pageSize, bool includeBranches = false);
        Task<int> GetUserConversationCountAsync(string userId);
        
        // Soft delete method
        Task<IEnumerable<Conversation>> GetDeletedUserConversationsAsync(string userId);
    }
}
