namespace ServiceLayer.Mesh.Configuration;

public class AppConfiguration :
    IFileDiscoveryFunctionConfiguration,
    IFileExtractFunctionConfiguration,
    IFileExtractQueueClientConfiguration,
    IFileTransformQueueClientConfiguration,
    IFileRetryFunctionConfiguration
{
    public string NbssMeshMailboxId => GetRequired("NbssMailboxId");

    public string FileExtractQueueName => GetRequired("FileExtractQueueName");

    public string FileTransformQueueName => GetRequired("FileTransformQueueName");

    public string StaleHours => GetRequired("StaleHours");

    private static string GetRequired(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);

        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidOperationException($"Environment variable '{key}' is not set or is empty.");
        }

        return value;
    }
}
