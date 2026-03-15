using System.ComponentModel.DataAnnotations;

namespace ParkingApi.Models;

public class Vehicle
{
  [Key]
  public string LicensePlate { get; set; } = null!;
  public string Type { get; set; } = null!;
  public string? Owner { get; set; }

  public Rental? CurrentRental { get; set; }
}