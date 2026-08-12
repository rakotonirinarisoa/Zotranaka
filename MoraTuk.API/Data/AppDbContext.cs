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
        modelBuilder.Entity<Payment>()
            .Property(p => p.Amount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Ride)
            .WithOne()
            .HasForeignKey<Payment>(p => p.RideId);
        modelBuilder.Entity<DriverEarning>()
            .Property(x => x.GrossAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<DriverEarning>()
            .Property(x => x.CommissionAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<DriverEarning>()
            .Property(x => x.WaitingFeeAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<DriverEarning>()
            .Property(x => x.DriverAmount)
            .HasPrecision(18, 2);
        modelBuilder.Entity<DriverEarning>()
            .HasOne(x => x.Ride)
            .WithMany()
            .HasForeignKey(x => x.RideId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DriverEarning>()
            .HasOne(x => x.Driver)
            .WithMany()
            .HasForeignKey(x => x.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DriverEarning>()
            .HasOne(x => x.Payment)
            .WithMany()
            .HasForeignKey(x => x.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DriverPayout>()
            .Property(x => x.GrossAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<DriverPayout>()
            .Property(x => x.CommissionAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<DriverPayout>()
            .Property(x => x.WaitingFeeAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<DriverPayout>()
            .Property(x => x.DriverAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<DriverPayout>()
            .HasOne(x => x.Driver)
            .WithMany()
            .HasForeignKey(x => x.DriverId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DriverEarning>()
            .HasOne(x => x.DriverPayout)
            .WithMany(x => x.Earnings)
            .HasForeignKey(x => x.DriverPayoutId)
        .OnDelete(DeleteBehavior.SetNull);
    }


    public DbSet<User> Users { get; set; }

    public DbSet<Driver> Drivers { get; set; }

    public DbSet<Ride> Rides { get; set; }

    public DbSet<Location> Locations { get; set; }

    public DbSet<UserLocation> UserLocations { get; set; }

    public DbSet<DriverLocation> DriverLocations { get; set; }

    public DbSet<Payment> Payments { get; set; }

    public DbSet<DriverEarning> DriverEarnings { get; set; }

    public DbSet<DriverPayout> DriverPayouts { get; set; }
}