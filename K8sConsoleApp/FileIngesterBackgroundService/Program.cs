using FileIngesterBackgroundService;
using FileIngesterBackgroundService.Services;

var builder = WebApplication.CreateBuilder(args);

// Get the connection string from configuration or environment variable
string storageConnectionString = builder.Configuration.GetConnectionString("AzureStorageAccount") 
    ?? Environment.GetEnvironmentVariable("AzureStorageConnectionString") 
    ?? throw new InvalidOperationException("Connection string 'AzureStorageAccount' not found.");

// Register BlobService with the connection string
builder.Services.AddSingleton<BlobService>(sp => 
    new BlobService(storageConnectionString, sp.GetRequiredService<ILogger<BlobService>>()));

builder.Services.AddHostedService<FileIngester>();

var app = builder.Build();

app.UseHttpsRedirection();

app.Run();

