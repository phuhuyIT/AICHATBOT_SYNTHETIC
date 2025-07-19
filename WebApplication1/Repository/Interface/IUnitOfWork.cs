using WebApplication1.Models;

namespace WebApplication1.Repository.Interface
{
    public interface IUnitOfWork : IDisposable
    {
        IConversationRepository ConversationRepository { get; }
        IConversationBranchRepository ConversationBranchRepository { get; }
        IMessageRepository MessageRepository { get; }
        IGenericRepository<ChatbotModel> ChatbotModelsRepository { get; }
        IGenericRepository<RefreshToken> RefreshTokenRepository { get; }
        ITemplateRepository TemplateRepository { get; }
        
        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
