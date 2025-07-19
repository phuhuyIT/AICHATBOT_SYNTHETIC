using WebApplication1.DTO.ChatbotModel;
using WebApplication1.Models;

namespace WebApplication1.Service.MappingService
{
    public static class ChatbotModelMappingService
    {
        public static ChatbotModelResponseDTO? ToResponseDTO(ChatbotModel? entity)
        {
            if (entity == null) return null;
            return new ChatbotModelResponseDTO
            {
                Id = entity.Id,
                ModelName = entity.ModelName,
                PricingTier = entity.PricingTier,
                IsAvailableForPaidUsers = entity.IsAvailableForPaidUsers
            };
        }

        public static List<ChatbotModelResponseDTO> ToResponseDTOList(IEnumerable<ChatbotModel>? entities)
        {
            if (entities == null) return new List<ChatbotModelResponseDTO>();
            return entities.Select(e => ToResponseDTO(e)!).Where(d => d != null).ToList();
        }

        public static ChatbotModel ToEntity(ChatbotModelCreateDTO dto)
        {
            return new ChatbotModel
            {
                ModelName = dto.ModelName,
                PricingTier = dto.PricingTier,
                IsAvailableForPaidUsers = dto.IsAvailableForPaidUsers
            };
        }

        public static void UpdateEntity(ChatbotModel entity, ChatbotModelUpdateDTO dto)
        {
            entity.ModelName = dto.ModelName;
            entity.PricingTier = dto.PricingTier;
            entity.IsAvailableForPaidUsers = dto.IsAvailableForPaidUsers;
        }
    }
}
