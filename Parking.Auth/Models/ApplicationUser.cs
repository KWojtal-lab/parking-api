using Microsoft.AspNetCore.Identity;

namespace Parking.Auth.Models;

public class ApplicationUser : IdentityUser
{
  public decimal Money { get; set; }
}
