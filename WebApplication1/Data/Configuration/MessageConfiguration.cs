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
            builder.HasIndex(m => m.BranchId)
                .HasDatabaseName("IX_Messages_BranchId");
                
            builder.HasIndex(m => m.Role)
                .HasDatabaseName("IX_Messages_Role");
                
            builder.HasIndex(m => new { m.BranchId, m.Role })
                .HasDatabaseName("IX_Messages_BranchId_Role");
                
            builder.HasIndex(m => m.CreatedAt)
                .HasDatabaseName("IX_Messages_CreatedAt");

            builder.HasIndex(m => m.ModelUsed)
                .HasDatabaseName("IX_Messages_ModelUsed");

            builder.HasIndex(m => m.ParentMessageId)
                .HasDatabaseName("IX_Messages_ParentMessageId");
        }
    }
}
