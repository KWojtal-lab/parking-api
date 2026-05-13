namespace Parking.Api.Entities;

public class Vehicle
{
  public string PlateNumber { get; set; } = string.Empty;
  public VehicleType VehicleType { get; set; }

  public ICollection<ParkingSession> ParkingSessions { get; set; } = new List<ParkingSession>();
}
