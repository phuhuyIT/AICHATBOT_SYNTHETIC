using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public class RechargePackage : AuditableEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid PackageId { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = null!;
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal BonusAmount { get; set; } = 0;
        
        [NotMapped]
        public decimal TotalAmount => Amount + BonusAmount;
        
        [Column(TypeName = "decimal(5,2)")]
        public decimal? DiscountPercentage { get; set; }
        
        public bool IsActive { get; set; } = true;
        public bool IsPromotional { get; set; } = false;
        public DateTime? PromotionStartDate { get; set; }
        public DateTime? PromotionEndDate { get; set; }
        
        public int SortOrder { get; set; } = 0;
        
        public virtual ICollection<Transaction> Transactions { get; set; } = new HashSet<Transaction>();
    }
}
