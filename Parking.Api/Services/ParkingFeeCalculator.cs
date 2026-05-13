namespace Parking.Api.Services;

public static class ParkingFeeCalculator
{
  public static decimal CalculateFee(
      DateTime startTimeUtc,
      DateTime? endTimeUtc,
      decimal firstHourFee,
      decimal secondHourFee,
      decimal everyNextHourFee)
  {
    var effectiveEnd = endTimeUtc ?? DateTime.UtcNow;
    if (effectiveEnd < startTimeUtc)
    {
      return 0m;
    }

    var duration = effectiveEnd - startTimeUtc;
    var startedHours = Math.Max(1, (int)Math.Ceiling(duration.TotalHours));

    if (startedHours == 1)
    {
      return firstHourFee;
    }

    if (startedHours == 2)
    {
      return firstHourFee + secondHourFee;
    }

    return firstHourFee + secondHourFee + ((startedHours - 2) * everyNextHourFee);
  }
}
