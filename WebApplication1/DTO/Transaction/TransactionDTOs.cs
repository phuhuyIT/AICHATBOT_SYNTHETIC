using System.ComponentModel.DataAnnotations;

namespace WebApplication1.DTO.Transaction
{
    public class TransactionCreateDTO
    {
        [Required]
        public string UserId { get; set; } = null!;
        
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }
        
        [Required]
        public int TransactionType { get; set; } // TransactionType enum as int
        
        public Guid? RechargePackageId { get; set; }
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        public int PaymentMethod { get; set; } // PaymentMethod enum as int
        
        public string? PaymentGateway { get; set; }
        
        public string? MetadataJson { get; set; }
    }

    public class TransactionResponseDTO
    {
        public Guid TransactionId { get; set; }
        public string UserId { get; set; } = null!;
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public string TransactionType { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string? Description { get; set; }
        public string? ReferenceNumber { get; set; }
        public decimal? BalanceBefore { get; set; }
        public decimal? BalanceAfter { get; set; }
        public Guid? RechargePackageId { get; set; }
        public string? RechargePackageName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class TransactionUpdateDTO
    {
        [Required]
        public Guid TransactionId { get; set; }
        
        public int? Status { get; set; } // TransactionStatus enum as int
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        public string? ReferenceNumber { get; set; }
        
        public string? MetadataJson { get; set; }
    }

    public class RechargeRequestDTO
    {
        [Required]
        public string UserId { get; set; } = null!;
        
        public Guid? RechargePackageId { get; set; }
        
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal? CustomAmount { get; set; }
        
        [Required]
        public int PaymentMethod { get; set; } // PaymentMethod enum as int
        
        public string? PaymentGateway { get; set; } = "Stripe";
        
        public string? ReturnUrl { get; set; }
        public string? CancelUrl { get; set; }
    }

    public class RechargeResponseDTO
    {
        public Guid TransactionId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = null!;
        public string? PaymentUrl { get; set; }
        public string? PaymentIntentId { get; set; }
        public string? ReferenceNumber { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class TransactionHistoryFilterDTO
    {
        public string? UserId { get; set; }
        public int? TransactionType { get; set; }
        public int? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
