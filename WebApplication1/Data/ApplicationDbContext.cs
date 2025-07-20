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
    public DbSet<ChatBotApiKey> ChatBotApiKeys { get; set; }
    public DbSet<ChatbotModel> ChatbotModels { get; set; }
    public DbSet<UsageLog> UsageLogs { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<ConversationBranch> ConversationBranches { get; set; }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder); 
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=ChatbotDB;Trusted_Connection=True;TrustServerCertificate=True;", options => options.UseHierarchyId());
    }
}
