using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using OrderProcessor.Shared.Models;

namespace OrderProcessor.FunctionApp;

public class OrderFunction
{
    private readonly ILogger<OrderFunction> _logger;
    private static readonly Meter _meter = new Meter("OrderProcessor", "1.0.0");
    private static readonly Counter<int> _messageProcessed = _meter.CreateCounter<int>("message_processed_total");
    private static readonly Counter<double> _processingDuration = _meter.CreateCounter<double>("message_processing_duration_seconds");
    public OrderFunction(ILogger<OrderFunction> logger)
    {
        _logger = logger;
    }

    [Function(nameof(OrderFunction))]
    public async Task Run([QueueTrigger("orders", Connection = "AzureWebJobsStorage")] QueueMessage message)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("Task started at: {time}", DateTime.Now);

            await Task.Delay(20000); // Introduce some delay before processing the Queue message.

            _logger.LogInformation("Task started after delay at: {time}", DateTime.Now);

            _logger.LogInformation("C# Queue trigger function processed: {messageText}", message.MessageText);

            // Processing the OrderEvent message
            var orderEventJson = message.MessageText;
            // deserialize the JSON and process the order
            var orderEvent = System.Text.Json.JsonSerializer.Deserialize<OrderEvent>(orderEventJson);
            if (orderEvent == null)
            {
                _logger.LogError("Failed to deserialize OrderEvent from message: {messageText}", message.MessageText);
                return;
            }
            // Here you can add logic to process the orderEvent, e.g., save to database, call other services, etc.

            _logger.LogInformation("Order event processed: {orderEventJson}", orderEventJson);
            _messageProcessed.Add(1, new KeyValuePair<string, object?>("status", "success"));
        }
        catch (Exception ex)
        {
            _messageProcessed.Add(1, new KeyValuePair<string, object?>("status", "error"));
        }
        finally
        {
            _processingDuration.Add(stopwatch.Elapsed.TotalSeconds);
        }
    }
}