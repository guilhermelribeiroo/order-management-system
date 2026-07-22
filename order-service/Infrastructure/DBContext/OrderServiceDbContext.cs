using Domain.Entities;
using Infrastructure.Configuration;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.DBContext
{
    public class OrderServiceDbContext : DbContext, IUnitOfWork
    {
        public OrderServiceDbContext(DbContextOptions<OrderServiceDbContext> options) : base(options) { }

        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new OrderEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new OrderItemEntityTypeConfiguration());
            modelBuilder.ApplyConfiguration(new OutboxMessageEntityTypeConfiguration());

            base.OnModelCreating(modelBuilder);
        }
    }
}