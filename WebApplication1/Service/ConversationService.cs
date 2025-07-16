using WebApplication1.DTO;
using WebApplication1.Models;
using WebApplication1.Repository.Interface;
using WebApplication1.Service.Interface;

namespace WebApplication1.Service
{
    public class ConversationService : IConversationService
    {
        private readonly IConversationRepository _conversationRepository;
        private readonly ILogger<ConversationService> _logger;

        public ConversationService(IConversationRepository conversationRepository, ILogger<ConversationService> logger)
        {
            _conversationRepository = conversationRepository;
            _logger = logger;
        }

        public async Task<ServiceResult<Conversation>> StartNewConversationAsync(string userId, bool isPaidUser)
        {
            try
            {
                var conversation = new Conversation
                {
                    UserId = userId,
                    StartedAt = DateTime.UtcNow,
                    IsPaidUser = isPaidUser,
                    IsActive = true,
                    UpdatedAt = DateTime.UtcNow
                };

                await _conversationRepository.AddAsync(conversation);
                
                _logger.LogInformation("New conversation started for user {UserId}", userId);
                return ServiceResult<Conversation>.Success(conversation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting new conversation for user {UserId}", userId);
                return ServiceResult<Conversation>.Failure("Failed to start new conversation");
            }
        }

        public async Task<ServiceResult<IEnumerable<Conversation>>> GetUserConversationsAsync(string userId, bool includeMessages = false)
        {
            try
            {
                var conversations = await _conversationRepository.GetUserConversationsAsync(userId, includeMessages);
                return ServiceResult<IEnumerable<Conversation>>.Success(conversations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversations for user {UserId}", userId);
                return ServiceResult<IEnumerable<Conversation>>.Failure("Failed to retrieve conversations");
            }
        }

        public async Task<ServiceResult<Conversation>> GetConversationWithMessagesAsync(int conversationId, string userId)
        {
            try
            {
                // Validate access
                var hasAccess = await _conversationRepository.IsConversationOwnedByUserAsync(conversationId, userId);
                if (!hasAccess)
                {
                    return ServiceResult<Conversation>.Failure("Access denied to conversation");
                }

                var conversation = await _conversationRepository.GetConversationWithMessagesAsync(conversationId);
                if (conversation == null)
                {
                    return ServiceResult<Conversation>.Failure("Conversation not found");
                }

                return ServiceResult<Conversation>.Success(conversation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation {ConversationId} for user {UserId}", conversationId, userId);
                return ServiceResult<Conversation>.Failure("Failed to retrieve conversation");
            }
        }

        public async Task<ServiceResult<bool>> EndConversationAsync(int conversationId, string userId)
        {
            try
            {
                var hasAccess = await _conversationRepository.IsConversationOwnedByUserAsync(conversationId, userId);
                if (!hasAccess)
                {
                    return ServiceResult<bool>.Failure("Access denied to conversation");
                }

                var result = await _conversationRepository.DeactivateConversationAsync(conversationId);
                if (!result)
                {
                    return ServiceResult<bool>.Failure("Conversation not found");
                }

                _logger.LogInformation("Conversation {ConversationId} ended by user {UserId}", conversationId, userId);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ending conversation {ConversationId} for user {UserId}", conversationId, userId);
                return ServiceResult<bool>.Failure("Failed to end conversation");
            }
        }

        public async Task<ServiceResult<Conversation>> GetOrCreateActiveConversationAsync(string userId, bool isPaidUser)
        {
            try
            {
                var activeConversations = await _conversationRepository.GetActiveConversationsAsync(userId);
                var latestConversation = activeConversations.FirstOrDefault();

                if (latestConversation != null)
                {
                    return ServiceResult<Conversation>.Success(latestConversation);
                }

                // Create new conversation if none exists
                return await StartNewConversationAsync(userId, isPaidUser);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting or creating active conversation for user {UserId}", userId);
                return ServiceResult<Conversation>.Failure("Failed to get or create conversation");
            }
        }

        public async Task<ServiceResult<bool>> ValidateConversationAccessAsync(int conversationId, string userId)
        {
            try
            {
                var hasAccess = await _conversationRepository.IsConversationOwnedByUserAsync(conversationId, userId);
                return ServiceResult<bool>.Success(hasAccess);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating access to conversation {ConversationId} for user {UserId}", conversationId, userId);
                return ServiceResult<bool>.Failure("Failed to validate access");
            }
        }

        // Implementation of IService<Conversation> methods
        public async Task<ServiceResult<Conversation>> AddAsync(Conversation entity)
        {
            try
            {
                await _conversationRepository.AddAsync(entity);
                return ServiceResult<Conversation>.Success(entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding conversation");
                return ServiceResult<Conversation>.Failure("Failed to add conversation");
            }
        }

        public async Task<ServiceResult<bool>> DeleteAsync(int id)
        {
            try
            {
                var result = await _conversationRepository.DeleteAsync(id);
                return ServiceResult<bool>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting conversation {Id}", id);
                return ServiceResult<bool>.Failure("Failed to delete conversation");
            }
        }

        public async Task<ServiceResult<IEnumerable<Conversation>>> GetAllAsync()
        {
            try
            {
                var conversations = await _conversationRepository.GetAllAsync();
                return ServiceResult<IEnumerable<Conversation>>.Success(conversations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all conversations");
                return ServiceResult<IEnumerable<Conversation>>.Failure("Failed to retrieve conversations");
            }
        }

        public async Task<ServiceResult<Conversation>> GetByIdAsync(int id)
        {
            try
            {
                var conversation = await _conversationRepository.GetByIdAsync(id);
                if (conversation == null)
                {
                    return ServiceResult<Conversation>.Failure("Conversation not found");
                }
                return ServiceResult<Conversation>.Success(conversation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation {Id}", id);
                return ServiceResult<Conversation>.Failure("Failed to retrieve conversation");
            }
        }

        public async Task<ServiceResult<Conversation>> UpdateAsync(Conversation entity)
        {
            try
            {
                await _conversationRepository.UpdateAsync(entity);
                return ServiceResult<Conversation>.Success(entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating conversation");
                return ServiceResult<Conversation>.Failure("Failed to update conversation");
            }
        }
    }
}
