using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models;

public partial class ApiKey
{
    [Key]
    public int ApiKeyId { get; set; }

    public string? UserId { get; set; }

    public string ApiKey1 { get; set; } = null!;

    public string ServiceName { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public string? Setting { get; set; }

    public string Usage { get; set; } = null!;

    public virtual ICollection<ChatbotModel> ChatbotModels { get; set; } = new List<ChatbotModel>();

    public virtual User? User { get; set; }
}
