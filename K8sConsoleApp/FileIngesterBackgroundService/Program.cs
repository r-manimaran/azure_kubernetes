using Azure.Identity;
using FileIngesterBackgroundService;
using FileIngesterBackgroundService.Services;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);

// Add memory cache for storing secrets
builder.Services.AddMemoryCache();

// Get the connection string from configuration or environment
// Replaced below using KeyVault Logic
//string storageConnectionString = builder.Configuration.GetConnectionString("AzureStorageAccount") 
//    ?? Environment.GetEnvironmentVariable("AzureStorageConnectionString") 
//    ?? throw new InvalidOperationException("Connection string 'AzureStorageAccount' not found.");

// Configure KeyVault
string keyVaultUrl = builder.Configuration["Azure:KeyVault:Uri"] 
                    ?? Environment.GetEnvironmentVariable("KeyVaultUri")
                    ?? throw new InvalidOperationException("Key Vault URI not found in configuation");

// Add Key Vault to configuration
builder.Configuration.AddAzureKeyVault(
           new Uri(keyVaultUrl),
    new DefaultAzureCredential());

// Register KeyVault Service
builder.Services.AddSingleton<KeyVaultService>(sp =>
        new KeyVaultService(keyVaultUrl,
                            sp.GetRequiredService<IMemoryCache>(),
                            sp.GetRequiredService<ILogger<KeyVaultService>>()));

// Register BlobService with KeyVault Service for rotation support
builder.Services.AddSingleton<BlobService>(sp =>
{
    var keyVaultService = sp.GetRequiredService<KeyVaultService>();
    
    var logger = sp.GetRequiredService<ILogger<BlobService>>();

    string secretName = builder.Configuration["Azure:KeyVault:SecretNames:StorageConnectionString"] ?? "StorageConnectionString";
    
    return new BlobService(keyVaultService, secretName, logger);
});

// Register BlobService with the connection string
// Replaced this logic using Connection string from Azure Key Vault
//builder.Services.AddSingleton<BlobService>(sp => 
    //new BlobService(storageConnectionString, sp.GetRequiredService<ILogger<BlobService>>()));

builder.Services.AddHostedService<FileIngester>();

var app = builder.Build();

app.UseHttpsRedirection();

app.Run();

