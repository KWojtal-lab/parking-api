namespace Parking.Api.Entities;

public class ParkingSettings
{
  public int Id { get; set; }
  public decimal FirstHourFee { get; set; }
  public decimal SecondHourFee { get; set; }
  public decimal EveryNextHourFee { get; set; }
}
