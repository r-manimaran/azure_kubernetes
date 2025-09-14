using OrderProcessor.Shared.Models;
using OrderProcessor.WebApi.DTOs;
using Azure.Storage.Queues;
using System.Text.Json;

namespace OrderProcessor.WebApi.Services;

public interface IOrderService
{
    Task<OrderEvent> CreateOrderAsync(OrderRequest orderRequest);
}
public class OrderService : IOrderService
{
    private readonly IQueueService _queueService;
    private readonly ILogger<OrderService> _logger;

    public OrderService(IQueueService queueService, ILogger<OrderService> logger)
    {
        _queueService = queueService;
        _logger = logger;
    }
    public async Task<OrderEvent> CreateOrderAsync(OrderRequest orderRequest)
    {
        var orderEvent = new OrderEvent
        {
            CustomerId = orderRequest.CustomerId,
            OrderDate = DateTime.UtcNow,
            Items = orderRequest.Items.Select(item => new OrderItem
            {
                ProductId = item.ProductId,
                Name = item.ProductName,
                Quantity = item.Quantity,
                Price = item.Price
            }).ToList(),
            TotalAmount = orderRequest.Items.Sum(item => item.Price * item.Quantity),
            Status = "Pending"
        };

        await _queueService.EnqueueMessageAsync(orderEvent);

        _logger.LogInformation("Order event created and enqueued: {orderEventJson}",JsonSerializer.Serialize(orderEvent));
        return orderEvent;

    }
}