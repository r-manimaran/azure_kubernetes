
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using FileProcessingApp;
using FileProcessingApp.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;

class Program
{
    public static async Task Main(string[] args)
    {

        var builder = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddUserSecrets<Program>();
                config.AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) =>
            {
                var appSettingsSection = context.Configuration.GetSection("AppSettings");
                services.Configure<AppSettings>(appSettingsSection);

                var keyVaultUrl = appSettingsSection["KeyVaultUrl"]??Environment.GetEnvironmentVariable("KeyVaultUrl");
                services.AddSingleton(new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential()));
                services.AddSingleton<SecretProviderService>();

                services.AddSingleton<QueueService>(sp =>
                {
                    var secretProvider = sp.GetRequiredService<SecretProviderService>();
                    
                    var settings = sp.GetRequiredService<IOptions<AppSettings>>().Value;
                    
                    var queueConnStr = secretProvider.GetSecretAsync("BlobStorageConnectionString").GetAwaiter().GetResult();
                    
                    return new QueueService(queueConnStr, settings.QueueName);
                });

                services.AddSingleton<BlobService>(sp =>
                {
                    var secretProvider = sp.GetRequiredService<SecretProviderService>();

                    var settings = sp.GetRequiredService<IOptions<AppSettings>>().Value;

                    var blobConnStr = secretProvider.GetSecretAsync("BlobStorageConnectionString").GetAwaiter().GetResult();

                    return new BlobService(blobConnStr, settings.BlobContainerName);
                });

                // Register custom services
                services.AddTransient<ProcessingService>();
            })            
            .UseSerilog((context, services, config) =>
            {
                config.ReadFrom.Configuration(context.Configuration);
            });

        var host = builder.Build();

        var processor = host.Services.GetRequiredService<ProcessingService>();

        await processor.RunAsync();

    }
}