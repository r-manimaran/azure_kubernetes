using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileProcessingApp.Services;

public class ProcessingService
{
    private readonly AppSettings _settings;
    private readonly SecretProviderService _secretProviderService;

    public ProcessingService(IOptions<AppSettings> settings, SecretProviderService secretProviderService)
    {
        _settings = settings.Value;
        _secretProviderService = secretProviderService;
    }

    public async Task RunAsync()
    {
        string blobConnStr = await _secretProviderService.GetSecretAsync("BlobStorageConnectionString");
        string queueConnStr = await _secretProviderService.GetSecretAsync("QueueConnectionString");
        string dbConnStr = await _secretProviderService.GetSecretAsync("PostgresConnectionString");

        Console.WriteLine($"Processing files from Queue:{_settings.QueueName}");
    }
}
