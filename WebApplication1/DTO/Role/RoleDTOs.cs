using System.ComponentModel.DataAnnotations;

namespace WebApplication1.DTO.Role
{
    public class RoleCreateDTO
    {
        [Required]
        [StringLength(256)]
        public string Name { get; set; } = null!;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        public bool IsActive { get; set; } = true;
    }

    public class RoleUpdateDTO
    {
        [Required]
        public string Id { get; set; } = null!;
        
        [Required]
        [StringLength(256)]
        public string Name { get; set; } = null!;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        public bool IsActive { get; set; } = true;
    }

    public class RoleResponseDTO
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
