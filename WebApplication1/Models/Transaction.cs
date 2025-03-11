using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class Transaction
    {
        [Key]
        public int TransactionId { get; set; }
        public string? UserId { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public string TransactionType { get; set; } = null!;
        public virtual User? User { get; set; }
    }
}
