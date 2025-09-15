using System;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using OrderProcessor.Shared.Models;

namespace OrderProcessor.FunctionApp;

public class OrderFunction
{
    private readonly ILogger<OrderFunction> _logger;

    public OrderFunction(ILogger<OrderFunction> logger)
    {
        _logger = logger;
    }

    [Function(nameof(OrderFunction))]
    public async Task Run([QueueTrigger("orders", Connection = "AzureWebJobsStorage")] QueueMessage message)
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
    }
}