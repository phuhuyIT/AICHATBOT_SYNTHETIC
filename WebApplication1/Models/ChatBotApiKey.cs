using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models;

public partial class ChatBotApiKey
{
    [Key]
    public int ApiKeyId { get; set; }

    public string? UserId { get; set; }

    public string ApiKey { get; set; } = null!;

    public string ServiceName { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public string? Setting { get; set; }

    public string Usage { get; set; } = null!;
    
    [ForeignKey("ChatbotModel")]
    public int ChatbotModelId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? UpdatedAt { get; set; }
    
    public virtual ChatbotModel? ChatbotModel { get; set; }
    public virtual User? User { get; set; }
}
