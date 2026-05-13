using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Parking.Api.Contracts;
using Parking.Api.Data;
using Parking.Api.Services;

namespace Parking.Api.Controllers.Operator;

[ApiController]
[Authorize(Roles = "Operator")]
[Route("api/operator/statistics")]
public class StatisticsController(
  ParkingDbContext dbContext,
  ParkingSettingsProvider settingsProvider) : ControllerBase
{
  [HttpGet("revenue")]
  public async Task<IActionResult> GetRevenue([FromQuery] string? period)
  {
    if (!TryParsePeriod(period, out var parsedPeriod))
    {
      return BadRequest("Period must be 'weekly' or 'monthly'.");
    }

    var (startDate, endDate, days) = GetDateRange(parsedPeriod);
    var sessions = await dbContext.ParkingSessions
        .Where(s => s.EndTime != null && s.EndTime >= startDate && s.EndTime < endDate.AddDays(1))
        .Select(s => new { s.StartTime, EndTime = s.EndTime!.Value })
        .ToListAsync();

    var settings = await settingsProvider.GetAsync();

    var totalsByDate = sessions
        .GroupBy(s => DateOnly.FromDateTime(s.EndTime))
        .ToDictionary(
          g => g.Key,
          g => g.Sum(s => ParkingFeeCalculator.CalculateFee(
            s.StartTime,
            s.EndTime,
            settings.FirstHourFee,
            settings.SecondHourFee,
            settings.EveryNextHourFee)));

    var points = BuildDateSeries(startDate, days)
        .Select(date => new RevenuePoint(date, totalsByDate.GetValueOrDefault(date, 0m)))
        .ToList();

    return Ok(new RevenueSeriesResponse(parsedPeriod.ToString().ToLowerInvariant(), points));
  }

  [HttpGet("sessions")]
  public async Task<IActionResult> GetSessionCounts([FromQuery] string? period)
  {
    if (!TryParsePeriod(period, out var parsedPeriod))
    {
      return BadRequest("Period must be 'weekly' or 'monthly'.");
    }

    var (startDate, endDate, days) = GetDateRange(parsedPeriod);
    var sessions = await dbContext.ParkingSessions
        .Where(s => s.EndTime != null && s.EndTime >= startDate && s.EndTime < endDate.AddDays(1))
        .Select(s => s.EndTime!.Value)
        .ToListAsync();

    var totalsByDate = sessions
        .GroupBy(DateOnly.FromDateTime)
        .ToDictionary(g => g.Key, g => g.Count());

    var points = BuildDateSeries(startDate, days)
        .Select(date => new SessionCountPoint(date, totalsByDate.GetValueOrDefault(date, 0)))
        .ToList();

    return Ok(new SessionSeriesResponse(parsedPeriod.ToString().ToLowerInvariant(), points));
  }

  [HttpGet("spot-popularity")]
  public async Task<IActionResult> GetSpotPopularity()
  {
    var rawPopularity = await dbContext.ParkingSessions
      .GroupBy(s => s.SpotNumber)
      .Select(g => new { SpotNumber = g.Key, OccupancyCount = g.Count() })
      .OrderBy(item => item.SpotNumber)
      .ToListAsync();

    var popularity = rawPopularity
      .Select(item => new SpotPopularityItem(item.SpotNumber, item.OccupancyCount))
      .ToList();

    return Ok(new SpotPopularityResponse(popularity));
  }

  private static bool TryParsePeriod(string? period, out StatisticsPeriod parsed)
  {
    if (string.Equals(period, "weekly", StringComparison.OrdinalIgnoreCase))
    {
      parsed = StatisticsPeriod.Weekly;
      return true;
    }

    if (string.Equals(period, "monthly", StringComparison.OrdinalIgnoreCase))
    {
      parsed = StatisticsPeriod.Monthly;
      return true;
    }

    parsed = default;
    return false;
  }

  private static (DateTime startDate, DateTime endDate, int days) GetDateRange(StatisticsPeriod period)
  {
    var todayUtc = DateTime.UtcNow.Date;
    var days = period == StatisticsPeriod.Weekly ? 7 : 30;
    var startDate = todayUtc.AddDays(-(days - 1));
    return (startDate, todayUtc, days);
  }

  private static IEnumerable<DateOnly> BuildDateSeries(DateTime startDate, int days)
  {
    for (var i = 0; i < days; i += 1)
    {
      yield return DateOnly.FromDateTime(startDate.AddDays(i));
    }
  }

  private enum StatisticsPeriod
  {
    Weekly,
    Monthly
  }

  public record RevenuePoint(DateOnly Date, decimal TotalRevenue);
  public record RevenueSeriesResponse(string Period, IReadOnlyList<RevenuePoint> Points);
  public record SessionCountPoint(DateOnly Date, int TotalSessions);
  public record SessionSeriesResponse(string Period, IReadOnlyList<SessionCountPoint> Points);
  public record SpotPopularityResponse(IReadOnlyList<SpotPopularityItem> Spots);
}
