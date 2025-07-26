﻿using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Repository.Interface;

namespace WebApplication1.Repository
{
    public class ConversationBranchRepository : GenericRepository<ConversationBranch>, IConversationBranchRepository
    {
        public ConversationBranchRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<ConversationBranch>> GetConversationBranchesAsync(Guid conversationId)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(b => b.ConversationId == conversationId && b.IsActive)
                .Include(b => b.Messages.Where(m => m.IsActive))
                .OrderBy(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<ConversationBranch?> GetMainBranchAsync(Guid conversationId)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(b => b.ConversationId == conversationId && b.ParentBranchId == null && b.IsActive)
                .Include(b => b.Messages.Where(m => m.IsActive))
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<ConversationBranch>> GetChildBranchesAsync(Guid parentBranchId)
        {
            return await _dbSet
                .AsNoTracking()
                .Where(b => b.ParentBranchId == parentBranchId && b.IsActive)
                .Include(b => b.Messages.Where(m => m.IsActive))
                .OrderBy(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<ConversationBranch?> GetBranchWithMessagesAsync(Guid branchId)
        {
            return await _dbSet
                .AsNoTracking()
                .Include(b => b.Messages.Where(m => m.IsActive).OrderBy(m => m.CreatedAt))
                .Include(b => b.Conversation)
                .FirstOrDefaultAsync(b => b.BranchId == branchId && b.IsActive);
        }
    }
}
