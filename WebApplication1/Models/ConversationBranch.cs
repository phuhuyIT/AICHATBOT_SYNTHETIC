using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Models;

public partial class ConversationBranch : AuditableEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid BranchId { get; set; }

    [ForeignKey(nameof(Conversation))]
    public Guid ConversationId { get; set; }
    public virtual Conversation Conversation { get; set; } = null!;

    // self‑referencing parent branch (for forks)
    public Guid? ParentBranchId { get; set; }
    public virtual ConversationBranch? ParentBranch { get; set; }

    // hierarchyid column stores full path for fast ancestor/descendant queries
    [Column(TypeName = "hierarchyid")]
    public HierarchyId? Path { get; set; }

    // running summary
    public string? Summary { get; set; }
    
    public bool IsActive { get; set; } = true;

    // optimistic‑concurrency token
    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public virtual ICollection<Message> Messages { get; set; } = new HashSet<Message>();
}
