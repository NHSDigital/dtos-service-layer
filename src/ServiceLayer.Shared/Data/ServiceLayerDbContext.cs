using Microsoft.EntityFrameworkCore;
using ServiceLayer.Data.Models;

namespace ServiceLayer.Data;

public class ServiceLayerDbContext(DbContextOptions<ServiceLayerDbContext> options) : DbContext(options)
{
    public DbSet<MeshFile> MeshFiles { get; set; }
    public DbSet<NbssAppointmentEvent> NbssAppointmentEvents { get; set; }
    public DbSet<MeshFileEvent> MeshFileEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureMeshFiles(modelBuilder);
        ConfigureMeshFileEvents(modelBuilder);
        ConfigureNbssAppointmentEvents(modelBuilder);
    }

    private static void ConfigureMeshFiles(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MeshFile>().HasKey(p => p.FileId);
        modelBuilder.Entity<MeshFile>().Property(e => e.Status).HasConversion<string>();
        modelBuilder.Entity<MeshFile>().Property(e => e.FileType).HasConversion<string>();
    }

    private static void ConfigureMeshFileEvents(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MeshFileEvent>().HasKey(e => e.EventId);
        modelBuilder.Entity<MeshFileEvent>().Property(e => e.Status).HasConversion<string>();
        modelBuilder.Entity<MeshFileEvent>().Property(e => e.Source).HasConversion<string>();
        modelBuilder.Entity<MeshFileEvent>()
            .HasOne<MeshFile>()
            .WithMany()
            .HasForeignKey(e => e.FileId);
    }

    private static void ConfigureNbssAppointmentEvents(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NbssAppointmentEvent>().HasKey(e => e.Id);
        modelBuilder.Entity<NbssAppointmentEvent>()
            .HasOne<MeshFile>()
            .WithMany()
            .HasForeignKey(e => e.MeshFileId);
    }
}
