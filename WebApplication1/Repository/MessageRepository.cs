using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Repository.Interface;

namespace WebApplication1.Repository
{
    public class MessageRepository : GenericRepository<Message>, IMessageRepository
    {
        public MessageRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Message>> GetConversationMessagesAsync(int conversationId)
        {
            return await _dbSet
                .Where(m => m.ConversationId == conversationId && m.IsActive)
                .Include(m => m.ModifiedMessages)
                .OrderBy(m => m.MessageTimestamp)
                .ToListAsync();
        }

        public async Task<Message?> GetLatestMessageAsync(int conversationId)
        {
            return await _dbSet
                .Where(m => m.ConversationId == conversationId && m.IsActive)
                .OrderByDescending(m => m.MessageTimestamp)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Message>> GetMessagesByModelAsync(string modelName)
        {
            return await _dbSet
                .Where(m => m.ModelUsed == modelName && m.IsActive)
                .OrderByDescending(m => m.MessageTimestamp)
                .ToListAsync();
        }

        public async Task<int> GetMessageCountByConversationAsync(int conversationId)
        {
            return await _dbSet
                .CountAsync(m => m.ConversationId == conversationId && m.IsActive);
        }

        public async Task<IEnumerable<Message>> GetPaginatedMessagesAsync(int conversationId, int pageNumber, int pageSize)
        {
            return await _dbSet
                .Where(m => m.ConversationId == conversationId && m.IsActive)
                .OrderBy(m => m.MessageTimestamp)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Include(m => m.ModifiedMessages)
                .ToListAsync();
        }

        public async Task<bool> DeleteMessagesByConversationAsync(int conversationId)
        {
            var messages = await _dbSet
                .Where(m => m.ConversationId == conversationId)
                .ToListAsync();

            if (!messages.Any()) return false;

            foreach (var message in messages)
            {
                message.IsActive = false;
                message.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
