using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Repository.Interface;

namespace WebApplication1.Repository
{
    public class ConversationRepository : GenericRepository<Conversation>, IConversationRepository
    {
        public ConversationRepository(ApplicationDbContext context) : base(context)
        {
        }

        public virtual async Task<IEnumerable<Conversation>> GetUserConversationsAsync(string userId, bool includeMessages = false)
        {
            var query = _dbSet.AsNoTracking().Where(c => c.UserId == userId && c.IsActive);

            if (includeMessages)
            {
                query = query.Include(c => c.Messages.Where(m => m.IsActive))
                           .ThenInclude(m => m.ModifiedMessages);
            }

            return await query.OrderByDescending(c => c.StartedAt).ToListAsync();
        }

        public virtual async Task<Conversation?> GetConversationWithMessagesAsync(int conversationId)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(c => c.Messages.Where(m => m.IsActive))
                .ThenInclude(m => m.ModifiedMessages)
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.ConversationId == conversationId && c.IsActive);
        }

        public virtual async Task<IEnumerable<Conversation>> GetActiveConversationsAsync(string userId)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(c => c.UserId == userId && c.IsActive && c.EndedAt == null)
                .OrderByDescending(c => c.StartedAt)
                .ToListAsync();
        }

        public virtual async Task<bool> DeactivateConversationAsync(int conversationId)
        {
            var conversation = await _dbSet.FindAsync(conversationId);
            if (conversation == null) return false;

            conversation.IsActive = false;
            conversation.EndedAt = DateTime.UtcNow;
            conversation.UpdatedAt = DateTime.UtcNow;

            return true;
        }

        public virtual async Task<Conversation?> GetLatestConversationAsync(string userId)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(c => c.UserId == userId && c.IsActive)
                .OrderByDescending(c => c.StartedAt)
                .FirstOrDefaultAsync();
        }

        public virtual async Task<bool> IsConversationOwnedByUserAsync(int conversationId, string userId)
        {
            return await _dbSet
                .AsNoTracking()
                .AnyAsync(c => c.ConversationId == conversationId && c.UserId == userId);
        }

        public virtual async Task<IEnumerable<Conversation>> GetPaginatedUserConversationsAsync(
            string userId, int pageNumber, int pageSize, bool includeMessages = false)
        {
            var query = _dbSet.AsNoTracking().Where(c => c.UserId == userId && c.IsActive);

            if (includeMessages)
            {
                query = query.Include(c => c.Messages.Where(m => m.IsActive))
                           .ThenInclude(m => m.ModifiedMessages);
            }

            return await query
                .OrderByDescending(c => c.StartedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public virtual async Task<int> GetUserConversationCountAsync(string userId)
        {
            return await _dbSet
                .AsNoTracking()
                .CountAsync(c => c.UserId == userId && c.IsActive);
        }
    }
}
