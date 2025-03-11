using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models;

public partial class ChatbotModel
{
    [Key]
    public int ModelId { get; set; }

    public string ModelName { get; set; } = null!;

    public int? ApiKeyId { get; set; }

    public string? PricingTier { get; set; }

    public bool IsAvailableForPaidUsers { get; set; }

    public virtual ApiKey? ApiKey { get; set; }
    public virtual ICollection<UsageLog> UsageLogs { get; set; } = new List<UsageLog>();
}
