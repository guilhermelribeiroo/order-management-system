namespace Domain.Entities
{
    public class Order : BaseEntity
    {
        public Guid CustomerId { get; private set; }
        public decimal TotalAmount { get; private set; }
        public OrderStatus Status { get; private set; } = OrderStatus.Pending;

        private readonly List<OrderItem> _items = new();
        public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

        public Order(Guid customerId)
        {
            CustomerId = customerId;
        }

        public void AddItem(Guid productId, string productName, decimal unitPrice, int quantity)
        {
            var item = new OrderItem(productId, productName, unitPrice, quantity);
            _items.Add(item);
            TotalAmount += unitPrice * quantity;
        }

        public void UpdateStatus(OrderStatus newStatus)
        {
            Status = newStatus;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public enum OrderStatus
    {
        Pending,
        PaymentProcessing,
        Paid,
        Cancelled,
        Completed
    }
}
