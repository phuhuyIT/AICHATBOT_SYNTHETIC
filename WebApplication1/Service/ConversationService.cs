using WebApplication1.DTO;
using WebApplication1.DTO.Conversation;
using WebApplication1.Models;
using WebApplication1.Repository.Interface;
using WebApplication1.Service.Interface;
using WebApplication1.Service.MappingService;

namespace WebApplication1.Service
{
    public class ConversationService : IConversationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ConversationService> _logger;

        public ConversationService(IUnitOfWork unitOfWork, ILogger<ConversationService> logger)
        {
            _unitOfWork = unitOfWork;
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

                await _unitOfWork.ConversationRepository.AddAsync(conversation);
                await _unitOfWork.SaveChangesAsync();
                
                _logger.LogInformation("New conversation started for user {UserId}", userId);
                return ServiceResult<Conversation>.Success(conversation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting new conversation for user {UserId}", userId);
                return ServiceResult<Conversation>.Failure("Failed to start new conversation");
            }
        }

        public async Task<ServiceResult<IEnumerable<Conversation>>> GetUserConversationsAsync(string userId, bool includeBranches = false)
        {
            try
            {
                var conversations = await _unitOfWork.ConversationRepository.GetUserConversationsAsync(userId, includeBranches);
                return ServiceResult<IEnumerable<Conversation>>.Success(conversations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversations for user {UserId}", userId);
                return ServiceResult<IEnumerable<Conversation>>.Failure("Failed to retrieve conversations");
            }
        }

        public async Task<ServiceResult<Conversation>> GetConversationWithBranchesAsync(Guid conversationId, string userId)
        {
            try
            {
                // Validate access
                var hasAccess = await _unitOfWork.ConversationRepository.IsConversationOwnedByUserAsync(conversationId, userId);
                if (!hasAccess)
                {
                    return ServiceResult<Conversation>.Failure("Access denied to conversation");
                }

                var conversation = await _unitOfWork.ConversationRepository.GetConversationWithBranchesAsync(conversationId);
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

        public async Task<ServiceResult<bool>> EndConversationAsync(Guid conversationId, string userId)
        {
            try
            {
                var hasAccess = await _unitOfWork.ConversationRepository.IsConversationOwnedByUserAsync(conversationId, userId);
                if (!hasAccess)
                {
                    return ServiceResult<bool>.Failure("Access denied to conversation");
                }

                var result = await _unitOfWork.ConversationRepository.DeactivateConversationAsync(conversationId);
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
                var activeConversations = await _unitOfWork.ConversationRepository.GetActiveConversationsAsync(userId);
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

        public async Task<ServiceResult<bool>> ValidateConversationAccessAsync(Guid conversationId, string userId)
        {
            try
            {
                var hasAccess = await _unitOfWork.ConversationRepository.IsConversationOwnedByUserAsync(conversationId, userId);
                return ServiceResult<bool>.Success(hasAccess);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating access to conversation {ConversationId} for user {UserId}", conversationId, userId);
                return ServiceResult<bool>.Failure("Failed to validate access");
            }
        }

        #region IReadService / IWriteService Implementation

        public async Task<ServiceResult<ConversationResponseDTO>> CreateAsync(ConversationCreateDTO createDto)
        {
            var entity = ConversationMappingService.ToEntity(createDto);
            try
            {
                await _unitOfWork.ConversationRepository.AddAsync(entity);
                await _unitOfWork.SaveChangesAsync();
                var dto = ConversationMappingService.ToResponseDTO(entity);
                return ServiceResult<ConversationResponseDTO>.Success(dto, "Conversation created");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating conversation for user {UserId}", createDto.UserId);
                return ServiceResult<ConversationResponseDTO>.Failure("Failed to create conversation");
            }
        }
        

        public async Task<ServiceResult<ConversationResponseDTO>> UpdateAsync(Guid id, ConversationUpdateDTO updateDto)
        {
            try
            {
                var entity = await _unitOfWork.ConversationRepository.GetByIdAsync(id);
                if (entity == null)
                    return ServiceResult<ConversationResponseDTO>.Failure("Conversation not found");

                ConversationMappingService.UpdateEntity(entity, updateDto);
                await _unitOfWork.ConversationRepository.UpdateAsync(entity);
                await _unitOfWork.SaveChangesAsync();
                var dto = ConversationMappingService.ToResponseDTO(entity);
                return ServiceResult<ConversationResponseDTO>.Success(dto, "Conversation updated");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating conversation {Id}", id);
                return ServiceResult<ConversationResponseDTO>.Failure("Failed to update conversation");
            }
        }

        public async Task<ServiceResult<IEnumerable<ConversationResponseDTO>>> GetAllAsync()
        {
            try
            {
                var conversations = await _unitOfWork.ConversationRepository.GetAllAsync();
                var dtos = conversations.Select(ConversationMappingService.ToResponseDTO);
                return ServiceResult<IEnumerable<ConversationResponseDTO>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all conversations");
                return ServiceResult<IEnumerable<ConversationResponseDTO>>.Failure("Failed to retrieve conversations");
            }
        }

        public async Task<ServiceResult<ConversationResponseDTO>> GetByIdAsync(Guid id)
        {
            try
            {
                var conversation = await _unitOfWork.ConversationRepository.GetByIdAsync(id);
                if (conversation == null)
                {
                    return ServiceResult<ConversationResponseDTO>.Failure("Conversation not found");
                }
                var dto = ConversationMappingService.ToResponseDTO(conversation);
                return ServiceResult<ConversationResponseDTO>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation {Id}", id);
                return ServiceResult<ConversationResponseDTO>.Failure("Failed to retrieve conversation");
            }
        }

        // DeleteAsync(int id) already exists below and fits the new interface – leave as is.

        #endregion

        // ---- Legacy entity-based CRUD methods kept for backward compatibility ----

        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            try
            {
                var result = await _unitOfWork.ConversationRepository.DeleteAsync(id);
                await _unitOfWork.SaveChangesAsync();
                return ServiceResult<bool>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting conversation {Id}", id);
                return ServiceResult<bool>.Failure("Failed to delete conversation");
            }
        }
    }
}
