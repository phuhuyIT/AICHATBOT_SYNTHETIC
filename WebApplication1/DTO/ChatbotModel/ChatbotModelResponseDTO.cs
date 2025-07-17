namespace WebApplication1.DTO.ChatbotModel
{
    public class ChatbotModelResponseDTO
    {
        public int Id { get; set; }
        public string? ModelName { get; set; }
        public string? PricingTier { get; set; }
        public bool IsAvailableForPaidUsers { get; set; }
        public bool IsActive { get; set; }
    }
}
