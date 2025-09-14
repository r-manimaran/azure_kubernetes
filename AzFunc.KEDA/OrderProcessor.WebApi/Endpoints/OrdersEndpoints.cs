using OrderProcessor.WebApi.DTOs;
using OrderProcessor.WebApi.Services;

namespace OrderProcessor.WebApi.Endpoints;

public static class OrdersEndpoints
{
    public static void MapOrdersEndpoints(this WebApplication app)
    {
        app.MapPost("/orders", async (OrderRequest orderRequest, IOrderService orderService) =>
        {
            var orderEvent = await  orderService.CreateOrderAsync(orderRequest);

            return Results.Accepted($"/orders/{orderEvent.OrderId}", new { orderEvent.OrderId });
        })
        .WithName("CreateOrder")
        .WithTags("Orders")
        .Produces(StatusCodes.Status202Accepted)
        .Produces(StatusCodes.Status400BadRequest);
    }
}
