using ServiceLayer.Common;

namespace ServiceLayer.Mesh.Configuration;

public class AppConfiguration :
    IFileDiscoveryFunctionConfiguration,
    IFileExtractFunctionConfiguration,
    IFileExtractQueueClientConfiguration,
    IFileTransformQueueClientConfiguration,
    IFileTransformFunctionConfiguration,
    IFileRetryFunctionConfiguration,
    IMeshHandshakeFunctionConfiguration
{
    public string NbssMeshMailboxId => GetRequired("NbssMailboxId");

    public string FileExtractQueueName => GetRequired("FileExtractQueueName");

    public string FileTransformQueueName => GetRequired("FileTransformQueueName");

    public int StaleHours => GetRequiredInt("StaleHours");

    private static string GetRequired(string key)
    {
        var value = EnvironmentVariables.GetRequired(key);

        return value;
    }

    private static int GetRequiredInt(string key)
    {
        var value = GetRequired(key);

        if (!int.TryParse(value, out var intValue))
        {
            throw new InvalidOperationException($"Environment variable '{key}' is not a valid integer");
        }

        return intValue;
    }
}
