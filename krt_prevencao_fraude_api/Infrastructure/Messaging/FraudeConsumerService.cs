using krt_prevencao_fraude_api.Core.Dtos;
using krt_prevencao_fraude_api.Core.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace krt_prevencao_fraude_api.Infrastructure.Messaging
{
    public abstract class BaseFraudeConsumerService : BackgroundService
    {
        private readonly ConnectionFactory _factory;
        private readonly IFraudeService _fraudeService;
        private const string exchangeName = "accounts_exchange";
        private readonly string _queueName;
        private readonly string _routingKey;
        private readonly Func<AccountDto, Task> _handler;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        protected BaseFraudeConsumerService(
            IFraudeService fraudeService,
            string queueName,
            string routingKey,
            Func<AccountDto, Task> handler)
        {
            _factory = new ConnectionFactory { HostName = "localhost" };
            _fraudeService = fraudeService;
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

            Console.WriteLine($"FraudeConsumer: Aguardando mensagens em '{_queueName}'...");

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

    public class FraudeAccountCreatedConsumerService : BaseFraudeConsumerService
    {
        public FraudeAccountCreatedConsumerService(IFraudeService fraudeService)
            : base(fraudeService,
                  queueName: "fraude_account_created_queue",
                  routingKey: "account_created",
                  handler: fraudeService.OnCreateAccountAsync)
        { }
    }
    public class FraudeAccountUpdatedConsumerService : BaseFraudeConsumerService
    {
        public FraudeAccountUpdatedConsumerService(IFraudeService fraudeService)
            : base(fraudeService,
                  queueName: "fraude_account_updated_queue",
                  routingKey: "account_updated",
                  handler: fraudeService.OnUpdateAccountAsync)
        { }
    }
    public class FraudeAccountDeletedConsumerService : BaseFraudeConsumerService
    {
        public FraudeAccountDeletedConsumerService(IFraudeService fraudeService)
            : base(fraudeService,
                  queueName: "fraude_account_deleted_queue",
                  routingKey: "account_deleted",
                  handler: fraudeService.OnDeleteAccountAsync)
        { }
    }
}
