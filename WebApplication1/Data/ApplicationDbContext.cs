using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;
namespace WebApplication1.Data;

public class ApplicationDbContext : IdentityDbContext<User>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    { }

    // Define DbSet properties for other entities
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<ModifiedMessage> ModifiedMessages { get; set; }
    public DbSet<ChatBotApiKey> ApiKeys { get; set; }
    public DbSet<ChatbotModel> ChatbotModels { get; set; }
    public DbSet<UsageLog> UsageLogs { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
}
