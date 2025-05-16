namespace ServiceLayer.Mesh.Configuration;

public class AppConfiguration :
    IFileDiscoveryFunctionConfiguration,
    IFileExtractFunctionConfiguration,
    IFileExtractQueueClientConfiguration,
    IFileTransformQueueClientConfiguration,
    IFileRetryFunctionConfiguration,
    IMeshHandshakeFunctionConfiguration
{
    public string NbssMeshMailboxId => GetRequired("NbssMailboxId");

    public string FileExtractQueueName => GetRequired("FileExtractQueueName");

    public string FileTransformQueueName => GetRequired("FileTransformQueueName");

    public int StaleHours => GetRequiredInt("StaleHours");

    private static string GetRequired(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);

        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidOperationException($"Environment variable '{key}' is not set or is empty.");
        }

        return value;
    }

    private static int GetRequiredInt(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);

        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidOperationException($"Environment variable '{key}' is not set or is empty.");
        }

        int intValue;

        if (int.TryParse(value, out intValue))
        {
            return intValue;
        }
        else
        {
            throw new InvalidOperationException($"Environment variable '{key}' is not a valid integer");
        }
    }
}
