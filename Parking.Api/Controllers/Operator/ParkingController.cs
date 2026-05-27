using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Parking.Api.Contracts;
using Parking.Api.Data;
using Parking.Api.Entities;
using Parking.Api.Services;

namespace Parking.Api.Controllers.Operator;

[ApiController]
[Authorize(Roles = "Operator")]
[Route("api/operator/[controller]")]
public class ParkingController(
  ParkingDbContext dbContext,
  ParkingSettingsProvider settingsProvider,
  IDistributedCache cache,
  CameraImageStorageService cameraImageStorageService,
  IHttpClientFactory httpClientFactory) : ControllerBase
{
  [HttpGet("{spotNumber:int}")]
  public async Task<IActionResult> GetSpotSession(int spotNumber)
  {
    var exists = await dbContext.ParkingSpots.AnyAsync(p => p.Id == spotNumber);
    if (!exists)
    {
      return BadRequest("Nieprawidłowy numer miejsca.");
    }

    var session = await dbContext.ParkingSessions
        .Where(s => s.SpotNumber == spotNumber && s.EndTime == null)
        .OrderByDescending(s => s.StartTime)
        .Select(s => new
        {
          s.Id,
          s.UserId,
          s.PlateNumber,
          s.SpotNumber,
          s.StartTime,
          VehicleType = s.Vehicle != null ? (VehicleType?)s.Vehicle.VehicleType : null,
        })
        .FirstOrDefaultAsync();

    if (session is null)
    {
      return NotFound("Brak aktywnej sesji dla tego miejsca.");
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

    var driverEmail = await GetUserEmailAsync(session.UserId);

    return Ok(new SpotSessionResponse(
        session.Id,
        session.UserId,
        session.PlateNumber,
        session.VehicleType,
        session.SpotNumber,
        driverEmail,
        session.StartTime,
        duration,
        fee));
  }

  [HttpPatch("mark-as-free/{spotNumber:int}")]
  public async Task<IActionResult> MarkAsFree(int spotNumber)
  {
    var session = await dbContext.ParkingSessions
        .Include(s => s.Vehicle)
        .FirstOrDefaultAsync(s => s.SpotNumber == spotNumber && s.EndTime == null);

    if (session is null)
    {
      return NotFound("Nie znaleziono aktywnej sesji.");
    }

    session.EndTime = DateTime.UtcNow;
    await dbContext.SaveChangesAsync();
    await cache.RemoveAsync(CacheKeys.OperatorStatistics);
    cameraImageStorageService.DeleteImage(session.SpotNumber);

    var settings = await settingsProvider.GetAsync();

    var totalFee = ParkingFeeCalculator.CalculateFee(
      session.StartTime,
      session.EndTime,
      settings.FirstHourFee,
      settings.SecondHourFee,
      settings.EveryNextHourFee);

    return Ok(new TerminateSessionResponse(
        session.Id,
        session.PlateNumber,
        session.Vehicle?.VehicleType,
        session.SpotNumber,
        session.StartTime,
        session.EndTime.Value,
        totalFee));
  }

  [HttpGet("camera/spot/{spotNumber:int}")]
  public async Task<IActionResult> GetSpotCamera(int spotNumber)
  {
    var exists = await dbContext.ParkingSpots.AnyAsync(p => p.Id == spotNumber);
    if (!exists)
    {
      return BadRequest("Nieprawidłowy numer miejsca.");
    }

    var activeSession = await dbContext.ParkingSessions
        .Where(s => s.SpotNumber == spotNumber && s.EndTime == null)
        .OrderByDescending(s => s.StartTime)
        .Select(s => new { s.PlateNumber, VehicleType = s.Vehicle != null ? (VehicleType?)s.Vehicle.VehicleType : null })
        .FirstOrDefaultAsync();

    string? imageUrl = null;
    if (activeSession is not null && cameraImageStorageService.ImageExists(spotNumber))
    {
      var publicPath = cameraImageStorageService.GetPublicUrl(spotNumber);
      imageUrl = $"{Request.Scheme}://{Request.Host}{publicPath}";
    }

    return Ok(new SpotCameraResponse(
        spotNumber,
        activeSession?.PlateNumber,
        activeSession?.VehicleType,
        imageUrl,
        activeSession is not null && imageUrl is not null));
  }

  [HttpGet("history/spot/{spotNumber:int}")]
  public async Task<IActionResult> GetSpotHistory(int spotNumber)
  {
    var exists = await dbContext.ParkingSpots.AnyAsync(p => p.Id == spotNumber);
    if (!exists)
    {
      return BadRequest("Nieprawidłowy numer miejsca.");
    }

    var sessions = await dbContext.ParkingSessions
        .Where(s => s.SpotNumber == spotNumber && s.EndTime != null)
        .OrderByDescending(s => s.EndTime)
        .Select(s => new HistorySessionData(
          s.Id,
          s.UserId,
          s.PlateNumber,
          s.Vehicle != null ? (VehicleType?)s.Vehicle.VehicleType : null,
          s.SpotNumber,
          s.StartTime,
          s.EndTime!.Value))
        .ToListAsync();

    var response = await BuildHistoryResponseAsync(sessions);
    return Ok(response);
  }

  [HttpGet("history")]
  public async Task<IActionResult> GetHistory()
  {
    var sessions = await dbContext.ParkingSessions
        .Where(s => s.EndTime != null)
        .OrderByDescending(s => s.EndTime)
        .Select(s => new HistorySessionData(
          s.Id,
          s.UserId,
          s.PlateNumber,
          s.Vehicle != null ? (VehicleType?)s.Vehicle.VehicleType : null,
          s.SpotNumber,
          s.StartTime,
          s.EndTime!.Value))
        .ToListAsync();

    var response = await BuildHistoryResponseAsync(sessions);
    return Ok(response);
  }

  private async Task<IEnumerable<HistoryItemResponse>> BuildHistoryResponseAsync(
    IReadOnlyList<HistorySessionData> sessions)
  {
    var settings = await settingsProvider.GetAsync();

    return sessions.Select(s => new HistoryItemResponse(
        s.Id,
        s.UserId,
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
  }

  private async Task<string?> GetUserEmailAsync(string userId)
  {
    try
    {
      var client = httpClientFactory.CreateClient("AuthApi");
      var response = await client.GetAsync($"/api/users/{userId}/profile");
      if (!response.IsSuccessStatusCode)
      {
        return null;
      }

      var payload = await response.Content.ReadFromJsonAsync<UserProfileResponse>();
      return payload?.Email;
    }
    catch
    {
      return null;
    }
  }

  public record OperatorAlertItem(Guid SessionId, string UserId, string PlateNumber, int SpotNumber, DateTime StartTime, TimeSpan CurrentDuration, decimal CurrentFee);
  public record SpotSessionResponse(Guid SessionId, string UserId, string PlateNumber, VehicleType? VehicleType, int SpotNumber, string? DriverEmail, DateTime StartTime, TimeSpan CurrentDuration, decimal CurrentFee);
  public record TerminateSessionResponse(Guid SessionId, string PlateNumber, VehicleType? VehicleType, int SpotNumber, DateTime StartTime, DateTime EndTime, decimal TotalFee);
  public record SpotCameraResponse(int SpotNumber, string? PlateNumber, VehicleType? VehicleType, string? CameraImageUrl, bool IsAvailable);
  public record HistoryItemResponse(Guid SessionId, string UserId, string PlateNumber, VehicleType? VehicleType, int SpotNumber, DateTime StartTime, DateTime EndTime, TimeSpan TotalDuration, decimal TotalFee);
  private record HistorySessionData(Guid Id, string UserId, string PlateNumber, VehicleType? VehicleType, int SpotNumber, DateTime StartTime, DateTime EndTime);
  private record UserProfileResponse(string UserId, string Email);
}
