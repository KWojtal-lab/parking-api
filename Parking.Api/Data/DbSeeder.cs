using Microsoft.EntityFrameworkCore;
using Parking.Api.Entities;
using Parking.Api.Services;

namespace Parking.Api.Data;

public static class DbSeeder
{
  public static async Task SeedAsync(IServiceProvider services)
  {
    using var scope = services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ParkingDbContext>();
    var imageProcessorClient = scope.ServiceProvider.GetRequiredService<ImageProcessorClient>();

    var hasData = await dbContext.ParkingSessions.AnyAsync()
        || await dbContext.Vehicles.AnyAsync()
        || await dbContext.UserLicensePlates.AnyAsync();

    if (hasData)
    {
      return;
    }

    var userIds = new[]
    {
      "11111111-1111-1111-1111-111111111111",
      "22222222-2222-2222-2222-222222222222",
      "33333333-3333-3333-3333-333333333333",
      "44444444-4444-4444-4444-444444444444",
      "55555555-5555-5555-5555-555555555555",
      "66666666-6666-6666-6666-666666666666",
      "77777777-7777-7777-7777-777777777777",
      "88888888-8888-8888-8888-888888888888"
    };

    var platePool = new[]
    {
      "KR1234A", "PO8765B", "GD4321C", "LU7654D", "WA3456E", "WRO5678",
      "KI1122A", "EL3344B", "PO9090C", "GD5566D", "KR7788E", "LU9900F",
      "WE1010G", "GD2020H", "WA3030I", "PO4040J", "KR5050K", "LU6060L",
      "WI7070M", "GD8080N", "WA9090O", "PO1111P", "KR2222Q", "LU3333R"
    };

    var vehicleTypes = Enum.GetValues<VehicleType>();
    var vehicles = platePool
      .Select((plate, index) => new Vehicle
      {
        PlateNumber = plate,
        VehicleType = vehicleTypes[index % vehicleTypes.Length]
      })
      .ToList();

    var licensePlates = new List<UserLicensePlate>();
    for (var i = 0; i < userIds.Length; i++)
    {
      var userId = userIds[i];
      var userPlates = platePool.Skip(i * 3).Take(3).ToArray();
      foreach (var plate in userPlates)
      {
        var vehicleType = vehicles.First(v => v.PlateNumber == plate).VehicleType;
        licensePlates.Add(new UserLicensePlate
        {
          UserId = userId,
          PlateNumber = plate,
          VehicleType = vehicleType
        });
      }
    }

    var now = DateTime.UtcNow;
    var random = new Random(42);
    var sessions = new List<ParkingSession>();

    var spotPool = Enumerable.Range(1, 50).OrderBy(_ => random.Next()).ToArray();
    var activeSpots = spotPool.Take(10).ToArray();
    var plateSpotMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    for (var i = 0; i < activeSpots.Length; i++)
    {
      var plate = platePool[i % platePool.Length];
      plateSpotMap[plate] = activeSpots[i];
      sessions.Add(new ParkingSession
      {
        Id = Guid.NewGuid(),
        UserId = userIds[i % userIds.Length],
        PlateNumber = plate,
        SpotNumber = activeSpots[i],
        StartTime = now.AddMinutes(-random.Next(15, 180))
      });
    }

    for (var i = 0; i < 990; i++)
    {
      var plate = platePool[(i + 10) % platePool.Length];
      var spotNumber = random.Next(1, 51);
      var startTime = now.AddDays(-random.Next(1, 30)).AddHours(-random.Next(1, 12));
      var endTime = startTime.AddMinutes(random.Next(30, 360));

      sessions.Add(new ParkingSession
      {
        Id = Guid.NewGuid(),
        UserId = userIds[(i + 3) % userIds.Length],
        PlateNumber = plate,
        SpotNumber = spotNumber,
        StartTime = startTime,
        EndTime = endTime
      });
    }

    var remainingSpots = new Queue<int>(spotPool.Except(activeSpots));
    foreach (var vehicle in vehicles)
    {
      if (!plateSpotMap.TryGetValue(vehicle.PlateNumber, out var spotNumber))
      {
        spotNumber = remainingSpots.Count > 0 ? remainingSpots.Dequeue() : 1;
        plateSpotMap[vehicle.PlateNumber] = spotNumber;
      }

      await imageProcessorClient.GeneratePlateImageAsync(
        vehicle.VehicleType.ToString().ToLowerInvariant(),
        vehicle.PlateNumber,
        spotNumber,
        CancellationToken.None);
    }

    dbContext.Vehicles.AddRange(vehicles);
    dbContext.UserLicensePlates.AddRange(licensePlates);
    dbContext.ParkingSessions.AddRange(sessions);

    await dbContext.SaveChangesAsync();
  }
}
