using Azure.Security.KeyVault.Secrets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileProcessingApp.Services;

public class SecretProviderService
{
    private readonly SecretClient _secretClient;

    public SecretProviderService(SecretClient secretClient)
    {
        _secretClient = secretClient;
    }
    public async Task<string> GetSecretAsync(string secretName)
    {
        var response = await  _secretClient.GetSecretAsync(secretName);
        return response.Value.Value;
    }
}
