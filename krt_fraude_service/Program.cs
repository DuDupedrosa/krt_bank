using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

class Program
{
    private const string ExchangeName = "accounts.exchange";

    static async Task Main()
    {
        var factory = new ConnectionFactory() { HostName = "localhost" };

        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync(exchange: ExchangeName, type: ExchangeType.Direct, durable: true);

        string queueName = "fraude.queue";
        await channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false);

        await channel.QueueBindAsync(queueName, ExchangeName, "account.created");
        await channel.QueueBindAsync(queueName, ExchangeName, "account.updated");
        await channel.QueueBindAsync(queueName, ExchangeName, "account.deleted");

        Console.WriteLine("[FraudeService] Aguardando eventos...");

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var message = Encoding.UTF8.GetString(ea.Body.ToArray());
            Console.WriteLine($"[FraudeService] Recebeu evento '{ea.RoutingKey}': {message}");
        };

        await channel.BasicConsumeAsync(queue: queueName, autoAck: true, consumer: consumer);
        Console.ReadLine();
    }
}
