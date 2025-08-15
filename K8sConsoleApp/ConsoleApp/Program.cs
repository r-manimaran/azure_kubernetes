
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using FileProcessingApp;
using FileProcessingApp.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog.Extensions.Hosting;
using Serilog;
using OpenTelemetry.Logs;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

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

                var keyVaultUrl = string.IsNullOrEmpty(appSettingsSection["KeyVaultUrl"])
                                    ? Environment.GetEnvironmentVariable("KeyVaultUrl")
                                    : appSettingsSection["KeyVaultUrl"];

                // Debug logging
                Console.WriteLine($"AppSettings KeyVaultUrl: '{appSettingsSection["KeyVaultUrl"]}'");
                Console.WriteLine($"Environment KeyVaultUrl: '{Environment.GetEnvironmentVariable("KeyVaultUrl")}'");
                Console.WriteLine($"Final KeyVaultUrl: '{keyVaultUrl}'");

                if (string.IsNullOrEmpty(keyVaultUrl))
                {
                    throw new Exception("KeyVaultUrl is not set in appsettings.json or environment variables");
                }

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

                var seqUrl = Environment.GetEnvironmentVariable("SEQ_URL") ?? "http://localhost:5341";
                var jaegerEndpoint = Environment.GetEnvironmentVariable("JAEGER_ENDPOINT") ?? "http://localhost:4317";


                // Add OpenTelemetry or other logging services if needed
                services.AddOpenTelemetry()
                .ConfigureResource(resourceBuilder =>
                    resourceBuilder.AddService("FileProcessingApp", "1.0.0")
                    .AddAttributes(new Dictionary<string, object>
                    {
                        ["deployment.environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                        ["service.instance.id"] = Environment.MachineName
                    }))

                .WithTracing(tracing => tracing
                    .AddSource("FileProcessingApp")
                    .AddHttpClientInstrumentation()
                    .AddOtlpExporter(otlpOptions =>
                    {
                        otlpOptions.Endpoint = new Uri(jaegerEndpoint);
                    }))
                .WithLogging(logging => logging
                    .AddConsoleExporter()
                    .AddOtlpExporter(options =>
                    {
                        options.Endpoint = new Uri(jaegerEndpoint);
                    }));

            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddOpenTelemetry();
            })
        .UseSerilog((context, services, config) =>
         {
             var seqUrl = Environment.GetEnvironmentVariable("SEQ_URL") ?? "http://localhost:5341";
             
             Console.WriteLine($"Using Seq URL: {seqUrl}");

             config.ReadFrom.Configuration(context.Configuration)
                   .WriteTo.Console()
                   .WriteTo.Seq(seqUrl)
                   .Enrich.WithProperty("ServiceName", "FileProcessingApp")
                   .Enrich.WithProperty("MachineName", Environment.MachineName);
         });

        var host = builder.Build();

        var processor = host.Services.GetRequiredService<ProcessingService>();

        await processor.RunAsync();

    }
}