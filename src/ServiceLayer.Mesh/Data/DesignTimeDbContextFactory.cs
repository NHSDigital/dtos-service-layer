using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ServiceLayer.Mesh.Data;

namespace ParticipantManager.API.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ServiceLayerDbContext>
{
    public ServiceLayerDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("DatabaseConnectionString");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DatabaseConnectionString' is not configured.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<ServiceLayerDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new ServiceLayerDbContext(optionsBuilder.Options);
    }
}
