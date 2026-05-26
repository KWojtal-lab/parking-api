using Microsoft.EntityFrameworkCore;
using Parking.Api.Entities;
using System.Linq;

namespace Parking.Api.Data;

public class ParkingDbContext(DbContextOptions<ParkingDbContext> options) : DbContext(options)
{
  public DbSet<Vehicle> Vehicles => Set<Vehicle>();
  public DbSet<ParkingSession> ParkingSessions => Set<ParkingSession>();
  public DbSet<ParkingSettings> ParkingSettings => Set<ParkingSettings>();
  public DbSet<ParkingSpot> ParkingSpots => Set<ParkingSpot>();
  public DbSet<UserLicensePlate> UserLicensePlates => Set<UserLicensePlate>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<Vehicle>(entity =>
    {
      entity.HasKey(v => v.PlateNumber);
      entity.Property(v => v.PlateNumber).HasMaxLength(20);
      entity.Property(v => v.VehicleType)
              .HasConversion(
                  value => value.ToString(),
                  value => ParseVehicleType(value))
              .HasMaxLength(20);
    });

    modelBuilder.Entity<ParkingSession>(entity =>
    {
      entity.HasKey(s => s.Id);
      entity.Property(s => s.UserId).HasMaxLength(100);
      entity.Property(s => s.PlateNumber).HasMaxLength(20);

      entity.HasOne(s => s.Vehicle)
              .WithMany(v => v.ParkingSessions)
              .HasForeignKey(s => s.PlateNumber)
              .OnDelete(DeleteBehavior.Restrict);

      entity.HasOne(s => s.ParkingSpot)
              .WithMany(p => p.ParkingSessions)
              .HasForeignKey(s => s.SpotNumber)
              .OnDelete(DeleteBehavior.Restrict);

      entity.HasIndex(s => s.UserId);
      entity.HasIndex(s => s.PlateNumber);
      entity.HasIndex(s => s.SpotNumber);
      entity.HasIndex(s => s.EndTime);
    });

    modelBuilder.Entity<ParkingSpot>(entity =>
    {
      entity.HasKey(p => p.Id);
    });

    modelBuilder.Entity<UserLicensePlate>(entity =>
    {
      entity.HasKey(p => new { p.UserId, p.PlateNumber });
      entity.Property(p => p.UserId).HasMaxLength(100);
      entity.Property(p => p.PlateNumber).HasMaxLength(20);
      entity.Property(p => p.VehicleType)
              .HasConversion(
                  value => value.ToString(),
                  value => ParseVehicleType(value))
              .HasMaxLength(20);
      entity.HasIndex(p => p.UserId);
    });

    modelBuilder.Entity<ParkingSettings>(entity =>
    {
      entity.HasKey(s => s.Id);
      entity.Property(s => s.FirstHourFee).HasColumnType("numeric(10,2)");
      entity.Property(s => s.SecondHourFee).HasColumnType("numeric(10,2)");
      entity.Property(s => s.EveryNextHourFee).HasColumnType("numeric(10,2)");

      entity.HasData(new ParkingSettings
      {
        Id = 1,
        FirstHourFee = 2m,
        SecondHourFee = 4m,
        EveryNextHourFee = 6m
      });
    });

    var spots = Enumerable.Range(1, 50)
      .Select(i => new ParkingSpot { Id = i, IsForDisabled = i >= 49 })
      .ToArray();

    modelBuilder.Entity<ParkingSpot>().HasData(spots);
  }

  private static VehicleType ParseVehicleType(string? value)
  {
    if (Enum.TryParse<VehicleType>(value, ignoreCase: true, out var parsed))
    {
      return parsed;
    }

    // Fallback for legacy bad values persisted before enum validation.
    return VehicleType.Sedan;
  }
}
