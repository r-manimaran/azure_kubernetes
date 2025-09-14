using Azure.Storage.Queues;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace OrderProcessor.WebApi.Services;

public class QueueService : IQueueService
{
    private readonly ILogger<QueueService> _logger;
    private readonly AzureSettings _azureSettings;
    private readonly QueueClient _queueClient;
    public QueueService(IOptions<AzureSettings> azureSettings, ILogger<QueueService> logger)
    {
        _azureSettings = azureSettings.Value;
        _logger = logger;
        _queueClient = new QueueClient(_azureSettings.QueueConnectionString, _azureSettings.QueueName);
    }
    public async Task EnqueueMessageAsync<T>(T message)
    {
        var jsonMessage = JsonSerializer.Serialize(message);

        _logger.LogInformation("Enqueuing message to Azure Queue: {jsonMessage}", jsonMessage);
        // Encode the message to Base64 to ensure it is safely transmitted (Azure Queue requires text <= 64KB)
        var base64Message = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(jsonMessage));
        
        await _queueClient.SendMessageAsync(base64Message);
    }
}

public interface IQueueService
{
    Task EnqueueMessageAsync<T>(T message);
}   
