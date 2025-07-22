using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models;

public abstract class AuditableEntity
{
    public const string SYSTEM_USER_ID = "system-user-id"; // Will be replaced with actual GUID
    
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Required]
    [MaxLength(450)] // Standard length for Identity user ID
    public string CreatedBy { get; set; } = SYSTEM_USER_ID;
    
    public DateTime? UpdatedAt { get; set; }
    
    [MaxLength(450)]
    public string? UpdatedBy { get; set; }
    
    // Navigation properties
    [ForeignKey(nameof(CreatedBy))]
    public virtual User Creator { get; set; } = null!;
    
    [ForeignKey(nameof(UpdatedBy))]
    public virtual User? Updater { get; set; }
}
