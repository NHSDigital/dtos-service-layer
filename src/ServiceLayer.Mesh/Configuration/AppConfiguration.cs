namespace ServiceLayer.Mesh.Configuration;

public class AppConfiguration :
    IFileDiscoveryFunctionConfiguration,
    IFileExtractQueueClientConfiguration,
    IFileTransformQueueClientConfiguration,
    IFileTransformFunctionConfiguration,
    IFileRetryFunctionConfiguration,
    IMeshHandshakeFunctionConfiguration,
    IValidationRunnerConfiguration
{
    public string NbssMeshMailboxId => GetRequired("NbssMailboxId");

    public string FileExtractQueueName => GetRequired("FileExtractQueueName");

    public string FileTransformQueueName => GetRequired("FileTransformQueueName");

    public int MaximumValidationErrors => GetOptionalInt("MaximumValidationErrors", 100);

    public int StaleHours => GetOptionalInt("StaleHours", 12);

    private static string GetRequired(string key) =>
        EnvironmentVariables.GetRequired(key);
    private static int GetOptionalInt(string key, int defaultValue) =>
        EnvironmentVariables.GetOptionalInt(key, defaultValue);
}
