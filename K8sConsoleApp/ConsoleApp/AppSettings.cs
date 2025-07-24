using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileProcessingApp;

public class AppSettings
{
    public string KeyVaultUrl { get; set; } = string.Empty;
    public string QueueName { get; set; } = string.Empty;
    public string BlobContainerName { get; set; } = string.Empty;
}
