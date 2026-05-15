namespace Infrastructure.Messaging
{
    public interface IEventBus
    {
        Task Publish<TEvent>(TEvent @event) where TEvent : class;
    }
}
