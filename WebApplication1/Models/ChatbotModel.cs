using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models;

public partial class ChatbotModel
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public string ModelName { get; set; } = null!;
    /// <summary>
    /// Dùng mô tả các mức giá của chatbot.
    /// </summary>
    public string? PricingTier { get; set; }

    public bool IsAvailableForPaidUsers { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<ChatBotApiKey> ChatBotApiKeys { get; set; } = new List<ChatBotApiKey>();
    public virtual ICollection<UsageLog> UsageLogs { get; set; } = new List<UsageLog>();
}
