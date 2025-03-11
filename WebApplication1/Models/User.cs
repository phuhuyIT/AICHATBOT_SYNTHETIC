using Microsoft.AspNetCore.Identity;

namespace WebApplication1.Models;

public partial class User : IdentityUser
{
    public DateTime? LastLogin { get; set; }
    public bool IsPaidUser { get; set; }
    public decimal? Balance { get; set; }
    public virtual ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
    public virtual ICollection<ApiKey> apiKeys { get; set; } = new HashSet<ApiKey>();
    public virtual ICollection<Transaction> Transactions { get; set; } = new HashSet<Transaction>();
    public virtual ICollection<UsageLog> UsageLogs { get; set; } = new List<UsageLog>();
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
