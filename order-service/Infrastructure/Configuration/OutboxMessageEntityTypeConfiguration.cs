using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration
{
    public class OutboxMessageEntityTypeConfiguration : IEntityTypeConfiguration<OutboxMessage>
    {
        public void Configure(EntityTypeBuilder<OutboxMessage> builder)
        {
            builder.ToTable("OutboxMessages");
            builder.HasKey(i => i.Id);
            builder.Property(i => i.Id);
            builder.Property(i => i.Type).IsRequired().HasMaxLength(200);
            builder.Property(i => i.Payload).IsRequired();
            builder.Property(i => i.CreatedAt).HasDefaultValueSql("NOW()").IsRequired();
            builder.Property(i => i.ProcessedAt).IsRequired(false);
            builder.Property(i => i.Status).HasConversion<string>().IsRequired().HasMaxLength(50);
            builder.Property(i => i.RetryCount).IsRequired();
            builder.Property(i => i.Error).IsRequired(false);
            builder.Property(i => i.NextRetryAt).IsRequired(false);

            builder.HasIndex(i => i.Status);
        }
    }
}
