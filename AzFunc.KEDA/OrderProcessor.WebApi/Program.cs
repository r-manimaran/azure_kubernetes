using OrderProcessor.WebApi;
using OrderProcessor.WebApi.Endpoints;
using OrderProcessor.WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.Configure<AzureSettings>(builder.Configuration.GetSection(AzureSettings.SectionName));

builder.Services.AddScoped<IOrderService, OrderService>();

builder.Services.AddScoped<IQueueService, QueueService>();

builder.Services.AddHostedService<OrderBackgroundProcessor>();

var app = builder.Build();

app.MapOpenApi();

app.UseSwaggerUI(options => {
    options.SwaggerEndpoint(
    "/openapi/v1.json", "OpenAPI v1");
});

app.MapOrdersEndpoints();

app.MapGet("/", () => "Hello World!");

app.Run();
