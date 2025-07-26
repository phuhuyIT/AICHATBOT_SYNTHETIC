using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;
using WebApplication1.Service.Interface;
using System.Security.Claims;
using WebApplication1.Service;

namespace WebApplication1.Data;

public class ApplicationDbContext : IdentityDbContext<User, Role, string>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceProvider _serviceProvider;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor httpContextAccessor, IServiceProvider serviceProvider)
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
        _serviceProvider = serviceProvider;
    }

    // Define DbSet properties for other entities
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<ChatBotApiKey> ChatBotApiKeys { get; set; }
    public DbSet<ChatbotModel> ChatbotModels { get; set; }
    public DbSet<UsageLog> UsageLogs { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<ConversationBranch> ConversationBranches { get; set; }
    
    // New transaction-related entities
    public DbSet<RechargePackage> RechargePackages { get; set; }
    public DbSet<PaymentTransaction> PaymentTransactions { get; set; }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await ApplyAuditInformation();
        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ApplyAuditInformation().GetAwaiter().GetResult();
        return base.SaveChanges();
    }

    private async Task ApplyAuditInformation()
    {
        var currentUserId = await GetCurrentOrSystemUserIdAsync();
        var utcNow = DateTime.UtcNow;

        // Handle AuditableEntity entities
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedBy = currentUserId;
                    entry.Entity.CreatedAt = utcNow;
                    break;

                case EntityState.Modified:
                    if (!string.IsNullOrEmpty(GetCurrentUserId()))
                    {
                        entry.Entity.UpdatedBy = currentUserId;
                        entry.Entity.UpdatedAt = utcNow;
                    }
                    // Prevent modification of CreatedBy and CreatedAt
                    entry.Property(nameof(AuditableEntity.CreatedBy)).IsModified = false;
                    entry.Property(nameof(AuditableEntity.CreatedAt)).IsModified = false;
                    break;
            }
        }

        // Handle Role entities separately (since they inherit from IdentityRole, not AuditableEntity)
        foreach (var entry in ChangeTracker.Entries<Role>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedBy = currentUserId;
                    entry.Entity.CreatedAt = utcNow;
                    break;

                case EntityState.Modified:
                    if (!string.IsNullOrEmpty(GetCurrentUserId()))
                    {
                        entry.Entity.UpdatedBy = currentUserId;
                        entry.Entity.UpdatedAt = utcNow;
                    }
                    // Prevent modification of CreatedBy and CreatedAt
                    entry.Property(nameof(Role.CreatedBy)).IsModified = false;
                    entry.Property(nameof(Role.CreatedAt)).IsModified = false;
                    break;
            }
        }
    }

    private async Task<string> GetCurrentOrSystemUserIdAsync()
    {
        var currentUserId = GetCurrentUserId();
        if (!string.IsNullOrEmpty(currentUserId))
        {
            return currentUserId;
        }

        // Fallback to system user for unauthenticated operations (like registration)
        using var scope = _serviceProvider.CreateScope();
        var seederService = scope.ServiceProvider.GetRequiredService<IDatabaseSeederService>();
        return await seederService.GetSystemUserIdAsync();
    }

    private string? GetCurrentUserId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated == true)
        {
            return httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        }
        return null;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure audit relationships to prevent cascading deletes on audit fields
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(AuditableEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .HasOne(typeof(User), nameof(AuditableEntity.Creator))
                    .WithMany()
                    .HasForeignKey(nameof(AuditableEntity.CreatedBy))
                    .OnDelete(DeleteBehavior.Restrict);

                modelBuilder.Entity(entityType.ClrType)
                    .HasOne(typeof(User), nameof(AuditableEntity.Updater))
                    .WithMany()
                    .HasForeignKey(nameof(AuditableEntity.UpdatedBy))
                    .OnDelete(DeleteBehavior.Restrict);
            }
        }
    }
}
