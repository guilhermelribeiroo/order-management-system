using Domain.Entities;
using Infrastructure.DBContext;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace IntegrationTests.Infrastructure
{
    public class OrderRepositoryTests : IDisposable
    {
        private readonly OrderDbContext _context;
        private readonly OrderRepository _repository;

        public OrderRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<OrderDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // isolated per test
                .Options;

            _context = new OrderDbContext(options);
            _repository = new OrderRepository(_context);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task AddAsync_ShouldPersistOrder()
        {
            var order = new Order(Guid.NewGuid());
            order.AddItem(Guid.NewGuid(), "Product A", 10.00m, 1);

            await _repository.AddAsync(order);
            await _repository.SaveChangesAsync();

            var saved = await _context.Orders.FindAsync(order.Id);
            Assert.NotNull(saved);
            Assert.Equal(order.CustomerId, saved.CustomerId);
        }

        [Fact]
        public async Task GetByIdAsync_ExistingOrder_ShouldReturnOrder()
        {
            var order = await SeedOrderAsync();

            var result = await _repository.GetByIdAsync(order.Id);

            Assert.NotNull(result);
            Assert.Equal(order.Id, result.Id);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ShouldReturnNull()
        {
            var result = await _repository.GetByIdAsync(Guid.NewGuid());

            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllOrders()
        {
            await SeedOrderAsync();
            await SeedOrderAsync();
            await SeedOrderAsync();

            var result = await _repository.GetAllAsync();

            Assert.Equal(3, result.Count());
        }

        [Fact]
        public async Task GetOrderWithItemsAsync_ShouldReturnOrderWithItems()
        {
            var order = new Order(Guid.NewGuid());
            order.AddItem(Guid.NewGuid(), "Product A", 5.00m, 3);
            order.AddItem(Guid.NewGuid(), "Product B", 15.00m, 1);

            await _repository.AddAsync(order);
            await _repository.SaveChangesAsync();

            var result = await _repository.GetOrderWithItemsAsync(order.Id);

            Assert.NotNull(result);
            Assert.Equal(2, result.Items.Count);
        }

        [Fact]
        public async Task GetOrderWithItemsAsync_NonExistingId_ShouldReturnNull()
        {
            var result = await _repository.GetOrderWithItemsAsync(Guid.NewGuid());

            Assert.Null(result);
        }

        [Fact]
        public async Task Update_ShouldPersistChanges()
        {
            var order = await SeedOrderAsync();

            order.UpdateStatus(OrderStatus.Paid);
            _repository.Update(order);
            await _repository.SaveChangesAsync();

            var updated = await _repository.GetByIdAsync(order.Id);
            Assert.Equal(OrderStatus.Paid, updated!.Status);
        }

        [Fact]
        public async Task Delete_ShouldRemoveOrder()
        {
            var order = await SeedOrderAsync();

            _repository.Delete(order);
            await _repository.SaveChangesAsync();

            var deleted = await _repository.GetByIdAsync(order.Id);
            Assert.Null(deleted);
        }

        [Fact]
        public async Task SaveChangesAsync_ShouldReturnNumberOfAffectedRows()
        {
            var order = new Order(Guid.NewGuid());
            await _repository.AddAsync(order);

            var affected = await _repository.SaveChangesAsync();

            Assert.Equal(1, affected);
        }

        // --- Helpers ---
        // Could be another class if more code is added
        private async Task<Order> SeedOrderAsync()
        {
            var order = new Order(Guid.NewGuid());
            order.AddItem(Guid.NewGuid(), "Default Item", 10.00m, 1);
            await _repository.AddAsync(order);
            await _repository.SaveChangesAsync();
            return order;
        }
    }

}
