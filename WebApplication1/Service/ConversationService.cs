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
        private readonly ILogger<ConversationService> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public ConversationService(ILogger<ConversationService> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
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
                var entity = GenericMappingService.MapToEntity<ConversationCreateDTO, Conversation>(createDto);
                // Set specific properties that need special handling
                entity.StartedAt = DateTime.UtcNow;
                
                await _unitOfWork.ConversationRepository.AddAsync(entity);
                
                var dto = GenericMappingService.MapToResponseDTO<Conversation, ConversationResponseDTO>(entity);
                return ServiceResult<ConversationResponseDTO>.Success(dto, "Conversation created successfully");
            }, _unitOfWork, _logger, "Error creating conversation");
        }

        public async Task<ServiceResult<IEnumerable<ConversationResponseDTO>>> GetAllAsync()
        {
            return await ServiceResult<IEnumerable<ConversationResponseDTO>>.ExecuteWithErrorHandlingAsync(async () =>
            {
                var conversations = await _unitOfWork.ConversationRepository.GetAllAsync();
                var dtos = conversations.Select(c => GenericMappingService.MapToResponseDTO<Conversation, ConversationResponseDTO>(c)).ToList();
                return ServiceResult<IEnumerable<ConversationResponseDTO>>.Success(dtos);
            }, _unitOfWork, _logger, "Error retrieving all conversations");
        }

        public async Task<ServiceResult<ConversationResponseDTO>> UpdateAsync(Guid id, ConversationUpdateDTO updateDto)
        {
            // ...existing validation code...

            return await ServiceResult<ConversationResponseDTO>.ExecuteWithTransactionAsync(async () =>
            {
                var entity = await _unitOfWork.ConversationRepository.GetByIdAsync(id);
                if (entity == null)
                {
                    return ServiceResult<ConversationResponseDTO>.Failure("Conversation not found");
                }

                GenericMappingService.UpdateEntityFromDTO(updateDto, entity);
                await _unitOfWork.ConversationRepository.UpdateAsync(entity);
                
                var dto = GenericMappingService.MapToResponseDTO<Conversation, ConversationResponseDTO>(entity);
                return ServiceResult<ConversationResponseDTO>.Success(dto, "Conversation updated successfully");
            }, _unitOfWork, _logger, $"Error updating conversation {id}");
        }

        public async Task<ServiceResult<IEnumerable<ConversationResponseDTO>>> GetConversationsByUserIdAsync(string userId)
        {
            return await ServiceResult<IEnumerable<ConversationResponseDTO>>.ExecuteWithErrorHandlingAsync(async () =>
            {
                // Use the correct method name that exists in the repository
                var conversations = await _unitOfWork.ConversationRepository.GetUserConversationsAsync(userId);
                var dtos = conversations.Select(c => GenericMappingService.MapToResponseDTO<Conversation, ConversationResponseDTO>(c)).ToList();
                return ServiceResult<IEnumerable<ConversationResponseDTO>>.Success(dtos);
            }, _unitOfWork, _logger, $"Error getting conversations for user {userId}");
        }

        public async Task<ServiceResult<ConversationResponseDTO>> GetByIdAsync(Guid id)
        {
            return await ServiceResult<ConversationResponseDTO>.ExecuteWithErrorHandlingAsync(async () =>
            {
                var entity = await _unitOfWork.ConversationRepository.GetByIdAsync(id);
                if (entity == null)
                    return ServiceResult<ConversationResponseDTO>.Failure("Conversation not found");

                var dto = GenericMappingService.MapToResponseDTO<Conversation, ConversationResponseDTO>(entity);
                return ServiceResult<ConversationResponseDTO>.Success(dto);
            }, _unitOfWork, _logger, $"Error getting conversation {id}");
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

        #region Soft Delete Methods

        public async Task<ServiceResult<bool>> SoftDeleteConversationAsync(Guid conversationId, string userId)
        {
            return await ServiceResult<bool>.ExecuteWithTransactionAsync(async () =>
            {
                // Validate user access
                var hasAccess = await _unitOfWork.ConversationRepository.IsConversationOwnedByUserAsync(conversationId, userId);
                if (!hasAccess)
                {
                    return ServiceResult<bool>.Failure("Access denied or conversation not found");
                }

                var result = await _unitOfWork.ConversationRepository.SoftDeleteAsync(conversationId);
                if (!result)
                {
                    return ServiceResult<bool>.Failure("Conversation not found");
                }

                _logger.LogInformation("Conversation {ConversationId} soft deleted by user {UserId}", conversationId, userId);
                return ServiceResult<bool>.Success(true, "Conversation deleted");
            }, _unitOfWork, _logger, $"Error soft deleting conversation {conversationId}");
        }

        public async Task<ServiceResult<bool>> RestoreConversationAsync(Guid conversationId, string userId)
        {
            return await ServiceResult<bool>.ExecuteWithTransactionAsync(async () =>
            {
                // Validate user access
                var hasAccess = await _unitOfWork.ConversationRepository.IsConversationOwnedByUserAsync(conversationId, userId);
                if (!hasAccess)
                {
                    return ServiceResult<bool>.Failure("Access denied or conversation not found");
                }

                var result = await _unitOfWork.ConversationRepository.RestoreAsync(conversationId);
                if (!result)
                {
                    return ServiceResult<bool>.Failure("Conversation not found");
                }

                _logger.LogInformation("Conversation {ConversationId} restored by user {UserId}", conversationId, userId);
                return ServiceResult<bool>.Success(true, "Conversation restored");
            }, _unitOfWork, _logger, $"Error restoring conversation {conversationId}");
        }

        public async Task<ServiceResult<IEnumerable<Conversation>>> GetDeletedConversationsAsync(string userId)
        {
            return await ServiceResult<IEnumerable<Conversation>>.ExecuteWithErrorHandlingAsync(async () =>
            {
                var conversations = await _unitOfWork.ConversationRepository.GetDeletedUserConversationsAsync(userId);
                return ServiceResult<IEnumerable<Conversation>>.Success(conversations);
            }, _unitOfWork, _logger, $"Error getting deleted conversations for user {userId}");
        }

        #endregion

        #region IWriteService Soft Delete Methods Implementation

        public async Task<ServiceResult<bool>> SoftDeleteAsync(Guid id)
        {
            return await ServiceResult<bool>.ExecuteWithTransactionAsync(async () =>
            {
                var result = await _unitOfWork.ConversationRepository.SoftDeleteAsync(id);
                if (!result)
                {
                    return ServiceResult<bool>.Failure("Conversation not found");
                }

                _logger.LogInformation("Conversation {ConversationId} soft deleted", id);
                return ServiceResult<bool>.Success(true, "Conversation deleted");
            }, _unitOfWork, _logger, $"Error soft deleting conversation {id}");
        }

        public async Task<ServiceResult<bool>> RestoreAsync(Guid id)
        {
            return await ServiceResult<bool>.ExecuteWithTransactionAsync(async () =>
            {
                var result = await _unitOfWork.ConversationRepository.RestoreAsync(id);
                if (!result)
                {
                    return ServiceResult<bool>.Failure("Conversation not found");
                }

                _logger.LogInformation("Conversation {ConversationId} restored", id);
                return ServiceResult<bool>.Success(true, "Conversation restored");
            }, _unitOfWork, _logger, $"Error restoring conversation {id}");
        }

        public async Task<ServiceResult<IEnumerable<ConversationResponseDTO>>> GetDeletedAsync()
        {
            return await ServiceResult<IEnumerable<ConversationResponseDTO>>.ExecuteWithErrorHandlingAsync(async () =>
            {
                // Use GetAllIncludingDeletedAsync and filter for deleted items
                var allConversations = await _unitOfWork.ConversationRepository.GetAllIncludingDeletedAsync();
                var deletedConversations = allConversations.Where(c => !c.IsActive);
                var dtos = deletedConversations.Select(c => GenericMappingService.MapToResponseDTO<Conversation, ConversationResponseDTO>(c)).ToList();
                return ServiceResult<IEnumerable<ConversationResponseDTO>>.Success(dtos);
            }, _unitOfWork, _logger, "Error retrieving deleted conversations");
        }

        #endregion
    }
}
