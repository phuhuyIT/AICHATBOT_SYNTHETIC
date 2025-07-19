namespace WebApplication1.DTO.Conversation
{
    public class ConversationCreateDTO
    {
        public string UserId { get; set; } = string.Empty;
        public bool IsPaidUser { get; set; }
    }

    public class ConversationUpdateDTO
    {
        public Guid ConversationId { get; set; }
        public bool IsActive { get; set; }
        public DateTime? EndedAt { get; set; }
    }

    public class ConversationResponseDTO
    {
        public Guid ConversationId { get; set; }
        public string? UserId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public bool IsPaidUser { get; set; }
        public bool IsActive { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
