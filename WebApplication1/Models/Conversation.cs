using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models;

public partial class Conversation : AuditableEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid ConversationId { get; set; }

    [ForeignKey(nameof(User))]
    public string UserId { get; set; } = null!;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public bool IsPaidUser { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual ICollection<ConversationBranch> Branches { get; set; } = new HashSet<ConversationBranch>();
    public virtual User User { get; set; } = null!;
}
