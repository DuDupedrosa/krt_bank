namespace krt_api.Core.Interfaces
{
    public interface IAccountProducer
    {
        Task PublishAsync(string routingKey, string exchangeName, string exchangeType, object message);
    }
}
