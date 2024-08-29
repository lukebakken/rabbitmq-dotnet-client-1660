using Genie.Common.Settings;
using RabbitMQ.Client;

namespace Genie.Common.Adapters.RabbitMQ;

public class RabbitUtils
{
    public static async Task<IConnection> GetConnection(RabbitMQSettings settings, bool isAsync)
    {
        ConnectionFactory factory = new ConnectionFactory();

        factory.Uri = new Uri($@"amqp://{settings.User}:{settings.Pass}@{settings.Host}:5672/");
        //factory.ConsumerDispatchConcurrency = 1;
        return await factory.CreateConnectionAsync();
    }
}