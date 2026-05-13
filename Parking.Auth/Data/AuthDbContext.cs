using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Parking.Auth.Models;

namespace Parking.Auth.Data;

public class AuthDbContext(DbContextOptions<AuthDbContext> options)
  : IdentityDbContext<ApplicationUser>(options)
{
}
