using WebApplication1.DTO.ApiKey;
using WebApplication1.Models;

namespace WebApplication1.Service
{
    public static class ApiKeyMappingService
    {
        public static ChatBotApiKey ToEntity(ApiKeyCreateDTO dto)
        {
            return new ChatBotApiKey
            {
                ApiKey = dto.ApiKey,
                ServiceName = dto.ServiceName,
                Setting = dto.Setting,
                Usage = dto.Usage,
                ChatbotModelId = dto.ChatbotModelId,
                IsActive = dto.IsActive,
                UserId = dto.UserId,
                CreatedAt = DateTime.UtcNow
            };
        }

        public static void UpdateEntity(ChatBotApiKey entity, ApiKeyUpdateDTO dto)
        {
            entity.ApiKey = dto.ApiKey;
            entity.ServiceName = dto.ServiceName;
            entity.Setting = dto.Setting;
            entity.Usage = dto.Usage;
            entity.ChatbotModelId = dto.ChatbotModelId;
            entity.IsActive = dto.IsActive;
            entity.UserId = dto.UserId;
            entity.UpdatedAt = DateTime.UtcNow;
        }

        public static ApiKeyResponseDTO? ToResponseDTO(ChatBotApiKey? entity)
        {
            if (entity == null) return null;
            return new ApiKeyResponseDTO
            {
                ApiKeyId = entity.ApiKeyId,
                UserId = entity.UserId,
                ApiKey = entity.ApiKey,
                ServiceName = entity.ServiceName,
                CreatedAt = entity.CreatedAt,
                Setting = entity.Setting,
                Usage = entity.Usage,
                ChatbotModelId = entity.ChatbotModelId,
                IsActive = entity.IsActive,
                UpdatedAt = entity.UpdatedAt,
                ChatbotModelName = entity.ChatbotModel?.ModelName,
                UserName = entity.User?.UserName
            };
        }

        public static ApiKeyListDTO? ToListDTO(ChatBotApiKey? entity)
        {
            if (entity == null) return null;
            return new ApiKeyListDTO
            {
                ApiKeyId = entity.ApiKeyId,
                ApiKey = entity.ApiKey,
                ServiceName = entity.ServiceName,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt,
                ChatbotModelName = entity.ChatbotModel?.ModelName,
                Usage = entity.Usage
            };
        }

        public static List<ApiKeyResponseDTO> ToResponseDTOList(IEnumerable<ChatBotApiKey>? entities)
        {
            if (entities == null) return new List<ApiKeyResponseDTO>();
            return entities.Select(e => ToResponseDTO(e)!).Where(d => d != null).ToList();
        }

        public static List<ApiKeyListDTO> ToListDTOList(IEnumerable<ChatBotApiKey>? entities)
        {
            if (entities == null) return new List<ApiKeyListDTO>();
            return entities.Select(e => ToListDTO(e)!).Where(d => d != null).ToList();
        }
    }
}
