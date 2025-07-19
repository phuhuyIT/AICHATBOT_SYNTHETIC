using WebApplication1.DTO.Conversation;
using WebApplication1.Models;

namespace WebApplication1.Service.MappingService
{
    public static class ConversationMappingService
    {
        public static Conversation ToEntity(ConversationCreateDTO dto)
        {
            return new Conversation
            {
                UserId = dto.UserId,
                StartedAt = DateTime.UtcNow,
                IsPaidUser = dto.IsPaidUser,
                IsActive = true,
                UpdatedAt = DateTime.UtcNow
            };
        }

        public static void UpdateEntity(Conversation entity, ConversationUpdateDTO dto)
        {
            entity.IsActive = dto.IsActive;
            entity.EndedAt = dto.EndedAt;
            entity.UpdatedAt = DateTime.UtcNow;
        }

        public static ConversationResponseDTO ToResponseDTO(Conversation entity)
        {
            return new ConversationResponseDTO
            {
                ConversationId = entity.ConversationId,
                UserId = entity.UserId,
                StartedAt = entity.StartedAt,
                EndedAt = entity.EndedAt,
                IsPaidUser = entity.IsPaidUser,
                IsActive = entity.IsActive,
                UpdatedAt = entity.UpdatedAt
            };
        }

        public static List<ConversationResponseDTO> ToResponseDTOList(IEnumerable<Conversation> entities)
        {
            return entities.Select(ToResponseDTO).ToList();
        }
    }
}
