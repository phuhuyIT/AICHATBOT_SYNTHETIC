using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public class Transaction : AuditableEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid TransactionId { get; set; }
        
        [Required]
        public string UserId { get; set; } = null!;
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        
        [Required]
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
        
        [Required]
        public TransactionType TransactionType { get; set; }
        
        [Required]
        public TransactionStatus Status { get; set; } = TransactionStatus.Pending;
        
        public Guid? RechargePackageId { get; set; }
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        [StringLength(100)]
        public string? ReferenceNumber { get; set; }
        
        // Balance before and after transaction for audit trail
        [Column(TypeName = "decimal(18,2)")]
        public decimal? BalanceBefore { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? BalanceAfter { get; set; }
        
        // Metadata for additional transaction info
        [Column(TypeName = "nvarchar(max)")]
        public string? MetadataJson { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        public virtual User User { get; set; } = null!;
        public virtual RechargePackage? RechargePackage { get; set; }
        public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new HashSet<PaymentTransaction>();
    }
}
