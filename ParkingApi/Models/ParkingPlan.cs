using System.ComponentModel.DataAnnotations;

namespace ParkingApi.Models;

public class ParkingPlan
{
  [Key]
  public string Type { get; set; } = null!;
  public int? PeakStartHour { get; set; }
  public int? PeakEndHour { get; set; }
  public int? HourNumber { get; set; }
  public int? Duration { get; set; }
  public decimal Price { get; set; }

  public ICollection<Rental> Rentals { get; set; } = new List<Rental>();
}