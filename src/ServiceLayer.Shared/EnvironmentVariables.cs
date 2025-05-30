namespace ServiceLayer;

public static class EnvironmentVariables
{
    /// <summary>
    /// Gets an environment variable by name. Throws if not found.
    /// </summary>
    /// <param name="key">The name of the environment variable.</param>
    /// <returns>The value of the environment variable.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the variable is not found or is empty.</exception>
    public static string GetRequired(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);

        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidOperationException($"Environment variable '{key}' is not set or is empty.");
        }

        return value;
    }
}
