using Domain.Entities;

namespace UnitTests.Domain
{
    public class OrderItemTests
    {
        [Fact]
        public void Constructor_ShouldSetAllProperties()
        {
            var productId = Guid.NewGuid();

            var item = new OrderItem(productId, "Test Product", 15.00m, 5);

            Assert.Equal(productId, item.ProductId);
            Assert.Equal("Test Product", item.ProductName);
            Assert.Equal(15.00m, item.UnitPrice);
            Assert.Equal(5, item.Quantity);
        }

        [Fact]
        public void Constructor_ShouldLeaveOrderIdAsDefault()
        {
            var item = new OrderItem(Guid.NewGuid(), "Test Product", 1.00m, 1);

            Assert.Equal(Guid.Empty, item.OrderId);
            Assert.Null(item.Order);
        }
    }

}
