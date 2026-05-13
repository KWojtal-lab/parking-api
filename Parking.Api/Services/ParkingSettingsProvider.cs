using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Parking.Api.Data;
using Parking.Api.Entities;
using System.Text.Json;

namespace Parking.Api.Services;

public class ParkingSettingsProvider(ParkingDbContext dbContext, IDistributedCache cache)
{
  public async Task<ParkingSettings> GetAsync(CancellationToken cancellationToken = default)
  {
    var cached = await cache.GetStringAsync(CacheKeys.ParkingSettings, cancellationToken);
    if (!string.IsNullOrWhiteSpace(cached))
    {
      var cachedSettings = JsonSerializer.Deserialize<ParkingSettings>(cached);
      if (cachedSettings is not null)
      {
        return cachedSettings;
      }
    }

    var settings = await dbContext.ParkingSettings
        .AsNoTracking()
        .SingleOrDefaultAsync(s => s.Id == 1, cancellationToken);

    if (settings is not null)
    {
      await cache.SetStringAsync(
          CacheKeys.ParkingSettings,
          JsonSerializer.Serialize(settings),
          new DistributedCacheEntryOptions
          {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
          },
          cancellationToken);

      return settings;
    }

    throw new InvalidOperationException("Parking settings row with Id=1 is missing.");
  }
}
