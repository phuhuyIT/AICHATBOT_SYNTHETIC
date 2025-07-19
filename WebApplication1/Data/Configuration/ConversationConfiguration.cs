using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebApplication1.Models;

namespace WebApplication1.Data.Configuration
{
    public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
    {
        public void Configure(EntityTypeBuilder<Conversation> builder)
        {
            // Primary key is already configured via [Key] attribute in model
            
            // Add indexes for frequently queried columns
            builder.HasIndex(c => c.UserId)
                .HasDatabaseName("IX_Conversations_UserId");
                
            builder.HasIndex(c => c.IsActive)
                .HasDatabaseName("IX_Conversations_IsActive");
                
            builder.HasIndex(c => new { c.UserId, c.IsActive })
                .HasDatabaseName("IX_Conversations_UserId_IsActive");

            builder.HasIndex(c => c.StartedAt)
                .HasDatabaseName("IX_Conversations_StartedAt");
        }
    }
}
