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
            return await ServiceResult<Conversation>.ExecuteWithTransactionAsync(async () =>
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
                
                _logger.LogInformation("New conversation started for user {UserId}", userId);
                return ServiceResult<Conversation>.Success(conversation);
            }, _unitOfWork, _logger, $"Error starting new conversation for user {userId}");
        }

        public async Task<ServiceResult<IEnumerable<Conversation>>> GetUserConversationsAsync(string userId, bool includeBranches = false)
        {
            return await ServiceResult<IEnumerable<Conversation>>.ExecuteWithErrorHandlingAsync(async () =>
            {
                var conversations = await _unitOfWork.ConversationRepository.GetUserConversationsAsync(userId, includeBranches);
                return ServiceResult<IEnumerable<Conversation>>.Success(conversations);
            }, _unitOfWork, _logger, $"Error getting conversations for user {userId}");
        }

        public async Task<ServiceResult<Conversation>> GetConversationWithBranchesAsync(Guid conversationId, string userId)
        {
            return await ServiceResult<Conversation>.ExecuteWithErrorHandlingAsync(async () =>
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
            }, _unitOfWork,_logger, $"Error getting conversation {conversationId} for user {userId}");
        }

        public async Task<ServiceResult<bool>> EndConversationAsync(Guid conversationId, string userId)
        {
            return await ServiceResult<bool>.ExecuteWithTransactionAsync(async () =>
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
            }, _unitOfWork, _logger, $"Error ending conversation {conversationId} for user {userId}");
        }

        public async Task<ServiceResult<Conversation>> GetOrCreateActiveConversationAsync(string userId, bool isPaidUser)
        {
            return await ServiceResult<Conversation>.ExecuteWithErrorHandlingAsync(async () =>
            {
                var activeConversations = await _unitOfWork.ConversationRepository.GetActiveConversationsAsync(userId);
                var latestConversation = activeConversations.FirstOrDefault();

                if (latestConversation != null)
                {
                    return ServiceResult<Conversation>.Success(latestConversation);
                }

                // Create new conversation if none exists
                return await StartNewConversationAsync(userId, isPaidUser);
            }, _unitOfWork,_logger, $"Error getting or creating active conversation for user {userId}");
        }

        public async Task<ServiceResult<bool>> ValidateConversationAccessAsync(Guid conversationId, string userId)
        {
            return await ServiceResult<bool>.ExecuteWithErrorHandlingAsync(async () =>
            {
                var hasAccess = await _unitOfWork.ConversationRepository.IsConversationOwnedByUserAsync(conversationId, userId);
                return ServiceResult<bool>.Success(hasAccess);
            }, _unitOfWork,_logger, $"Error validating access to conversation {conversationId} for user {userId}");
        }

        #region IReadService / IWriteService Implementation

        public async Task<ServiceResult<ConversationResponseDTO>> CreateAsync(ConversationCreateDTO createDto)
        {
            return await ServiceResult<ConversationResponseDTO>.ExecuteWithTransactionAsync(async () =>
            {
                var entity = ConversationMappingService.ToEntity(createDto);
                await _unitOfWork.ConversationRepository.AddAsync(entity);

                var dto = ConversationMappingService.ToResponseDTO(entity);
                return ServiceResult<ConversationResponseDTO>.Success(dto, "Conversation created");
            }, _unitOfWork, _logger, $"Error creating conversation for user {createDto.UserId}");
        }

        public async Task<ServiceResult<ConversationResponseDTO>> UpdateAsync(Guid id, ConversationUpdateDTO updateDto)
        {
            return await ServiceResult<ConversationResponseDTO>.ExecuteWithTransactionAsync(async () =>
            {
                var entity = await _unitOfWork.ConversationRepository.GetByIdAsync(id);
                if (entity == null)
                {
                    return ServiceResult<ConversationResponseDTO>.Failure("Conversation not found");
                }

                ConversationMappingService.UpdateEntity(entity, updateDto);
                await _unitOfWork.ConversationRepository.UpdateAsync(entity);

                var dto = ConversationMappingService.ToResponseDTO(entity);
                return ServiceResult<ConversationResponseDTO>.Success(dto, "Conversation updated");
            }, _unitOfWork, _logger, $"Error updating conversation {id}");
        }

        public async Task<ServiceResult<IEnumerable<ConversationResponseDTO>>> GetAllAsync()
        {
            return await ServiceResult<IEnumerable<ConversationResponseDTO>>.ExecuteWithErrorHandlingAsync(async () =>
            {
                var conversations = await _unitOfWork.ConversationRepository.GetAllAsync();
                var dtos = conversations.Select(ConversationMappingService.ToResponseDTO);
                return ServiceResult<IEnumerable<ConversationResponseDTO>>.Success(dtos);
            }, _unitOfWork,_logger, "Error getting all conversations");
        }

        public async Task<ServiceResult<ConversationResponseDTO>> GetByIdAsync(Guid id)
        {
            return await ServiceResult<ConversationResponseDTO>.ExecuteWithErrorHandlingAsync(async () =>
            {
                var entity = await _unitOfWork.ConversationRepository.GetByIdAsync(id);
                if (entity == null)
                    return ServiceResult<ConversationResponseDTO>.Failure("Conversation not found");

                var dto = ConversationMappingService.ToResponseDTO(entity);
                return ServiceResult<ConversationResponseDTO>.Success(dto);
            }, _unitOfWork,_logger, $"Error getting conversation {id}");
        }

        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            return await ServiceResult<bool>.ExecuteWithTransactionAsync(async () =>
            {
                var result = await _unitOfWork.ConversationRepository.DeleteAsync(id);
                if (!result)
                {
                    return ServiceResult<bool>.Failure("Conversation not found");
                }

                return ServiceResult<bool>.Success(true, "Conversation deleted");
            }, _unitOfWork, _logger, $"Error deleting conversation {id}");
        }

        #endregion
    }
}
