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

        public async Task<IEnumerable<Conversation>> GetUserConversationsAsync(string userId, bool includeMessages = false)
        {
            var query = _dbSet.Where(c => c.UserId == userId && c.IsActive);

            if (includeMessages)
            {
                query = query.Include(c => c.Messages.Where(m => m.IsActive))
                           .ThenInclude(m => m.ModifiedMessages);
            }

            return await query.OrderByDescending(c => c.StartedAt).ToListAsync();
        }

        public async Task<Conversation?> GetConversationWithMessagesAsync(int conversationId)
        {
            return await _dbSet
                .Include(c => c.Messages.Where(m => m.IsActive))
                .ThenInclude(m => m.ModifiedMessages)
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.ConversationId == conversationId && c.IsActive);
        }

        public async Task<IEnumerable<Conversation>> GetActiveConversationsAsync(string userId)
        {
            return await _dbSet
                .Where(c => c.UserId == userId && c.IsActive && c.EndedAt == null)
                .OrderByDescending(c => c.StartedAt)
                .ToListAsync();
        }

        public async Task<bool> DeactivateConversationAsync(int conversationId)
        {
            var conversation = await _dbSet.FindAsync(conversationId);
            if (conversation == null) return false;

            conversation.IsActive = false;
            conversation.EndedAt = DateTime.UtcNow;
            conversation.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Conversation?> GetLatestConversationAsync(string userId)
        {
            return await _dbSet
                .Where(c => c.UserId == userId && c.IsActive)
                .OrderByDescending(c => c.StartedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> IsConversationOwnedByUserAsync(int conversationId, string userId)
        {
            return await _dbSet
                .AnyAsync(c => c.ConversationId == conversationId && c.UserId == userId);
        }
    }
}
