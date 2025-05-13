namespace ServiceLayer.Mesh.Configuration;

public class AppConfiguration : IFileDiscoveryFunctionConfiguration
{
    public string NbssMeshMailboxId => GetRequired("NbssMailboxId");

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
