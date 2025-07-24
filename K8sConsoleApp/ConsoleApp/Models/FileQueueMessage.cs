using Azure.Storage.Queues.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileProcessingApp.Models;

internal class FileQueueMessage
{
    public string BlobFileUrl { get; set; } = string.Empty;
    public string BlobType { get; set; } = string.Empty;
    public string ContainerName { get; set; } = string.Empty;
    public string BlobFileName { get; set;} = string.Empty;
    public QueueMessage QueueMessage { get; set; }
    public string BlobFileExtension { get; set; } = string.Empty;
}
