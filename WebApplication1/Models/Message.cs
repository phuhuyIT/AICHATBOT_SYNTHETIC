using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models;

public partial class Message : AuditableEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid MessageId { get; set; }

    [ForeignKey(nameof(Branch))]
    public Guid BranchId { get; set; }
    public virtual ConversationBranch Branch { get; set; } = null!;

    // optional pointer to previous message inside the same branch
    public Guid? ParentMessageId { get; set; }
    public virtual Message? ParentMessage { get; set; }

    // user / assistant / system
    [MaxLength(10)]
    public string Role { get; set; } = "user";

    [Required]
    public string Content { get; set; } = string.Empty;

    public string? ModelUsed { get; set; }

    // token count for sliding‑window logic
    public int Tokens { get; set; }

    // optional embedding for RAG (OpenAI 1536‑float → byte[] serialized)
    [Column(TypeName = "varbinary(max)")]
    public byte[]? Embedding { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
