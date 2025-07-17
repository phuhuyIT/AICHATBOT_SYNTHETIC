using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebApplication1.Models;

namespace WebApplication1.Data.Configuration
{
    public class MessageConfiguration : IEntityTypeConfiguration<Message>
    {
        public void Configure(EntityTypeBuilder<Message> builder)
        {
            // Add indexes for frequently queried columns
            builder.HasIndex(m => m.ConversationId)
                .HasDatabaseName("IX_Messages_ConversationId");
                
            builder.HasIndex(m => m.IsActive)
                .HasDatabaseName("IX_Messages_IsActive");
                
            builder.HasIndex(m => new { m.ConversationId, m.IsActive })
                .HasDatabaseName("IX_Messages_ConversationId_IsActive");
                
            builder.HasIndex(m => m.MessageTimestamp)
                .HasDatabaseName("IX_Messages_MessageTimestamp");
                
            builder.HasIndex(m => m.ModelUsed)
                .HasDatabaseName("IX_Messages_ModelUsed");
        }
    }
}
