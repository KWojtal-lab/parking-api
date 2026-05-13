using System.ComponentModel.DataAnnotations.Schema;

namespace Parking.Api.Entities;

public class ParkingSession
{
  public Guid Id { get; set; }
  public string UserId { get; set; } = string.Empty;
  public string PlateNumber { get; set; } = string.Empty;

  [ForeignKey(nameof(ParkingSpot))]
  public int SpotNumber { get; set; }

  public ParkingSpot? ParkingSpot { get; set; }

  public DateTime StartTime { get; set; }
  public DateTime? EndTime { get; set; }

  public Vehicle? Vehicle { get; set; }
}
