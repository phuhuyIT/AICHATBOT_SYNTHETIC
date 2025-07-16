using System.ComponentModel.DataAnnotations;

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
    
    public class ApiKeyCreateDTO
    {
        [Required]
        public string ApiKeyValue { get; set; } = null!;
        
        public string? Description { get; set; }
        
        public bool IsActive { get; set; } = true;
    }
}

