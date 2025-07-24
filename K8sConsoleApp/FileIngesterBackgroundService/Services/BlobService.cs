using Azure.Storage.Blobs;

namespace FileIngesterBackgroundService.Services;

public class BlobService
{
    private readonly ILogger<BlobService> _logger;
    private BlobServiceClient _blobServiceClient;
    private readonly KeyVaultService _keyVaultService;
    private readonly string _secretName;
    
    public BlobService(string connectionString, ILogger<BlobService> logger)
    {
        _blobServiceClient = new BlobServiceClient(connectionString);
        _logger = logger;
    }
    
    public BlobService(KeyVaultService keyVaultService, string secretName, ILogger<BlobService> logger)
    {
        _keyVaultService = keyVaultService;
        _secretName = secretName;
        _logger = logger;
        RefreshConnection();
    }
    
    private void RefreshConnection()
    {
        var connectionString = _keyVaultService.GetSecretAsync(_secretName).GetAwaiter().GetResult();
        _blobServiceClient = new BlobServiceClient(connectionString);
        _logger.LogInformation("Blob service connection refreshed");
    }

    public async Task EnsureContainerAsync(string containerName)
    {
        _logger?.LogInformation("Ensuring container: {ContainerName}", containerName);
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync();
    }
    public async Task<bool> ContainerExistsAsync(string containerName)
    {
        _logger?.LogInformation("Checking if container exists: {ContainerName}", containerName);
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        return await containerClient.ExistsAsync();
    }
    public async Task<List<string>> GetContainerListAsync()
    {
        _logger?.LogInformation("Retrieving list of containers");
        var containers = _blobServiceClient.GetBlobContainersAsync();
        var containerList = new List<string>();
        await foreach (var container in containers)
        {
            containerList.Add(container.Name);
        }
        return containerList;
    }

    public async Task DeleteContainerAsync(string containerName)
    {
        _logger?.LogInformation("Deleting container: {ContainerName}", containerName);
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.DeleteIfExistsAsync();
    }

    public async Task<bool> CheckBlobExistsAsync(string containerName, string blobName)
    {
        _logger?.LogInformation("Checking if blob exists: {BlobName} in container: {ContainerName}", blobName, containerName);
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        return await blobClient.ExistsAsync();
    }
    public async Task<List<string>> GetBlobListAsync(string containerName)
    {
        _logger?.LogInformation("Retrieving list of blobs in container: {ContainerName}", containerName);
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobs = containerClient.GetBlobsAsync();
        var blobList = new List<string>();

        await foreach (var blob in blobs)
        {
            blobList.Add(blob.Name);
        }
        return blobList;
    }
    public async Task<string> UploadBlobAsync(string containerName, string blobName, byte[] content)
    {
        _logger?.LogInformation("Uploading blob: {BlobName} to container: {ContainerName}", blobName, containerName);
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync();

        var blobClient = containerClient.GetBlobClient(blobName);
        using var stream = new MemoryStream(content);
        // Retry logic can be added
        await RetryAsync(async () =>
        {
            await blobClient.UploadAsync(stream, overwrite: true);
        });

        return blobClient.Uri.ToString();
    }

    public async Task<byte[]> DownloadBlobAsync(string containerName, string blobName)
    {
        _logger?.LogInformation("Downloading blob: {BlobName} from container: {ContainerName}", blobName, containerName);
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        return await RetryAsync(async () =>
        {
            var response = await blobClient.DownloadAsync();
            using var memoryStream = new MemoryStream();
            await response.Value.Content.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        });

    }
    public async Task DeleteBlobAsync(string containerName, string blobName)
    {
        _logger?.LogInformation("Deleting blob: {BlobName} from container: {ContainerName}", blobName, containerName);
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        await blobClient.DeleteIfExistsAsync();
    }
    private async Task RetryAsync(Func<Task> action, int maxRetries = 3, int delayMilliseconds = 1000)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            _logger?.LogInformation("Attempt {Attempt} of {MaxRetries}", i + 1, maxRetries);
            try
            {
                await action();
                return;
            }
            catch (Exception ex) when (i < maxRetries - 1 && IsAuthenticationError(ex))
            {
                _logger.LogWarning("Authentication error, refreshing connection: {Error}", ex.Message);
                if (_keyVaultService != null)
                {
                    _keyVaultService.InvalidateSecret(_secretName);
                    RefreshConnection();
                }
                await Task.Delay(delayMilliseconds);
            }
            catch (Exception ex) when (i < maxRetries - 1)
            {
                await Task.Delay(delayMilliseconds);
            }
        }
        throw new InvalidOperationException("Operation failed after maximum retries.");
    }
    
    private bool IsAuthenticationError(Exception ex)
    {
        return ex.Message.Contains("authentication", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("unauthorized", StringComparison.OrdinalIgnoreCase) ||
               ex.Message.Contains("forbidden", StringComparison.OrdinalIgnoreCase);
    }
    private async Task<T> RetryAsync<T>(Func<Task<T>> action, int maxRetries = 3, int delayMilliseconds = 1000)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            _logger?.LogInformation("Attempt {Attempt} of {MaxRetries}", i + 1, maxRetries);
            try
            {
                return await action();
            }
            catch (Exception ex) when (i < maxRetries - 1)
            {
                await Task.Delay(delayMilliseconds);
            }
        }
        throw new InvalidOperationException("Operation failed after maximum retries.");
    }

}
