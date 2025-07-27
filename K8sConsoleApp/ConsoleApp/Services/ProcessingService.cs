using Azure.Storage.Blobs.Models;
using FileProcessingApp.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FileProcessingApp.Services;

public class ProcessingService
{
    private readonly AppSettings _settings;
    private readonly SecretProviderService _secretProviderService;
    private readonly BlobService _blobService;
    private readonly QueueService _queueService;
    private readonly ILogger<ProcessingService> _logger;

    public ProcessingService(IOptions<AppSettings> settings, SecretProviderService secretProviderService,BlobService blobService,
        QueueService queueService, ILogger<ProcessingService> logger)
    {
        _settings = settings.Value;
        _secretProviderService = secretProviderService;
        _blobService = blobService;
        _queueService = queueService;
        _logger = logger;
    }

    public async Task RunAsync()
    {

        _logger.LogInformation("Processing files from {Queue}",_settings.QueueName);

        while (true)
        {
            try
            {
                string? messageContent = await _queueService.ReceiveMessageAsync();
                if(messageContent == null)
                {
                    _logger.LogInformation("No messages in queue. Waiting...");
                    await Task.Delay(5000);
                    continue;
                }

                // Deserialize queue message
                // Deserialize the message content to a strongly typed object
                var eventType = new { topic = "", subject = "", eventType = "", id = "", data = new { url = "", blobType = "", eTag = "", contentLength = "" } };
                var fileIngestData = JsonConvert.DeserializeAnonymousType(messageContent, eventType);

                FileQueueMessage fileQueueMessage = new FileQueueMessage();

                if (fileIngestData?.data != null)
                {
                    string blobUrl = fileIngestData.data.url;
                    fileQueueMessage.BlobFileUrl = blobUrl;
                    fileQueueMessage.BlobType = fileIngestData.data.blobType;
                    fileQueueMessage.ContainerName = Utilities.GetContainerName(blobUrl);
                    fileQueueMessage.BlobFileName = Utilities.GetBlobFileName(blobUrl);
                    fileQueueMessage.BlobFileExtension = Utilities.GetBlobFileExtension(blobUrl);

                    _logger.LogInformation("File Ingest Data: {BlobFileUrl}, {BlobType}, {ContainerName}, {BlobFileName}, {BlobFileExtension}",
                        fileQueueMessage.BlobFileUrl,
                        fileQueueMessage.BlobType,
                        fileQueueMessage.ContainerName,
                        fileQueueMessage.BlobFileName,
                        fileQueueMessage.BlobFileExtension);

                    _logger.LogInformation("Processing blob:{BlobFileName}", fileQueueMessage.BlobFileName);

                    // Download CSV content from blob
                    string csvContent = await _blobService.DownloadBlobContentAsync(fileQueueMessage.BlobFileName);

                    //parse and log csv records
                    var userRecords = ParseCSVContent(csvContent);
                    _logger.LogInformation("Found:{userRecordsCount} records in {BlobFile}", userRecords.Count, fileQueueMessage.BlobFileName);

                    foreach (var userRecord in userRecords)
                    {
                        _logger.LogInformation($"User: {userRecord.UserName}, Email:{userRecord.Email},CreatedOn:{userRecord.CreatedOn}");
                    }
                    _logger.LogInformation("Completed Processing {BlobFileName}", fileQueueMessage.BlobFileName);
                    _logger.LogInformation("  ");
                    _logger.LogInformation("-----------------------------------------------------------------------");
                }
            }
            catch(Exception ex)
            {
                _logger.LogInformation($"Error: {ex.Message}");
                await Task.Delay(5000);
            }
        }
    }

    private List<UserRecord> ParseCSVContent(string csvContent)
    {
        _logger.LogInformation("Parsing CSV File Content");
       var records = new List<UserRecord>();
       var lines = csvContent.Split('\n',StringSplitOptions.RemoveEmptyEntries);

        //skip the header row
        for (int i = 1; i < lines.Length; i++)
        {
            var columns = lines[i].Split(',');
            if (columns.Length >= 5)
            {
                records.Add(new UserRecord
                {
                    UserName = columns[2].Trim(),
                    Email = columns[3].Trim(),
                    CreatedOn = DateTime.TryParse(columns[4].Trim(), out DateTime createdOn) ? createdOn : DateTime.MinValue
                });
            }
        }
        return records;
    }
}
