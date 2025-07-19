using System.ComponentModel.DataAnnotations;
using WebApplication1.DTO.ApiKey;

namespace WebApplication1.DTO.ChatbotModel
{
    public class ChatbotModelUpdateDTO
    {
        [Required]
        public Guid Id { get; set; }
        
        [Required]
        public string ModelName { get; set; } = null!;
        
        public string? PricingTier { get; set; }
        
        public bool IsAvailableForPaidUsers { get; set; }
        
        public List<ApiKeyUpdateDTO>? ApiKeys { get; set; }
    }
    
}
