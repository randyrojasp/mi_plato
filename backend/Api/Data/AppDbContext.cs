using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<GameItem> GameItems => Set<GameItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Room>().HasIndex(room => room.Code).IsUnique();
        modelBuilder.Entity<Product>().HasIndex(product => product.Name).IsUnique();
    }
}
