
using Bogus;
using FileIngesterBackgroundService.Models;
using FileIngesterBackgroundService.Services;
using System.Text;

namespace FileIngesterBackgroundService;

public class FileIngester : BackgroundService
{
    private readonly BlobService _blobService;
    private readonly ILogger<FileIngester> _logger;
    private readonly IConfiguration _configuration;

    public FileIngester(BlobService blobService, ILogger<FileIngester> logger, IConfiguration configuration)
    {
        _blobService = blobService;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                string blobUri = Environment.GetEnvironmentVariable("BlobUri") ?? _configuration["Azure:BlobUri"] ?? string.Empty;
                string containerName = Environment.GetEnvironmentVariable("StorageContainerName") ?? _configuration["Azure:ContainerName"] ?? string.Empty;

                DateTime dateTimeNow = DateTime.Now;
                string fileName = $"UserRecord_{dateTimeNow:yyyyMMdd_HHmmss}.csv";
                string filePath = Path.Combine(Path.GetTempPath(), fileName);
                string blobFileName = Path.GetFileName(filePath);

                // Create a random number between 10 to 50
                int rndRecords = Random.Shared.Next(10, 50);
                List<UserRecord> records = GenerateUserRecords(rndRecords);
                // Generate a CSV File
                string csvFile = GenerateCsvFile(records);
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    await writer.WriteAsync(csvFile);
                }

                byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
                var blob = await _blobService.UploadBlobAsync(containerName, blobFileName, fileBytes);
                _logger.LogInformation($"Blob Uri:{blobUri}, ContainerName:{containerName}, FileName:{blobFileName}");
            }
            catch (Exception ex)
            {
                _logger.LogError("{Exception}:", ex.ToString());
            }
            Thread.Sleep(millisecondsTimeout: 30000); // Every half min it will create a file in Azure Storage
        }
    }
    /// <summary>
    /// Generate User Records
    /// </summary>
    /// <param name="maxRecords"></param>
    /// <returns></returns>
    private List<UserRecord> GenerateUserRecords(int maxRecords = 10)
    {
        return new Faker<UserRecord>()
            .RuleFor(e => e.FirstName, f => f.Name.FirstName())
            .RuleFor(e => e.LastName, f => f.Name.LastName())
            .RuleFor(e => e.UserName, (f, e) => $"{e.LastName.ToLower().Substring(0, 1)}{e.FirstName.ToLower()}")
            // Email should be firstcharacter of lastname + first name + "@example.com"
            .RuleFor(e => e.Email, (f, e) => $"{e.LastName.ToLower().Substring(0, 1)}{e.FirstName.ToLower()}@example.com")
            .RuleFor(e => e.CreatedOn, f => f.Date.Past(5).ToUniversalTime())
            .Generate(maxRecords);
    }

    /// <summary>
    /// Generate a CSV File string
    /// </summary>
    /// <param name="records"></param>
    /// <returns></returns>
    private string GenerateCsvFile(List<UserRecord> records)
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("FirstName,LastName,UserName,Email,CreatedOn");
        foreach (var record in records)
        {
            stringBuilder.AppendLine($"{record.FirstName}, {record.LastName}, {record.UserName}, {record.Email}, {record.CreatedOn}");
        }
        return stringBuilder.ToString();
    }
}
