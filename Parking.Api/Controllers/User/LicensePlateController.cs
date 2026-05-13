using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Parking.Api.Data;
using Parking.Api.Entities;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace Parking.Api.Controllers.User;

[ApiController]
[Authorize(Roles = "User")]
[Route("api/license-plates")]
public class LicensePlateController(ParkingDbContext dbContext) : ControllerBase
{
  private static readonly Regex PlateRegex = new("^[A-Z]{2,3}[0-9A-Z]{4,5}$", RegexOptions.Compiled);

  [HttpGet]
  public async Task<IActionResult> GetAll()
  {
    var userId = GetUserId();
    if (userId is null)
    {
      return Unauthorized();
    }

    var plates = await dbContext.UserLicensePlates
        .Where(p => p.UserId == userId)
        .OrderBy(p => p.PlateNumber)
        .Select(p => p.PlateNumber)
        .ToListAsync();

    return Ok(new LicensePlateListResponse(plates));
  }

  [HttpPost]
  public async Task<IActionResult> Add([FromBody] AddLicensePlateRequest request)
  {
    var userId = GetUserId();
    if (userId is null)
    {
      return Unauthorized();
    }

    var normalizedPlate = NormalizePlate(request.PlateNumber);
    if (normalizedPlate is null)
    {
      return BadRequest("PlateNumber is required.");
    }

    if (!PlateRegex.IsMatch(normalizedPlate))
    {
      return BadRequest("Provided plate number does not match expected format.");
    }

    var exists = await dbContext.UserLicensePlates
      .AnyAsync(p => p.PlateNumber == normalizedPlate);

    if (exists)
    {
      return Conflict("License plate already exists.");
    }

    dbContext.UserLicensePlates.Add(new UserLicensePlate
    {
      UserId = userId,
      PlateNumber = normalizedPlate
    });

    await dbContext.SaveChangesAsync();

    return CreatedAtAction(nameof(GetAll), new { }, new LicensePlateResponse(normalizedPlate));
  }

  [HttpPut("{plateNumber}")]
  public async Task<IActionResult> Update(string plateNumber, [FromBody] UpdateLicensePlateRequest request)
  {
    var userId = GetUserId();
    if (userId is null)
    {
      return Unauthorized();
    }

    var normalizedOldPlate = NormalizePlate(plateNumber);
    if (normalizedOldPlate is null)
    {
      return BadRequest("Plate number is required.");
    }

    var normalizedNewPlate = NormalizePlate(request.NewPlateNumber);
    if (normalizedNewPlate is null)
    {
      return BadRequest("NewPlateNumber is required.");
    }

    if (!PlateRegex.IsMatch(normalizedNewPlate))
    {
      return BadRequest("Provided plate number does not match expected format.");
    }

    var existing = await dbContext.UserLicensePlates
        .FirstOrDefaultAsync(p => p.UserId == userId && p.PlateNumber == normalizedOldPlate);

    if (existing is null)
    {
      return NotFound("License plate not found.");
    }

    if (normalizedOldPlate == normalizedNewPlate)
    {
      return Ok(new LicensePlateResponse(existing.PlateNumber));
    }

    var duplicate = await dbContext.UserLicensePlates
      .AnyAsync(p => p.PlateNumber == normalizedNewPlate);

    if (duplicate)
    {
      return Conflict("License plate already exists.");
    }

    dbContext.UserLicensePlates.Remove(existing);
    dbContext.UserLicensePlates.Add(new UserLicensePlate
    {
      UserId = userId,
      PlateNumber = normalizedNewPlate
    });

    await dbContext.SaveChangesAsync();

    return Ok(new LicensePlateResponse(normalizedNewPlate));
  }

  [HttpDelete("{plateNumber}")]
  public async Task<IActionResult> Delete(string plateNumber)
  {
    var userId = GetUserId();
    if (userId is null)
    {
      return Unauthorized();
    }

    var normalizedPlate = NormalizePlate(plateNumber);
    if (normalizedPlate is null)
    {
      return BadRequest("Plate number is required.");
    }

    var existing = await dbContext.UserLicensePlates
        .FirstOrDefaultAsync(p => p.UserId == userId && p.PlateNumber == normalizedPlate);

    if (existing is null)
    {
      return NotFound("License plate not found.");
    }

    dbContext.UserLicensePlates.Remove(existing);
    await dbContext.SaveChangesAsync();

    return NoContent();
  }

  private static string? NormalizePlate(string? plateNumber)
  {
    if (string.IsNullOrWhiteSpace(plateNumber))
    {
      return null;
    }

    return plateNumber.Trim().ToUpperInvariant();
  }

  private string? GetUserId()
  {
    return User.FindFirstValue(ClaimTypes.NameIdentifier);
  }

  public record AddLicensePlateRequest(string PlateNumber);
  public record UpdateLicensePlateRequest(string NewPlateNumber);
  public record LicensePlateResponse(string PlateNumber);
  public record LicensePlateListResponse(IEnumerable<string> Plates);
}
