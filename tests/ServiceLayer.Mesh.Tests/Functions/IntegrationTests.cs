using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Moq;

namespace ServiceLayer.Mesh.Tests.Integration;

public class IntegrationTests
{
    private const string ConnectionString = "Server=localhost,1433;User Id=sa;Password=YourPassword123;TrustServerCertificate=True;";

    public IntegrationTests()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (environment == null)
        {
            throw new InvalidOperationException("ASPNETCORE_ENVIRONMENT environment variable is not set of is empty.");
        }
        if (environment == "development")
        {
            RunCommand("podman compose up");
        }
        if (environment == "production")
        {
            RunCommand("docker compose up");
        }

        // Wait for SQL Server to be reachable
        //await WaitForSqlServerAsync();
    }

    public void Teardown()
    {
        // Stop containers
        RunCommand("docker compose down");
    }

    [Fact]
    public async Task ShouldWriteToDatabase()
    {
        using var sql = new SqlConnection(ConnectionString);
        await sql.OpenAsync();

        // var count = await sql.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM SomeTable");
        // Assert.IsTrue(count > 0, "Expected at least one row in SomeTable.");
    }

    private void RunCommand(string command)
    {
        var psi = new ProcessStartInfo("cmd", $"/c {command}")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new Exception($"Command failed: {command}\n{process.StandardError.ReadToEnd()}");
        }
    }

    private async Task WaitForSqlServerAsync(int timeoutSeconds = 60)
    {
        var start = DateTime.UtcNow;
        while ((DateTime.UtcNow - start).TotalSeconds < timeoutSeconds)
        {
            try
            {
                using var sql = new SqlConnection(ConnectionString);
                await sql.OpenAsync();
                return; // Success
            }
            catch
            {
                await Task.Delay(1000);
            }
        }

        throw new TimeoutException("SQL Server did not become available in time.");
    }
}
