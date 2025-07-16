using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models;

public partial class Message
{
    [Key]
    public int MessageId { get; set; }

    public int? ConversationId { get; set; }

    public string UserMessage { get; set; } = null!;

    public string AiResponse { get; set; } = null!;

    public string? ModelUsed { get; set; }

    public DateTime MessageTimestamp { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? UpdatedAt { get; set; }

    public virtual Conversation? Conversation { get; set; }

    public virtual ICollection<ModifiedMessage> ModifiedMessages { get; set; } = new List<ModifiedMessage>();
}
