using FluentValidation;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using WebApi.Data;
using WebApi.Dtos;
using WebApi.Extenisons;
using WebApi.Services;
using WebApi.Validations;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();

/*builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("RedisConnection");
    options.InstanceName ="test";
});*/
builder.Configuration.AddEnvironmentVariables();
var connectionString = builder.Configuration.GetConnectionString("AzureSql");
Console.WriteLine($"connectionString:{connectionString}");

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'AzureSql' was not found in configuration.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions=>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    });
});

builder.Services.AddSwaggerGen();

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddValidatorsFromAssemblyContaining<CreatePostValidator>();

builder.Services.AddScoped<ICacheService, CacheService>();

builder.Services.AddScoped<IPostService, PostService>();

builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();
});

builder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionString: connectionString,
        name: "sql",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "ready" })
    .AddCheck("self", () => HealthCheckResult.Healthy(),
        tags: new[] { "live" });

var app = builder.Build();

app.MapOpenApi();

app.UseSwagger();

app.UseSwaggerUI();

app.ApplyMigration();

// configure health check endpoints
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health");

// Add graceful shutdown
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
lifetime.ApplicationStopping.Register(() =>
{
    // Add delay to allow in-flight requests to complete.
    Thread.Sleep(TimeSpan.FromSeconds(10));
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
