namespace Parking.Api.Entities;

public class ParkingSpot
{
  public int Id { get; set; }
  public bool IsForDisabled { get; set; }

  public ICollection<ParkingSession>? ParkingSessions { get; set; }
}
