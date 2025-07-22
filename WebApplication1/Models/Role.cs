using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models;

public class Role : IdentityRole
{
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Audit fields (can't inherit from AuditableEntity due to IdentityRole)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    [MaxLength(450)]
    public string? CreatedBy { get; set; }
    
    [MaxLength(450)]
    public string? UpdatedBy { get; set; }
    
    // Navigation properties
    [ForeignKey(nameof(CreatedBy))]
    public virtual User? Creator { get; set; }
    
    [ForeignKey(nameof(UpdatedBy))]
    public virtual User? Updater { get; set; }
}
