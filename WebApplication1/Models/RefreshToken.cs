using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class RefreshToken
    {
        [Key]
        public int RefreshTokenID { get; set; }
        public string? UserId { get; set; }
        public string Token { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsExpired => DateTime.UtcNow >= ExpiryDate;
        public bool IsActive => !IsRevoked && !IsExpired;
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public DateTime? Revoked { get; set; }
        public bool IsRevoked => Revoked.HasValue;
        public DateTime? UpdatedAt { get; set; }
        public virtual User? User { get; set; }
    }
}
