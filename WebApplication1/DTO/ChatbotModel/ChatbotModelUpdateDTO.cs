using System.ComponentModel.DataAnnotations;

namespace WebApplication1.DTO.ChatbotModel
{
    public class ChatbotModelUpdateDTO
    {
        [Required]
        public int Id { get; set; }
        
        [Required]
        public string ModelName { get; set; } = null!;
        
        public string? PricingTier { get; set; }
        
        public bool IsAvailableForPaidUsers { get; set; }
        
        public List<ApiKeyUpdateDTO>? ApiKeys { get; set; }
    }
    
    public class ApiKeyUpdateDTO
    {
        public int? Id { get; set; } // null for new API keys
        
        [Required]
        public string ApiKeyValue { get; set; } = null!;
        
        public string? Description { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public bool IsDeleted { get; set; } = false; // for marking API keys to be deleted
    }
}

