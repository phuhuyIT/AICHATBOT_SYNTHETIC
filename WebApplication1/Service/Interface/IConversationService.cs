using WebApplication1.DTO;
using WebApplication1.DTO.Conversation;
using WebApplication1.Models;

namespace WebApplication1.Service.Interface
{
    public interface IConversationService : IReadService<ConversationResponseDTO>, IWriteService<ConversationCreateDTO, ConversationUpdateDTO, ConversationResponseDTO>
    {
        Task<ServiceResult<Conversation>> StartNewConversationAsync(string userId, bool isPaidUser);
        Task<ServiceResult<IEnumerable<Conversation>>> GetUserConversationsAsync(string userId, bool includeBranches = false);
        Task<ServiceResult<Conversation>> GetConversationWithBranchesAsync(Guid conversationId, string userId);
        Task<ServiceResult<bool>> EndConversationAsync(Guid conversationId, string userId);
        Task<ServiceResult<Conversation>> GetOrCreateActiveConversationAsync(string userId, bool isPaidUser);
        Task<ServiceResult<bool>> ValidateConversationAccessAsync(Guid conversationId, string userId);
        
        // Soft delete methods
        Task<ServiceResult<bool>> SoftDeleteConversationAsync(Guid conversationId, string userId);
        Task<ServiceResult<bool>> RestoreConversationAsync(Guid conversationId, string userId);
        Task<ServiceResult<IEnumerable<Conversation>>> GetDeletedConversationsAsync(string userId);
    }
}
