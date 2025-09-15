
using Azure.Storage.Queues;
using Bogus;
using Microsoft.Extensions.Options;
using OrderProcessor.Shared.Models;

namespace OrderProcessor.WebApi;

public class OrderBackgroundProcessor : BackgroundService
{
    private readonly Faker _faker;
    private readonly ILogger<OrderBackgroundProcessor> _logger;
    private readonly AzureSettings _azureSettings;
    private readonly QueueClient _queueClient;
    public OrderBackgroundProcessor(ILogger<OrderBackgroundProcessor> logger, IOptions<AzureSettings> azureSettings)
    {
        _faker = new Faker();
        _logger = logger;
        _azureSettings = azureSettings.Value;
        _queueClient = new QueueClient(_azureSettings.QueueConnectionString, _azureSettings.QueueName, new QueueClientOptions
        {
            MessageEncoding = QueueMessageEncoding.Base64
        });

    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var items = new Faker<OrderItem>()
              .RuleFor(i => i.ProductId, f => $"prod-{f.Random.Guid().ToString()[..8]}")
              .RuleFor(i => i.Name, f => f.Commerce.ProductName())
              .RuleFor(i => i.Quantity, f => f.Random.Int(1, 10))
              .RuleFor(i => i.Price, f => decimal.Parse(f.Commerce.Price(1, 100)))
              .Generate(Random.Shared.Next(1, 10));

            decimal total =items.Sum(i=>i.Price * i .Quantity);
            var newOrderEvent = new OrderEvent
            {
                OrderId = Guid.CreateVersion7().ToString(),
                CustomerId = $"cust-{_faker.Random.Int(1, 100)}",
                OrderDate = DateTime.UtcNow,
                TotalAmount = total,
                Items = items,
                Status = "Pending"
            };
            await _queueClient.CreateIfNotExistsAsync();

            await _queueClient.SendMessageAsync(System.Text.Json.JsonSerializer.Serialize(newOrderEvent));
            
            _logger.LogInformation($"Order {newOrderEvent.OrderId} created");
            
            // Delay for 500ms
            Task.Delay(500, stoppingToken).Wait(stoppingToken);
        }        
    }
}
