using Microsoft.EntityFrameworkCore;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.Repository.Interface;
using WebApplication1.Service;

namespace WebApplication1.Repository
{
    public class CachedConversationRepository : ConversationRepository
    {
        private readonly ICacheService _cacheService;
        private readonly ILogger<CachedConversationRepository> _logger;
        
        // Cache key prefixes
        private const string UserConversationsPrefix = "user_conversations_";
        private const string ConversationPrefix = "conversation_";
        private const string ActiveConversationsPrefix = "active_conversations_";
        
        public CachedConversationRepository(ApplicationDbContext context, ICacheService cacheService, 
            ILogger<CachedConversationRepository> logger) 
            : base(context)
        {
            _cacheService = cacheService;
            _logger = logger;
        }
        
        public override async Task<IEnumerable<Conversation>> GetUserConversationsAsync(string userId, bool includeBranches = false)
        {
            var cacheKey = $"{UserConversationsPrefix}{userId}_{includeBranches}";
            
            // Try to get from cache
            var cachedResult = _cacheService.Get<IEnumerable<Conversation>>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogInformation("Cache hit for user conversations: {UserId}", userId);
                return cachedResult;
            }
            
            // Get from database
            var result = await base.GetUserConversationsAsync(userId, includeBranches);
            
            // Cache the result
            _cacheService.Set(cacheKey, result, TimeSpan.FromMinutes(15));
            
            return result;
        }
        
        public override async Task<Conversation?> GetConversationWithBranchesAsync(Guid conversationId)
        {
            var cacheKey = $"{ConversationPrefix}{conversationId}_with_branches";
            
            // Try to get from cache
            var cachedResult = _cacheService.Get<Conversation?>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogInformation("Cache hit for conversation with branches: {ConversationId}", conversationId);
                return cachedResult;
            }
            
            // Get from database
            var result = await base.GetConversationWithBranchesAsync(conversationId);
            
            // Cache the result
            if (result != null)
            {
                _cacheService.Set(cacheKey, result, TimeSpan.FromMinutes(10));
            }
            
            return result;
        }
        
        public override async Task<IEnumerable<Conversation>> GetActiveConversationsAsync(string userId)
        {
            var cacheKey = $"{ActiveConversationsPrefix}{userId}";
            
            // Try to get from cache
            var cachedResult = _cacheService.Get<IEnumerable<Conversation>>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogInformation("Cache hit for active conversations: {UserId}", userId);
                return cachedResult;
            }
            
            // Get from database
            var result = await base.GetActiveConversationsAsync(userId);
            
            // Cache the result for a shorter time as active conversations change more frequently
            _cacheService.Set(cacheKey, result, TimeSpan.FromMinutes(5));
            
            return result;
        }
        
        public override async Task<Conversation?> GetLatestConversationAsync(string userId)
        {
            var cacheKey = $"{UserConversationsPrefix}{userId}_latest";
            
            // Try to get from cache
            var cachedResult = _cacheService.Get<Conversation?>(cacheKey);
            if (cachedResult != null)
            {
                _logger.LogInformation("Cache hit for latest conversation: {UserId}", userId);
                return cachedResult;
            }
            
            // Get from database
            var result = await base.GetLatestConversationAsync(userId);
            
            // Cache the result for a shorter time
            if (result != null)
            {
                _cacheService.Set(cacheKey, result, TimeSpan.FromMinutes(5));
            }
            
            return result;
        }
        
        // Override modification methods to invalidate cache
        public override async Task AddAsync(Conversation entity)
        {
            await base.AddAsync(entity);
            InvalidateUserCache(entity.UserId);
        }
        
        public override async Task UpdateAsync(Conversation entity)
        {
            await base.UpdateAsync(entity);
            InvalidateUserCache(entity.UserId);
            InvalidateConversationCache(entity.ConversationId);
        }
        
        public override async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await GetByIdAsync(id);
            if (entity != null)
            {
                InvalidateUserCache(entity.UserId);
                InvalidateConversationCache(id);
            }
            
            return await base.DeleteAsync(id);
        }
        
        public override async Task<bool> DeactivateConversationAsync(Guid conversationId)
        {
            var conversation = await GetByIdAsync(conversationId);
            if (conversation != null)
            {
                InvalidateUserCache(conversation.UserId);
                InvalidateConversationCache(conversationId);
            }
            
            return await base.DeactivateConversationAsync(conversationId);
        }
        
        // Helper methods to invalidate cache
        private void InvalidateUserCache(string userId)
        {
            _cacheService.Remove($"{UserConversationsPrefix}{userId}_True");
            _cacheService.Remove($"{UserConversationsPrefix}{userId}_False");
            _cacheService.Remove($"{UserConversationsPrefix}{userId}_latest");
            _cacheService.Remove($"{ActiveConversationsPrefix}{userId}");
        }
        
        private void InvalidateConversationCache(Guid conversationId)
        {
            _cacheService.Remove($"{ConversationPrefix}{conversationId}_with_branches");
        }
    }
}
