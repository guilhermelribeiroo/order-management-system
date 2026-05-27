using Domain.Entities;

namespace Infrastructure.Interfaces
{
    public interface IOutboxMessageRepository : IRepository<OutboxMessage>
    {
    }
}
