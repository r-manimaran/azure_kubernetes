using Azure.Storage.Blobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileProcessingApp.Services;

public class BlobService
{
    private readonly BlobContainerClient _blobContainerClient;
    public BlobService(string blobConnectionString, string containerName)
    {
        _blobContainerClient = new BlobContainerClient(blobConnectionString, containerName);
    }
    public async Task<string> DownloadBlobContentAsync(string blobName)
    {
        var blobClient = _blobContainerClient.GetBlobClient(blobName);
        var response = await blobClient.DownloadContentAsync();
        return response.Value.Content.ToString();
    }
}
