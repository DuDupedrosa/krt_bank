using System.Text.Json;
using System.Text;
using RabbitMQ.Client;
using krt_api.Core.Interfaces;

namespace krt_api.Infrastructure.Messaging
{
    public class AccountProducer : IAccountProducer
    {
        private readonly ConnectionFactory _factory;

        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        public AccountProducer()
        {
            _factory = new ConnectionFactory() { HostName = "localhost" };
        }
        public async Task PublishAsync(string routingKey, string exchangeName, string exchangeType, object message)
        {
            using var connection = await _factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.ExchangeDeclareAsync(
                exchange: exchangeName,
                type: exchangeType,
                durable: true
            );

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, _jsonOptions));

            await channel.BasicPublishAsync(
                exchange: exchangeName,
                routingKey: routingKey,
                mandatory: false,
                body: body
            );

            Console.WriteLine($"[Producer] Publicado evento '{routingKey}' {JsonSerializer.Serialize(message)}");
        }
    }
}
