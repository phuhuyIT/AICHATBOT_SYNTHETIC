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

        public async Task<IEnumerable<Message>> GetBranchMessagesAsync(Guid branchId)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(m => m.BranchId == branchId)
                .Include(m => m.ParentMessage)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<Message?> GetLatestMessageAsync(Guid branchId)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(m => m.BranchId == branchId)
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Message>> GetMessagesByModelAsync(string modelName)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(m => m.ModelUsed == modelName)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetMessageCountByBranchAsync(Guid branchId)
        {
            return await _dbSet
                .AsNoTracking()
                .CountAsync(m => m.BranchId == branchId);
        }

        public async Task<IEnumerable<Message>> GetPaginatedMessagesAsync(Guid branchId, int pageNumber, int pageSize)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(m => m.BranchId == branchId)
                .OrderBy(m => m.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<Message>> GetMessagesByRoleAsync(Guid branchId, string role)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(m => m.BranchId == branchId && m.Role == role)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }

        // Legacy method for backward compatibility - converts conversation to branch operations
        public async Task<IEnumerable<Message>> GetConversationMessagesAsync(Guid conversationId)
        {
            // Get all branches for the conversation and return their messages
            var branchIds = await _context.ConversationBranches
                .Where(b => b.ConversationId == conversationId)
                .Select(b => b.BranchId)
                .ToListAsync();

            return await _dbSet
                .AsNoTracking()
                .Where(m => branchIds.Contains(m.BranchId))
                .Include(m => m.ParentMessage)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Message>> GetRecentMessagesAsync(Guid branchId, int count = 10)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(m => m.BranchId == branchId)
                .OrderByDescending(m => m.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<bool> BulkDeactivateMessagesAsync(IEnumerable<Guid> messageIds)
        {
            var messages = await _dbSet
                .Where(m => messageIds.Contains(m.MessageId))
                .ToListAsync();

            foreach (var message in messages)
            {
                message.UpdatedAt = DateTime.UtcNow;
            }

            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<IEnumerable<Message>> GetMessagesByDateRangeAsync(Guid branchId, DateTime startDate, DateTime endDate)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(m => m.BranchId == branchId && 
                           m.CreatedAt >= startDate && 
                           m.CreatedAt <= endDate)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> DeleteMessagesByBranchAsync(Guid branchId)
        {
            var messages = await _dbSet
                .Where(m => m.BranchId == branchId)
                .ToListAsync();

            foreach (var message in messages)
            {
                message.UpdatedAt = DateTime.UtcNow;
            }

            return await _context.SaveChangesAsync() > 0;
        }
    }
}
