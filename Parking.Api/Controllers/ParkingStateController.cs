using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Parking.Api.Data;

namespace Parking.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/parking")]
public class ParkingStateController(ParkingDbContext dbContext) : ControllerBase
{
  [HttpGet("state")]
  public async Task<IActionResult> GetState()
  {
    var takenSpots = await dbContext.ParkingSessions
        .Where(s => s.EndTime == null)
        .OrderBy(s => s.SpotNumber)
        .Select(s => s.SpotNumber)
        .ToListAsync();

    return Ok(new ParkingStateResponse(takenSpots));
  }

  public record ParkingStateResponse(IEnumerable<int> TakenSpots);
}
