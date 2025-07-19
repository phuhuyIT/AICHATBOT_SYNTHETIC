using WebApplication1.Models;

namespace WebApplication1.Repository.Interface
{
    public interface IConversationBranchRepository : IGenericRepository<ConversationBranch>
    {
        Task<IEnumerable<ConversationBranch>> GetConversationBranchesAsync(Guid conversationId);
        Task<ConversationBranch?> GetMainBranchAsync(Guid conversationId);
        Task<IEnumerable<ConversationBranch>> GetChildBranchesAsync(Guid parentBranchId);
        Task<ConversationBranch?> GetBranchWithMessagesAsync(Guid branchId);
    }
}
