using System.ComponentModel.DataAnnotations;

namespace ParkingApi.Models;

public class Rental
{
  [Key]
  public int Id { get; set; }
  public DateTime StartTime { get; set; }
  public DateTime? EndTime { get; set; }
  public int ParkingSpotNumber { get; set; }

  public string VehicleId { get; set; } = null!;
  public Vehicle Vehicle { get; set; } = null!;

  public string ParkingPlansType { get; set; } = null!;
  public ParkingPlan ParkingPlan { get; set; } = null!;
}