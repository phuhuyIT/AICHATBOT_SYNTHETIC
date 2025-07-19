namespace WebApplication1.DTO
{
    public class SendMessageRequest
    {
        public string UserMessage { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public Guid? ConversationId { get; set; }
    }

    public class SendMessageResponse
    {
        public Guid MessageId { get; set; }
        public string UserMessage { get; set; } = string.Empty;
        public string AiResponse { get; set; } = string.Empty;
        public string ModelUsed { get; set; } = string.Empty;
        public DateTime MessageTimestamp { get; set; }
        public Guid ConversationId { get; set; }
    }

    public class ConversationSummary
    {
        public Guid ConversationId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public int MessageCount { get; set; }
        public string? LastMessage { get; set; }
        public bool IsActive { get; set; }
    }

    public class MessageHistory
    {
        public Guid MessageId { get; set; }
        public string UserMessage { get; set; } = string.Empty;
        public string AiResponse { get; set; } = string.Empty;
        public string ModelUsed { get; set; } = string.Empty;
        public DateTime MessageTimestamp { get; set; }
    }

    public class RegenerateResponseRequest
    {
        public Guid MessageId { get; set; }
        public string? NewModelName { get; set; }
    }

    public class ChatModelInfo
    {
        public string ModelName { get; set; } = string.Empty;
        public string? PricingTier { get; set; }
        public bool IsAvailableForPaidUsers { get; set; }
        public bool IsActive { get; set; }
    }
}
