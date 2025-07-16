using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class UsageLog
    {
        [Key]
        public int UsageLogID { get; set; }
        public string? UserID { get; set; }
        public int? ChatbotModelID { get; set; }
        public DateTime UsageDate { get; set; }
        public Double UsageAmount { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? UpdatedAt { get; set; }
        public virtual User? User { get; set; }
        public virtual ChatbotModel? ChatbotModel { get; set; }
    }
}
