using Azure.Storage.Queues;

namespace GracefulShutdownQueueWorker;

public class QueueWorker : BackgroundService
{
    private readonly ILogger<QueueWorker> _logger;
    private readonly QueueClient _queueClient;
    public QueueWorker(ILogger<QueueWorker> logger)
    {
        _logger = logger;
        _queueClient = new QueueClient(Environment.GetEnvironmentVariable("AzureWebJobsStorage"), "orders");
        _queueClient.CreateIfNotExists();
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker Started. Listening for messages...");

        while (!stoppingToken.IsCancellationRequested)
        {
            var messages = await _queueClient.ReceiveMessagesAsync(1, TimeSpan.FromSeconds(10), stoppingToken);
           
            if(messages.Value.Length == 0)
            {
                _logger.LogInformation("No messages found. Waiting for 2 second..");
                await  Task.Delay(2000, stoppingToken);
                continue;
            }

            foreach (var message in messages.Value)
            {
                try
                {
                    _logger.LogInformation("Processing message: {message}", message.MessageText);

                    //Simulate some work
                    await Task.Delay(5000, stoppingToken);

                    //Delete only after message is processed successfully
                    await _queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, stoppingToken);

                    _logger.LogInformation("Message processed successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message: {message}", message.MessageText);
                }
            }
            
        }
        _logger.LogInformation("ExecuteAsync loop exited, worker shutting down");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("StopAsync triggerd, final cleanup...");
        // Flush telemetry/logs, close DB/queue connections etc.
        await Task.Delay(2000,cancellationToken);
        _logger.LogInformation("Cleanup done, worker stopped");
    }
}
