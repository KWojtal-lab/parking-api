using Microsoft.EntityFrameworkCore;

namespace ParkingApi.Models;

public class AppDbContext : DbContext
{
  public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

  public DbSet<Vehicle> Vehicles { get; set; }
  public DbSet<ParkingPlan> ParkingPlans { get; set; }
  public DbSet<Rental> Rentals { get; set; }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<Rental>()
        .HasOne(r => r.Vehicle)
        .WithOne(v => v.CurrentRental)
        .HasForeignKey<Rental>(r => r.VehicleId);

    modelBuilder.Entity<Rental>()
        .HasOne(r => r.ParkingPlan)
        .WithMany(p => p.Rentals)
        .HasForeignKey(r => r.ParkingPlansType);

    base.OnModelCreating(modelBuilder);
  }
}