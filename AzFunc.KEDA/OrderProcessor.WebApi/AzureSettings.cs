namespace OrderProcessor.WebApi;

public class AzureSettings
{
    public static string SectionName = "AzureSettings";
    public string QueueConnectionString { get; set; } = string.Empty;
    public string QueueName { get; set; } = string.Empty;
}
