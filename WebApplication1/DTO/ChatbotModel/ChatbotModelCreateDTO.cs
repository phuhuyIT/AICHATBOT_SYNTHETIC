using System.ComponentModel.DataAnnotations;
using WebApplication1.DTO.ApiKey;

namespace WebApplication1.DTO.ChatbotModel
{
    public class ChatbotModelCreateDTO
    {
        [Required]
        public string ModelName { get; set; } = null!;
        
        public string? PricingTier { get; set; }
        
        public bool IsAvailableForPaidUsers { get; set; }
        
        public List<ApiKeyCreateDTO>? ApiKeys { get; set; }
    }
}

