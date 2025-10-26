using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text.Json;
using krt_cartoes_api.Core.Interfaces;
using krt_cartoes_api.Core.Dtos;
using System.Text;

namespace krt_cartoes_api.Infrastructure.Messaging
{
    public abstract class BaseCartoesConsumerService : BackgroundService
    {
        private readonly ConnectionFactory _factory;
        private readonly ICartoesService _cartoesService;
        private const string exchangeName = "accounts_exchange";
        private readonly string _queueName;
        private readonly string _routingKey;
        private readonly Func<AccountDto, Task> _handler;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        protected BaseCartoesConsumerService(
            ICartoesService cartoesService,
            string queueName,
            string routingKey,
            Func<AccountDto, Task> handler)
        {
            _factory = new ConnectionFactory { HostName = "localhost" };
            _cartoesService = cartoesService;
            _queueName = queueName;
            _routingKey = routingKey;
            _handler = handler;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var connection = await _factory.CreateConnectionAsync();
            var channel = await connection.CreateChannelAsync();

            await channel.ExchangeDeclareAsync(exchangeName, ExchangeType.Direct, durable: true);
            await channel.QueueDeclareAsync(_queueName, durable: true, exclusive: false, autoDelete: false);
            await channel.QueueBindAsync(_queueName, exchangeName, _routingKey);

            Console.WriteLine($"CartoesConsumer: Aguardando mensagens em '{_queueName}'...");

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                var message = Encoding.UTF8.GetString(ea.Body.ToArray());
                var accountEvent = JsonSerializer.Deserialize<AccountDto>(message, _jsonOptions);
                if (accountEvent != null)
                    await _handler(accountEvent);
            };

            await channel.BasicConsumeAsync(_queueName, autoAck: true, consumer: consumer);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
    public class CartoesAccountCreatedConsumerService : BaseCartoesConsumerService
    {
        public CartoesAccountCreatedConsumerService(ICartoesService cartoesService)
            : base(cartoesService,
                  queueName: "cartoes_account_created_queue",
                  routingKey: "account_created",
                  handler: cartoesService.OnCreateAccountAsync)
        { }
    }
    public class CartoesAccountUpdatedConsumerService : BaseCartoesConsumerService
    {
        public CartoesAccountUpdatedConsumerService(ICartoesService cartoesService)
            : base(cartoesService,
                  queueName: "cartoes_account_updated_queue",
                  routingKey: "account_updated",
                  handler: cartoesService.OnUpdateAccountAsync)
        { }
    }
    public class CartoesAccountDeletedConsumerService : BaseCartoesConsumerService
    {
        public CartoesAccountDeletedConsumerService(ICartoesService cartoesService)
            : base(cartoesService,
                  queueName: "cartoes_account_deleted_queue",
                  routingKey: "account_deleted",
                  handler: cartoesService.OnDeleteAccountAsync)
        { }
    }

}
