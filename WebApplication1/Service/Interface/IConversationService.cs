using WebApplication1.DTO;
using WebApplication1.Models;

namespace WebApplication1.Service.Interface
{
    public interface IConversationService : IService<Conversation>
    {
        Task<ServiceResult<Conversation>> StartNewConversationAsync(string userId, bool isPaidUser);
        Task<ServiceResult<IEnumerable<Conversation>>> GetUserConversationsAsync(string userId, bool includeMessages = false);
        Task<ServiceResult<Conversation>> GetConversationWithMessagesAsync(int conversationId, string userId);
        Task<ServiceResult<bool>> EndConversationAsync(int conversationId, string userId);
        Task<ServiceResult<Conversation>> GetOrCreateActiveConversationAsync(string userId, bool isPaidUser);
        Task<ServiceResult<bool>> ValidateConversationAccessAsync(int conversationId, string userId);
    }
}
