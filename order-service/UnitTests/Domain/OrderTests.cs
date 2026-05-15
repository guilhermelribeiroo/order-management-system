using Domain.Entities;

namespace UnitTests.Domain
{
    public class OrderTests
    {
        [Fact]
        public void Constructor_ShouldSetCustomerIdAndDefaultStatus()
        {
            var customerId = Guid.NewGuid();

            var order = new Order(customerId);

            Assert.Equal(customerId, order.CustomerId);
            Assert.Equal(OrderStatus.Pending, order.Status);
            Assert.Equal(0, order.TotalAmount);
            Assert.Empty(order.Items);
        }

        [Fact]
        public void AddItem_ShouldAddItemAndUpdateTotalAmount()
        {
            var order = new Order(Guid.NewGuid());
            var productId = Guid.NewGuid();

            order.AddItem(productId, "Product A", 10.00m, 3);

            Assert.Single(order.Items);
            Assert.Equal(30.00m, order.TotalAmount);
        }

        [Fact]
        public void AddItem_MultipleItems_ShouldAccumulateTotalAmount()
        {
            var order = new Order(Guid.NewGuid());

            order.AddItem(Guid.NewGuid(), "Product A", 10.00m, 2); // 20
            order.AddItem(Guid.NewGuid(), "Product B", 5.50m, 4);  // 22

            Assert.Equal(2, order.Items.Count);
            Assert.Equal(42.00m, order.TotalAmount);
        }

        [Fact]
        public void AddItem_ShouldCreateOrderItemWithCorrectProperties()
        {
            var order = new Order(Guid.NewGuid());
            var productId = Guid.NewGuid();

            order.AddItem(productId, "Test Product", 99.99m, 1);

            var item = order.Items.Single();
            Assert.Equal(productId, item.ProductId);
            Assert.Equal("Test Product", item.ProductName);
            Assert.Equal(99.99m, item.UnitPrice);
            Assert.Equal(1, item.Quantity);
        }

        [Fact]
        public void UpdateStatus_ShouldChangeStatus()
        {
            var order = new Order(Guid.NewGuid());

            order.UpdateStatus(OrderStatus.Paid);

            Assert.Equal(OrderStatus.Paid, order.Status);
        }

        [Fact]
        public void UpdateStatus_ShouldSetUpdatedAt()
        {
            var order = new Order(Guid.NewGuid());
            var before = DateTime.UtcNow;

            order.UpdateStatus(OrderStatus.Cancelled);

            Assert.True(order.UpdatedAt >= before);
        }

        [Theory]
        [InlineData(OrderStatus.PaymentProcessing)]
        [InlineData(OrderStatus.Paid)]
        [InlineData(OrderStatus.Cancelled)]
        [InlineData(OrderStatus.Completed)]
        public void UpdateStatus_AllValidStatuses_ShouldSucceed(OrderStatus status)
        {
            var order = new Order(Guid.NewGuid());

            order.UpdateStatus(status);

            Assert.Equal(status, order.Status);
        }

        [Fact]
        public void Items_ShouldBeReadOnly()
        {
            var order = new Order(Guid.NewGuid());

            // IReadOnlyCollection cannot be cast to a mutable collection
            Assert.IsAssignableFrom<IReadOnlyCollection<OrderItem>>(order.Items);
        }
    }
}
