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

    public static int GetRequiredInt(string key)
    {
        var value = GetRequired(key);

        if (!int.TryParse(value, out var intValue))
        {
            throw new InvalidOperationException($"Environment variable '{key}' is not a valid integer");
        }

        return intValue;
    }

    public static int GetOptionalInt(string key, int defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(key);

        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        if (!int.TryParse(value, out var intValue))
        {
            throw new InvalidOperationException($"Environment variable '{key}' is not a valid integer");
        }

        return intValue;
    }
}
