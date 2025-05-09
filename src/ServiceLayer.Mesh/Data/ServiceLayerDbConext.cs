using Microsoft.EntityFrameworkCore;
using ServiceLayer.Mesh.Models;

namespace ServiceLayer.Mesh.Data;

public class ServiceLayerDbContext(DbContextOptions<ServiceLayerDbContext> options) : DbContext(options)
{
    public DbSet<MeshFile> MeshFiles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure relationships, keys, etc.
        modelBuilder.Entity<MeshFile>().HasKey(p => p.FileId);
        modelBuilder.Entity<MeshFile>().Property(e => e.Status).HasConversion<string>();
        modelBuilder.Entity<MeshFile>().Property(e => e.FileType).HasConversion<string>();
    }
}
