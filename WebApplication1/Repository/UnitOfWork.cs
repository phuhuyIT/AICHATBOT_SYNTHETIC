using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Repository.Interface;

namespace WebApplication1.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IDbContextTransaction? _transaction;
        private bool _disposed = false;

        private IConversationRepository? _conversationRepository;
        private IConversationBranchRepository? _conversationBranchRepository;
        private IMessageRepository? _messageRepository;
        private IChatbotModelsRepository? _chatbotModelsRepository;
        private ITemplateRepository? _templateRepository;

        public UnitOfWork(ApplicationDbContext context,
            IConversationRepository conversationRepository,
            IConversationBranchRepository conversationBranchRepository,
            IMessageRepository messageRepository,
            IChatbotModelsRepository chatbotModelsRepository,
            ITemplateRepository templateRepository)
        {
            _context = context;
            _conversationRepository = conversationRepository;
            _conversationBranchRepository = conversationBranchRepository;
            _messageRepository = messageRepository;
            _chatbotModelsRepository = chatbotModelsRepository;
            _templateRepository = templateRepository;
        }

        public IConversationRepository ConversationRepository => _conversationRepository ??= 
            new ConversationRepository(_context);

        public IConversationBranchRepository ConversationBranchRepository => _conversationBranchRepository ??= 
            new ConversationBranchRepository(_context);

        public IMessageRepository MessageRepository => _messageRepository ??= 
            new MessageRepository(_context);

        public IGenericRepository<ChatbotModel> ChatbotModelsRepository => _chatbotModelsRepository ??= 
            new ChatbotModelsRepository(_context);

        public ITemplateRepository TemplateRepository => _templateRepository!;

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _transaction?.Dispose();
                _context?.Dispose();
                _disposed = true;
            }
        }
    }
}
