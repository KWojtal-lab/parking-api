
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkingApi.Models;

[ApiController]
[Route("/api/[controller]")]
public class RentalsController : ControllerBase
{
  private AppDbContext _db;
  public RentalsController(AppDbContext db)
  {
    _db = db;
  }

  [HttpGet]
  public async Task<IActionResult> GetRentals()
  {
    var rentals = await _db.Rentals.Include(r => r.Vehicle).Include(r => r.ParkingPlan).ToListAsync();
    return Ok(rentals);
  }
}