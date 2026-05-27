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
      .Select(p => new LicensePlateResponse(p.PlateNumber, p.VehicleType))
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
      return BadRequest("Numer rejestracyjny jest wymagany.");
    }

    if (!PlateRegex.IsMatch(normalizedPlate))
    {
      return BadRequest("Podany numer rejestracyjny ma nieprawidłowy format.");
    }

    var exists = await dbContext.UserLicensePlates
      .AnyAsync(p => p.PlateNumber == normalizedPlate);

    if (exists)
    {
      return Conflict("Taki numer rejestracyjny już istnieje.");
    }

    dbContext.UserLicensePlates.Add(new UserLicensePlate
    {
      UserId = userId,
      PlateNumber = normalizedPlate,
      VehicleType = request.VehicleType
    });

    await dbContext.SaveChangesAsync();

    return CreatedAtAction(nameof(GetAll), new { }, new LicensePlateResponse(normalizedPlate, request.VehicleType));
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
      return BadRequest("Numer rejestracyjny jest wymagany.");
    }

    var normalizedNewPlate = NormalizePlate(request.NewPlateNumber);
    if (normalizedNewPlate is null)
    {
      return BadRequest("Nowy numer rejestracyjny jest wymagany.");
    }

    if (!PlateRegex.IsMatch(normalizedNewPlate))
    {
      return BadRequest("Podany numer rejestracyjny ma nieprawidłowy format.");
    }

    var existing = await dbContext.UserLicensePlates
        .FirstOrDefaultAsync(p => p.UserId == userId && p.PlateNumber == normalizedOldPlate);

    if (existing is null)
    {
      return NotFound("Nie znaleziono numeru rejestracyjnego.");
    }

    if (normalizedOldPlate == normalizedNewPlate)
    {
      if (existing.VehicleType != request.VehicleType)
      {
        existing.VehicleType = request.VehicleType;
        await dbContext.SaveChangesAsync();
      }

      return Ok(new LicensePlateResponse(existing.PlateNumber, existing.VehicleType));
    }

    var duplicate = await dbContext.UserLicensePlates
      .AnyAsync(p => p.PlateNumber == normalizedNewPlate);

    if (duplicate)
    {
      return Conflict("Taki numer rejestracyjny już istnieje.");
    }

    dbContext.UserLicensePlates.Remove(existing);
    dbContext.UserLicensePlates.Add(new UserLicensePlate
    {
      UserId = userId,
      PlateNumber = normalizedNewPlate,
      VehicleType = request.VehicleType
    });

    await dbContext.SaveChangesAsync();

    return Ok(new LicensePlateResponse(normalizedNewPlate, request.VehicleType));
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
      return BadRequest("Numer rejestracyjny jest wymagany.");
    }

    var existing = await dbContext.UserLicensePlates
        .FirstOrDefaultAsync(p => p.UserId == userId && p.PlateNumber == normalizedPlate);

    if (existing is null)
    {
      return NotFound("Nie znaleziono numeru rejestracyjnego.");
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

  public record AddLicensePlateRequest(string PlateNumber, VehicleType VehicleType);
  public record UpdateLicensePlateRequest(string NewPlateNumber, VehicleType VehicleType);
  public record LicensePlateResponse(string PlateNumber, VehicleType VehicleType);
  public record LicensePlateListResponse(IEnumerable<LicensePlateResponse> Plates);
}
