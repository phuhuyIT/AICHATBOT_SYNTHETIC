using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Transactions;

namespace WebApplication1.Models
{
    public class PaymentTransaction : AuditableEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid PaymentTransactionId { get; set; }
        
        [Required]
        public Guid TransactionId { get; set; }
        
        [Required]
        [StringLength(50)]
        public string PaymentGateway { get; set; } = null!; // MoMo, VNPay, ZaloPay, etc.
        
        [StringLength(100)]
        public string? ExternalTransactionId { get; set; }
        
        [StringLength(100)]
        public string? PaymentIntentId { get; set; }
        
        [Required]
        public PaymentMethod PaymentMethod { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }
        
        [StringLength(10)]
        public string Currency { get; set; } = "VND"; // Support VND for Vietnamese payments
        
        [Required]
        public TransactionStatus Status { get; set; } = TransactionStatus.Pending; // Fixed enum
        
        public DateTime? ProcessedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? FailedAt { get; set; }
        
        [StringLength(500)]
        public string? FailureReason { get; set; }
        
        [StringLength(100)]
        public string? GatewayResponseCode { get; set; }
        
        // JSON data from payment gateway
        [Column(TypeName = "nvarchar(max)")]
        public string? GatewayResponseData { get; set; }
        
        // Vietnamese payment gateway specific fields
        [StringLength(20)]
        public string? MaskedCardNumber { get; set; }
        
        [StringLength(50)]
        public string? PaymentEmail { get; set; }
        
        [StringLength(20)]
        public string? BankCode { get; set; } // For banking gateways
        
        [StringLength(100)]
        public string? QRCodeData { get; set; } // For QR payments
        
        [StringLength(200)]
        public string? ReturnUrl { get; set; } // Payment return URL
        
        [StringLength(200)]
        public string? CancelUrl { get; set; } // Payment cancel URL
        
        [StringLength(200)]
        public string? NotifyUrl { get; set; } // Webhook notification URL
        
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        public virtual Transaction Transaction { get; set; } = null!;
    }
}
