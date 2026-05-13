namespace Parking.Api.Contracts;

public record SpotPopularityItem(int SpotNumber, int OccupancyCount);

public record RevenueSummary(decimal Today, decimal Last7Days, decimal ThisMonth);

public record OperatorStatisticsResponse(
  RevenueSummary Revenue,
  IEnumerable<SpotPopularityItem> SpotPopularity,
  TimeSpan AverageParkingDuration);
