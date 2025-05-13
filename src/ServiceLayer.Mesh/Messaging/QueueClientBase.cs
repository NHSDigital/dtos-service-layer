using System.Text.Json;
using Azure.Storage.Queues;
using Microsoft.Extensions.Logging;

namespace ServiceLayer.Mesh.Messaging;

public abstract class QueueClientBase(ILogger logger, QueueServiceClient queueServiceClient, string queueName)
{
    private QueueClient? _queueClient;
    private QueueClient? _poisonQueueClient;

    private QueueClient Client => _queueClient ??= CreateClient();
    private QueueClient PoisonClient => _poisonQueueClient ??= CreatePoisonClient();

    private static readonly JsonSerializerOptions QueueJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private QueueClient CreateClient()
    {
        var client = queueServiceClient.GetQueueClient(queueName);
        client.CreateIfNotExists(); // TODO - consider environment gating this
        return client;
    }

    private QueueClient CreatePoisonClient()
    {
        var poisonQueueName = $"{queueName}-poison";
        var client = queueServiceClient.GetQueueClient(poisonQueueName);
        client.CreateIfNotExists(); // TODO - consider environment gating this
        return client;
    }

    protected async Task SendJsonMessageAsync<T>(T message)
    {
        try
        {
            var json = JsonSerializer.Serialize(message, QueueJsonOptions);
            await Client.SendMessageAsync(json);
        }
        catch (Exception e)
        {
            // TODO - consider including file ID or correlation ID in error logs
            logger.LogError(e, "Error sending message to queue {QueueName}", queueName);
            throw;
        }
    }

    protected async Task SendToPoisonQueueAsync<T>(T message)
    {
        try
        {
            var json = JsonSerializer.Serialize(message, QueueJsonOptions);
            await PoisonClient.SendMessageAsync(json);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error sending message to poison queue {PoisonQueueName}", $"{queueName}-poison");
            throw;
        }
    }
}
