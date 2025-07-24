using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Caching.Memory;

namespace FileIngesterBackgroundService.Services;

public class KeyVaultService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<KeyVaultService> _logger;
    private readonly SecretClient _secretClient;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(30); // Reduced for rotation
    public KeyVaultService(string keyValultUrl, IMemoryCache memoryCache, ILogger<KeyVaultService> logger)
    {
        _secretClient = new SecretClient(new Uri(keyValultUrl), new DefaultAzureCredential());
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async Task<string> GetSecretAsync(string secretName)
    {
        // Try to get from Cache first
        if(_memoryCache.TryGetValue(secretName, out string cachedSecret))
        {
            _logger.LogInformation("Retrieved secret {secretName} from cache", secretName);
            return cachedSecret;
        }

        // If not in cache
        _logger.LogInformation("Retrieving secret {SecretName} from Azure Key Vault", secretName);
        var secret = await _secretClient.GetSecretAsync(secretName);

        // Cache the secret
        _memoryCache.Set(secretName, secret.Value, _cacheDuration);
        return secret.Value.Value;

    }

    public void InvalidateSecret(string secretName)
    {
        _memoryCache.Remove(secretName);
        _logger.LogInformation("Invalidated cached secret {SecretName}", secretName);
    }
}
