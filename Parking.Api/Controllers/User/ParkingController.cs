using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.EntityFrameworkCore;
using Parking.Api.Data;
using Parking.Api.Entities;
using Parking.Api.Services;
using System.Security.Claims;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Parking.Api.Controllers.User;

[ApiController]
[Authorize(Roles = "User")]
[Route("api/[controller]")]
public class ParkingController(
  ParkingDbContext dbContext,
  ParkingSettingsProvider settingsProvider,
  IDistributedCache cache,
  ImageProcessorClient imageProcessorClient,
  CameraImageStorageService cameraImageStorageService,
  IHttpClientFactory httpClientFactory) : ControllerBase
{
  private static readonly Regex PlateRegex = new("^[A-Z]{2,3}[0-9A-Z]{4,5}$", RegexOptions.Compiled);
  private readonly HttpClient _authClient = httpClientFactory.CreateClient("AuthApi");

  [HttpPost("entry")]
  public async Task<IActionResult> Entry([FromBody] ParkingEntryRequest request)
  {
    var userId = GetUserId();
    if (userId is null)
    {
      return Unauthorized();
    }

    if (string.IsNullOrWhiteSpace(request.PlateNumber))
    {
      return BadRequest("Numer rejestracyjny jest wymagany.");
    }

    var normalizedPlate = request.PlateNumber.Trim().ToUpperInvariant();

    if (!PlateRegex.IsMatch(normalizedPlate))
    {
      return BadRequest("Podany numer rejestracyjny ma nieprawidłowy format.");
    }

    var userPlate = await dbContext.UserLicensePlates
        .FirstOrDefaultAsync(p => p.UserId == userId && p.PlateNumber == normalizedPlate);

    if (userPlate is null)
    {
      return BadRequest("Numer rejestracyjny nie jest przypisany do tego użytkownika.");
    }

    var activeSessions = await dbContext.ParkingSessions
        .Where(s => s.EndTime == null)
        .Select(s => new { s.UserId, s.PlateNumber, s.SpotNumber })
        .ToListAsync();

    var totalSpots = await dbContext.ParkingSpots.CountAsync();
    if (activeSessions.Count >= totalSpots)
    {
      return Conflict("Parking jest pełny.");
    }

    if (activeSessions.Any(s => s.UserId == userId))
    {
      return Conflict("Użytkownik ma już aktywną sesję parkowania.");
    }

    if (activeSessions.Any(s => s.PlateNumber == normalizedPlate))
    {
      return Conflict("Pojazd ma już aktywną sesję parkowania.");
    }

    var occupiedSpots = activeSessions.Select(s => s.SpotNumber).ToHashSet();
    var allSpots = await dbContext.ParkingSpots.Select(p => p.Id).ToArrayAsync();
    var freeSpots = allSpots.Where(spot => !occupiedSpots.Contains(spot)).ToArray();

    var assignedSpot = freeSpots[Random.Shared.Next(freeSpots.Length)];
    var vehicleType = userPlate.VehicleType;

    try
    {
      await imageProcessorClient.GeneratePlateImageAsync(
        vehicleType.ToString().ToLowerInvariant(),
        normalizedPlate,
        assignedSpot,
        HttpContext.RequestAborted);
    }
    catch (Exception)
    {
      return StatusCode(StatusCodes.Status502BadGateway, "Nie udało się wygenerować obrazu z kamery dla tego wjazdu.");
    }

    var vehicle = await dbContext.Vehicles.FindAsync(normalizedPlate);
    if (vehicle is null)
    {
      vehicle = new Vehicle
      {
        PlateNumber = normalizedPlate,
        VehicleType = vehicleType
      };
      dbContext.Vehicles.Add(vehicle);
    }
    else
    {
      vehicle.VehicleType = vehicleType;
    }

    var session = new ParkingSession
    {
      Id = Guid.NewGuid(),
      UserId = userId,
      PlateNumber = normalizedPlate,
      SpotNumber = assignedSpot,
      StartTime = DateTime.UtcNow,
      EndTime = null
    };

    dbContext.ParkingSessions.Add(session);
    await dbContext.SaveChangesAsync();
    await cache.RemoveAsync(CacheKeys.OperatorStatistics);

    return Ok(new ParkingEntryResponse(
        session.Id,
        assignedSpot,
      session.StartTime));
  }

  [HttpPost("exit")]
  public async Task<IActionResult> Exit()
  {
    var userId = GetUserId();
    if (userId is null)
    {
      return Unauthorized();
    }

    var session = await dbContext.ParkingSessions
        .Include(s => s.Vehicle)
        .Where(s => s.UserId == userId && s.EndTime == null)
        .OrderByDescending(s => s.StartTime)
        .FirstOrDefaultAsync();

    if (session is null)
    {
      return NotFound("Nie znaleziono aktywnej sesji dla użytkownika.");
    }

    session.EndTime = DateTime.UtcNow;

    var settings = await settingsProvider.GetAsync();

    var totalFee = ParkingFeeCalculator.CalculateFee(
      session.StartTime,
      session.EndTime,
      settings.FirstHourFee,
      settings.SecondHourFee,
      settings.EveryNextHourFee);
    var totalDuration = session.EndTime.Value - session.StartTime;

    var paymentError = await TryChargeUserAsync(userId, totalFee, HttpContext.RequestAborted);
    if (paymentError is not null)
    {
      return paymentError;
    }

    await dbContext.SaveChangesAsync();
    await cache.RemoveAsync(CacheKeys.OperatorStatistics);
    cameraImageStorageService.DeleteImage(session.SpotNumber);

    return Ok(new ParkingExitResponse(
      totalFee,
      totalDuration,
      session.PlateNumber,
      session.Vehicle?.VehicleType));
  }

  [HttpGet("current-session")]
  public async Task<IActionResult> GetCurrentSession()
  {
    var userId = GetUserId();
    if (userId is null)
    {
      return Unauthorized();
    }

    var session = await dbContext.ParkingSessions
        .Where(s => s.UserId == userId && s.EndTime == null)
        .OrderByDescending(s => s.StartTime)
        .Select(s => new
        {
          s.Id,
          s.PlateNumber,
          s.SpotNumber,
          s.StartTime,
          VehicleType = s.Vehicle != null ? (VehicleType?)s.Vehicle.VehicleType : null,
        })
        .FirstOrDefaultAsync();

    if (session is null)
    {
      return NotFound("Nie znaleziono aktywnej sesji dla użytkownika.");
    }

    var now = DateTime.UtcNow;
    var duration = now - session.StartTime;
    var settings = await settingsProvider.GetAsync();
    var fee = ParkingFeeCalculator.CalculateFee(
      session.StartTime,
      null,
      settings.FirstHourFee,
      settings.SecondHourFee,
      settings.EveryNextHourFee);

    return Ok(new MyActiveSessionResponse(
        session.Id,
        session.PlateNumber,
        session.VehicleType,
        session.SpotNumber,
        session.StartTime,
        duration,
        fee));
  }

  [HttpGet("history")]
  public async Task<IActionResult> GetParkingHistory()
  {
    var userId = GetUserId();
    if (userId is null)
    {
      return Unauthorized();
    }

    var sessions = await dbContext.ParkingSessions
        .Where(s => s.UserId == userId && s.EndTime != null)
        .OrderByDescending(s => s.EndTime)
        .Select(s => new
        {
          s.Id,
          s.PlateNumber,
          VehicleType = s.Vehicle != null ? (VehicleType?)s.Vehicle.VehicleType : null,
          s.SpotNumber,
          s.StartTime,
          EndTime = s.EndTime!.Value
        })
        .ToListAsync();

    var settings = await settingsProvider.GetAsync();

    var response = sessions.Select(s => new MyHistoryItemResponse(
        s.Id,
        s.PlateNumber,
        s.VehicleType,
        s.SpotNumber,
        s.StartTime,
        s.EndTime,
        s.EndTime - s.StartTime,
        ParkingFeeCalculator.CalculateFee(
          s.StartTime,
          s.EndTime,
          settings.FirstHourFee,
          settings.SecondHourFee,
          settings.EveryNextHourFee)));

    return Ok(response);
  }

  private string? GetUserId()
  {
    return User.FindFirstValue(ClaimTypes.NameIdentifier);
  }

  private async Task<IActionResult?> TryChargeUserAsync(string userId, decimal amount, CancellationToken cancellationToken)
  {
    if (amount <= 0)
    {
      return null;
    }

    var request = new HttpRequestMessage(HttpMethod.Post, $"/api/users/{userId}/money/subtract")
    {
      Content = CreateJsonContent(new MoneyAdjustmentRequest(amount))
    };
    CopyAuthorizationHeader(request);

    using var response = await _authClient.SendAsync(request, cancellationToken);
    if (response.IsSuccessStatusCode)
    {
      return null;
    }

    if (response.Content is null || response.StatusCode == HttpStatusCode.NoContent)
    {
      return StatusCode((int)response.StatusCode);
    }

    var payload = await response.Content.ReadAsStringAsync(cancellationToken);
    var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";

    return new ContentResult
    {
      StatusCode = (int)response.StatusCode,
      ContentType = contentType,
      Content = payload
    };
  }

  private static StringContent CreateJsonContent<T>(T body)
  {
    var payload = JsonSerializer.Serialize(body);
    return new StringContent(payload, Encoding.UTF8, "application/json");
  }

  private void CopyAuthorizationHeader(HttpRequestMessage request)
  {
    if (Request.Headers.TryGetValue("Authorization", out var value))
    {
      request.Headers.TryAddWithoutValidation("Authorization", value.ToString());
    }
  }

  public record ParkingEntryRequest(string PlateNumber);
  public record ParkingEntryResponse(Guid SessionId, int SpotNumber, DateTime StartTime);
  public record ParkingExitResponse(decimal TotalFee, TimeSpan TotalDuration, string LicensePlate, VehicleType? VehicleType);
  public record MyActiveSessionResponse(Guid SessionId, string PlateNumber, VehicleType? VehicleType, int SpotNumber, DateTime StartTime, TimeSpan CurrentDuration, decimal CurrentFee);
  public record MyHistoryItemResponse(Guid SessionId, string PlateNumber, VehicleType? VehicleType, int SpotNumber, DateTime StartTime, DateTime EndTime, TimeSpan TotalDuration, decimal TotalFee);
  private sealed record MoneyAdjustmentRequest(decimal Amount);
}
