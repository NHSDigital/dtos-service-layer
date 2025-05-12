using Microsoft.EntityFrameworkCore;
using ServiceLayer.API.Models;

namespace ServiceLayer.API.Data;

public class ServiceLayerDbContext(DbContextOptions<ServiceLayerDbContext> options) : DbContext(options)
{
    public DbSet<BSSelectEpisode> BSSelectEpisodes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BSSelectEpisode>().HasKey(e => e.EpisodeId);
    }
}
