using Microsoft.EntityFrameworkCore;
using MoraTuk.API.Models;

namespace MoraTuk.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);


        modelBuilder.Entity<Ride>()
            .Property(x => x.Price)
            .HasPrecision(10, 2);
    }


    public DbSet<User> Users { get; set; }

    public DbSet<Driver> Drivers { get; set; }

    public DbSet<Ride> Rides { get; set; }
    public DbSet<Location> Locations { get; set; }
    public DbSet<UserLocation> UserLocations { get; set; }
    public DbSet<DriverLocation> DriverLocations { get; set; }
}