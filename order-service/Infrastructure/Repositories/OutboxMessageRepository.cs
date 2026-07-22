using Domain.Entities;
using Infrastructure.DBContext;
using Infrastructure.Interfaces;

namespace Infrastructure.Repositories
{
    public class OutboxMessageRepository : Repository<OutboxMessage>, IOutboxMessageRepository
    {
        public OutboxMessageRepository(OrderServiceDbContext context) : base(context)
        {
        }
    }
}
