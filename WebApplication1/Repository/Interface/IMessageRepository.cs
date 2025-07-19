using WebApplication1.Models;

namespace WebApplication1.Repository.Interface
{
    public interface IMessageRepository : IGenericRepository<Message>
    {
        // New branch-based methods for your updated model
        Task<IEnumerable<Message>> GetBranchMessagesAsync(Guid branchId);
        Task<Message?> GetLatestMessageAsync(Guid branchId);
        Task<IEnumerable<Message>> GetMessagesByModelAsync(string modelName);
        Task<int> GetMessageCountByBranchAsync(Guid branchId);
        Task<IEnumerable<Message>> GetPaginatedMessagesAsync(Guid branchId, int pageNumber, int pageSize);
        Task<IEnumerable<Message>> GetMessagesByRoleAsync(Guid branchId, string role);
        
        // Legacy method for backward compatibility (conversation-level operations)
        Task<IEnumerable<Message>> GetConversationMessagesAsync(Guid conversationId);
        
        // Updated methods with proper Guid support
        Task<IEnumerable<Message>> GetRecentMessagesAsync(Guid branchId, int count = 10);
        Task<bool> BulkDeactivateMessagesAsync(IEnumerable<Guid> messageIds);
        Task<IEnumerable<Message>> GetMessagesByDateRangeAsync(
            Guid branchId, DateTime startDate, DateTime endDate);
        Task<bool> DeleteMessagesByBranchAsync(Guid branchId);
    }
}
