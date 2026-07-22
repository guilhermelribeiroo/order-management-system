using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration
{
    public class OrderEntityTypeConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.ToTable("Orders");
            builder.HasKey(o => o.Id);
            builder.Property(o => o.Id).ValueGeneratedNever();

            builder.Property(o => o.CustomerId).IsRequired();
            builder.Property(o => o.TotalAmount).IsRequired().HasColumnType("decimal(18,2)");
            builder.Property(o => o.Status).IsRequired();
            builder.Property(o => o.CreatedAt).IsRequired();
            builder.Property(o => o.UpdatedAt).IsRequired();

            builder.HasMany(o => o.Items)
                   .WithOne(i => i.Order)
                   .HasForeignKey(i => i.OrderId);
        }
    }
}