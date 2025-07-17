using WebApplication1.Models;

namespace WebApplication1.Repository.Interface
{
    public interface IMessageRepository : IGenericRepository<Message>
    {
        Task<IEnumerable<Message>> GetConversationMessagesAsync(int conversationId);
        Task<Message?> GetLatestMessageAsync(int conversationId);
        Task<IEnumerable<Message>> GetMessagesByModelAsync(string modelName);
        Task<int> GetMessageCountByConversationAsync(int conversationId);
        Task<IEnumerable<Message>> GetPaginatedMessagesAsync(int conversationId, int pageNumber, int pageSize);
        Task<bool> DeleteMessagesByConversationAsync(int conversationId);
        
        // New optimized methods
        Task<IEnumerable<Message>> GetRecentMessagesAsync(int conversationId, int count = 10);
        Task<bool> BulkDeactivateMessagesAsync(IEnumerable<int> messageIds);
        Task<IEnumerable<Message>> GetMessagesByDateRangeAsync(
            int conversationId, DateTime startDate, DateTime endDate);
    }
}
