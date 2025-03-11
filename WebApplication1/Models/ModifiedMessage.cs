using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models;

public partial class ModifiedMessage
{
    [Key]
    public int ModifiedMessageId { get; set; }

    public int? MessageId { get; set; }

    public string UserModifiedMessage { get; set; } = null!;

    public DateTime ModifiedAt { get; set; }

    public virtual Message? Message { get; set; }
}
