using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.EntityFrameworkCore;
using Parking.Api.Contracts;
using Parking.Api.Data;
using Parking.Api.Services;

namespace Parking.Api.Controllers.Operator;

[ApiController]
[Authorize(Roles = "Operator")]
[Route("api/operator/[controller]")]
public class ParkingController(
  ParkingDbContext dbContext,
  ParkingSettingsProvider settingsProvider,
  IDistributedCache cache,
  CameraImageStorageService cameraImageStorageService) : ControllerBase
{
  [HttpGet("state")]
  public async Task<IActionResult> GetState()
  {
    var takenSpots = await dbContext.ParkingSessions
        .Where(s => s.EndTime == null)
        .OrderBy(s => s.SpotNumber)
      .Select(s => s.SpotNumber)
        .ToListAsync();

    return Ok(new StateResponse(takenSpots));
  }

  [HttpPatch("mark-as-free/{sessionId:guid}")]
  public async Task<IActionResult> MarkAsFree(Guid sessionId)
  {
    var session = await dbContext.ParkingSessions
        .FirstOrDefaultAsync(s => s.Id == sessionId && s.EndTime == null);

    if (session is null)
    {
      return NotFound("Active session not found.");
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
      return BadRequest("Spot number is invalid.");
    }

    var activeSession = await dbContext.ParkingSessions
        .Where(s => s.SpotNumber == spotNumber && s.EndTime == null)
        .OrderByDescending(s => s.StartTime)
        .Select(s => new { s.PlateNumber })
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
        imageUrl,
        activeSession is not null && imageUrl is not null));
  }

  [HttpGet("history/spot/{spotNumber:int}")]
  public async Task<IActionResult> GetSpotHistory(int spotNumber)
  {
    var exists = await dbContext.ParkingSpots.AnyAsync(p => p.Id == spotNumber);
    if (!exists)
    {
      return BadRequest("Spot number is invalid.");
    }

    var sessions = await dbContext.ParkingSessions
        .Where(s => s.SpotNumber == spotNumber && s.EndTime != null)
        .OrderByDescending(s => s.EndTime)
        .Select(s => new HistorySessionData(
          s.Id,
          s.UserId,
          s.PlateNumber,
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

  public record StateResponse(IEnumerable<int> TakenSpots);
  public record OperatorAlertItem(Guid SessionId, string UserId, string PlateNumber, int SpotNumber, DateTime StartTime, TimeSpan CurrentDuration, decimal CurrentFee);
  public record TerminateSessionResponse(Guid SessionId, string PlateNumber, int SpotNumber, DateTime StartTime, DateTime EndTime, decimal TotalFee);
  public record SpotCameraResponse(int SpotNumber, string? PlateNumber, string? CameraImageUrl, bool IsAvailable);
  public record HistoryItemResponse(Guid SessionId, string UserId, string PlateNumber, int SpotNumber, DateTime StartTime, DateTime EndTime, TimeSpan TotalDuration, decimal TotalFee);
  private record HistorySessionData(Guid Id, string UserId, string PlateNumber, int SpotNumber, DateTime StartTime, DateTime EndTime);
}
