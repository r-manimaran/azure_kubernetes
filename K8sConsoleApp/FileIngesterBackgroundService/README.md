# Azure Blob Storage File Ingester Background  using .Net

A .NET 9 background service that generates synthetic user data and uploads CSV files to Azure Blob Storage using Azure Key Vault for secure credential management.

## Overview

This application runs as a background service that:
- Generates random user records using the Bogus library
- Creates CSV files with user data every 30 seconds
- Uploads files to Azure Blob Storage
- Manages secrets securely through Azure Key Vault
- Implements retry logic and caching for reliability

## Architecture

```
FileIngesterBackgroundService/
├── Models/
│   └── UserRecord.cs          # Data model for user records
├── Services/
│   ├── BlobService.cs         # Azure Blob Storage operations
│   └── KeyVaultService.cs     # Azure Key Vault integration
├── FileIngester.cs            # Main background service
├── Program.cs                 # Application entry point
└── appsettings.json          # Configuration settings
```

## Features

- **Background Processing**: Continuous file generation and upload
- **Secure Configuration**: Azure Key Vault integration for connection strings
- **Retry Logic**: Built-in retry mechanism for blob operations
- **Memory Caching**: Caches Key Vault secrets to reduce API calls
- **Synthetic Data**: Generates realistic user data using Bogus
- **Configurable**: Environment-based configuration support

## Prerequisites

- .NET 9 SDK
- Azure Storage Account
- Azure Key Vault
- Azure CLI (for authentication)

## Configuration

### Azure Key Vault Setup

1. Create an Azure Key Vault
2. Store your storage connection string as a secret named `StorageConnectionString`
3. Configure authentication (Managed Identity or Service Principal)

### Application Settings

Update `appsettings.json`:

```json
{
  "Azure": {
    "BlobUri": "https://yourstorageaccount.blob.core.windows.net/",
    "ContainerName": "userrecords",
    "KeyVault": {
      "Uri": "https://your-keyvault.vault.azure.net/",
      "SecretNames": {
        "StorageConnectionString": "StorageConnectionString"
      },
      "CacheDurationMinutes": 30,
      "RotationCheckIntervalMinutes": 60
    }
  }
}
```

### Environment Variables

Alternatively, set these environment variables:
- `KeyVaultUri`: Azure Key Vault URL
- `BlobUri`: Azure Blob Storage URL
- `StorageContainerName`: Container name for file uploads

## Installation & Running

1. Clone the repository
2. Navigate to the project directory
3. Restore dependencies:
   ```bash
   dotnet restore
   ```
4. Configure Azure authentication:
   ```bash
   az login
   ```
5. Run the application:
   ```bash
   dotnet run
   ```

## Dependencies

- **Azure.Storage.Blobs** (12.25.0) - Azure Blob Storage client
- **Azure.Security.KeyVault.Secrets** (4.8.0) - Key Vault integration
- **Azure.Identity** (1.14.2) - Azure authentication
- **Bogus** (35.6.3) - Synthetic data generation

## Generated Data Format

CSV files contain the following columns:
- FirstName
- LastName
- UserName (generated as first letter of last name + first name)
- Email (username@example.com)
- CreatedOn (UTC timestamp)

## Monitoring

The application logs:
- File generation and upload activities
- Key Vault secret retrieval
- Blob storage operations
- Error handling and retry attempts

## Security

- Connection strings stored securely in Azure Key Vault
- Secrets cached with 30-minute expiration for rotation support
- DefaultAzureCredential for authentication
- No sensitive data in configuration files
- Automatic connection refresh on authentication failures
- Zero-downtime credential rotation

## Connection String Rotation

### Automated Rotation Strategy

The application supports automatic Azure Storage connection string rotation with zero downtime:

#### Azure Automation Setup

1. Create Azure Automation Account
2. Add PowerShell runbook for rotation:

```powershell
param(
    [string]$StorageAccountName,
    [string]$ResourceGroupName,
    [string]$KeyVaultName,
    [string]$SecretName = "StorageConnectionString"
)

# Rotate storage account key
$keys = Invoke-AzStorageAccountKeyRotation -ResourceGroupName $ResourceGroupName -Name $StorageAccountName -KeyName "key1"

# Update Key Vault secret
$connectionString = "DefaultEndpointsProtocol=https;AccountName=$StorageAccountName;AccountKey=$($keys.Keys[0].Value);EndpointSuffix=core.windows.net"
Set-AzKeyVaultSecret -VaultName $KeyVaultName -Name $SecretName -SecretValue (ConvertTo-SecureString $connectionString -AsPlainText -Force)

Write-Output "Storage key rotated and Key Vault updated successfully"
```

3. Schedule runbook to run monthly/quarterly

#### Manual Rotation

```bash
# Rotate primary key
az storage account keys renew --account-name <storage-account> --key primary

# Get new connection string
az storage account show-connection-string --name <storage-account> --resource-group <rg-name>

# Update Key Vault
az keyvault secret set --vault-name <keyvault-name> --name "StorageConnectionString" --value "<new-connection-string>"
```

### How It Works

1. **Detection**: Application detects authentication failures during blob operations
2. **Cache Invalidation**: Automatically clears cached connection string
3. **Refresh**: Retrieves new connection string from Key Vault
4. **Retry**: Retries failed operation with new credentials
5. **Logging**: All rotation events are logged for monitoring

### Testing Rotation Behavior

#### Test 1: Manual Key Rotation

```bash
# 1. Start the application
dotnet run

# 2. Wait for successful file uploads (check logs)

# 3. Rotate storage key
az storage account keys renew --account-name <your-storage-account> --key primary

# 4. Update Key Vault (simulate automation)
az keyvault secret set --vault-name <your-keyvault> --name "StorageConnectionString" --value "<new-connection-string>"

# 5. Observe logs - should see:
# - Authentication errors
# - Cache invalidation
# - Connection refresh
# - Successful retry
```

#### Test 2: Cache Expiration Test

```bash
# 1. Set short cache duration in appsettings.json
"CacheDurationMinutes": 2

# 2. Run application and monitor Key Vault access logs
# 3. Verify cache refresh every 2 minutes
```

#### Test 3: Simulated Failure

```bash
# 1. Temporarily update Key Vault with invalid connection string
az keyvault secret set --vault-name <keyvault> --name "StorageConnectionString" --value "invalid-connection"

# 2. Observe retry behavior and error handling
# 3. Restore valid connection string
# 4. Verify automatic recovery
```

### Monitoring Rotation Events

Look for these log messages:
- `"Authentication error, refreshing connection"`
- `"Invalidated cached secret StorageConnectionString"`
- `"Blob service connection refreshed"`
- `"Retrieved secret StorageConnectionString from cache"`

### Configuration for Rotation

```json
{
  "Azure": {
    "KeyVault": {
      "CacheDurationMinutes": 30,
      "RotationCheckIntervalMinutes": 60
    }
  }
}
```