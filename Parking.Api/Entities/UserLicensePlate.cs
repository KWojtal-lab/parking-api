namespace Parking.Api.Entities;

public class UserLicensePlate
{
  public string UserId { get; set; } = string.Empty;
  public string PlateNumber { get; set; } = string.Empty;
  public VehicleType VehicleType { get; set; } = VehicleType.Sedan;
}
