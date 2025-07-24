
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using FileProcessingApp;
using FileProcessingApp.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

class Program
{
    public static async Task Main(string[] args)
    {

        var builder = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) =>
            {
                var appSettingsSection = context.Configuration.GetSection("AppSettings");
                services.Configure<AppSettings>(appSettingsSection);

                var keyVaultUrl = appSettingsSection["KeyVaultUrl"];
                services.AddSingleton(new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential()));
                services.AddSingleton<SecretProviderService>();

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