﻿using WebApplication1.Models;

namespace WebApplication1.Repository.Interface
{
    public interface IConversationRepository : IGenericRepository<Conversation>
    {
        Task<IEnumerable<Conversation>> GetUserConversationsAsync(string userId, bool includeMessages = false);
        Task<Conversation?> GetConversationWithMessagesAsync(int conversationId);
        Task<IEnumerable<Conversation>> GetActiveConversationsAsync(string userId);
        Task<bool> DeactivateConversationAsync(int conversationId);
        Task<Conversation?> GetLatestConversationAsync(string userId);
        Task<bool> IsConversationOwnedByUserAsync(int conversationId, string userId);
    }
}
