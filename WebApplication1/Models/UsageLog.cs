using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public class UsageLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid UsageLogId { get; set; }
        public string? UserId { get; set; }
        public Guid? ChatbotModelId { get; set; }
        public DateTime UsageDate { get; set; }
        public Double UsageAmount { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? UpdatedAt { get; set; }
        public virtual User? User { get; set; }
        public virtual ChatbotModel? ChatbotModel { get; set; }
    }
}
