using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Parking.Auth.Data;
using Parking.Auth.Models;
using Parking.Auth.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
  {
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 1;
    options.Password.RequiredUniqueChars = 0;
  })
    .AddEntityFrameworkStores<AuthDbContext>()
    .AddErrorDescriber<PolishIdentityErrorDescriber>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication()
    .AddJwtBearer(options =>
    {
      var jwtSection = builder.Configuration.GetSection("Jwt");
      options.TokenValidationParameters = new TokenValidationParameters
      {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
              Encoding.UTF8.GetBytes(jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key is required"))),
        ValidateIssuer = true,
        ValidIssuer = jwtSection["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSection["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
      };
    });

builder.Services.AddCors(options =>
{
  options.AddDefaultPolicy(policy =>
    policy.AllowAnyOrigin()
      .AllowAnyHeader()
      .AllowAnyMethod());
});

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

await ApplyMigrationsAsync(app);
await EnsureRolesAsync(app);
await EnsureOperatorUserAsync(app);
await DbSeeder.SeedAsync(app.Services);

if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

var authGroup = app.MapGroup("/api/auth");

authGroup.MapPost("register", async (
    RegisterRequest request,
    UserManager<ApplicationUser> userManager) =>
{
  if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
  {
    return Results.BadRequest("Email i hasło są wymagane.");
  }

  if (!IsValidEmail(request.Email))
  {
    return Results.BadRequest("Nieprawidłowy adres email.");
  }

  var user = new ApplicationUser
  {
    UserName = request.Email,
    Email = request.Email
  };

  var result = await userManager.CreateAsync(user, request.Password);
  if (!result.Succeeded)
  {
    return Results.BadRequest("Ten adres e-mail jest już zajęty.");
  }

  await userManager.AddToRoleAsync(user, "User");

  return Results.Created("/api/auth/register", new RegisterResponse(user.Id, user.Email ?? string.Empty));
});

authGroup.MapPost("login", async (
    LoginRequest request,
    UserManager<ApplicationUser> userManager,
    IConfiguration configuration) =>
{
  var user = await userManager.FindByEmailAsync(request.Email);
  if (user is null)
  {
    return Results.Unauthorized();
  }

  var validPassword = await userManager.CheckPasswordAsync(user, request.Password);
  if (!validPassword)
  {
    return Results.Unauthorized();
  }

  var jwtSection = configuration.GetSection("Jwt");
  var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key configuration is required.");
  var issuer = jwtSection["Issuer"] ?? "Parking.Auth";
  var audience = jwtSection["Audience"] ?? "Parking.Api";
  var expiresMinutes = int.TryParse(jwtSection["TokenLifetimeMinutes"], out var parsed)
      ? parsed
      : 60;

  var roles = await userManager.GetRolesAsync(user);

  var claims = new List<Claim>
    {
        new(JwtRegisteredClaimNames.Sub, user.Id),
        new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
        new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new(ClaimTypes.NameIdentifier, user.Id),
        new(ClaimTypes.Name, user.UserName ?? string.Empty)
    };

  claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

  var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
  var token = new JwtSecurityToken(
      issuer: issuer,
      audience: audience,
      claims: claims,
      expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
      signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));

  var tokenValue = new JwtSecurityTokenHandler().WriteToken(token);
  return Results.Ok(new AuthResponse(tokenValue, token.ValidTo, roles));
});

authGroup.MapPost("change-password", async (
    ChangePasswordRequest request,
    UserManager<ApplicationUser> userManager,
    ClaimsPrincipal principal) =>
{
  var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
  if (userId is null)
  {
    return Results.Unauthorized();
  }

  if (string.IsNullOrWhiteSpace(request.NewPassword))
  {
    return Results.BadRequest(new { message = "Nowe hasło jest wymagane." });
  }

  var user = await userManager.FindByIdAsync(userId);
  if (user is null)
  {
    return Results.NotFound();
  }

  var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
  if (!result.Succeeded)
  {
    var message = string.Join(", ", result.Errors.Select(e => e.Description));
    return Results.BadRequest(new { message });
  }

  return Results.NoContent();
})
.RequireAuthorization(policy =>
    policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
          .RequireAuthenticatedUser());

var usersGroup = app.MapGroup("/api/users");

usersGroup.MapGet("{userId}/money", async (
    string userId,
    UserManager<ApplicationUser> userManager) =>
{
  var user = await userManager.FindByIdAsync(userId);
  if (user is null)
  {
    return Results.NotFound();
  }

  return Results.Ok(new MoneyResponse(user.Id, user.Money));
});

usersGroup.MapGet("{userId}/profile", async (
    string userId,
    UserManager<ApplicationUser> userManager) =>
{
  var user = await userManager.FindByIdAsync(userId);
  if (user is null)
  {
    return Results.NotFound();
  }

  return Results.Ok(new UserProfileResponse(user.Id, user.Email ?? string.Empty));
});

usersGroup.MapPost("{userId}/money/add", async (
    string userId,
    MoneyAdjustmentRequest request,
    UserManager<ApplicationUser> userManager) =>
{
  if (request.Amount <= 0)
  {
    return Results.BadRequest("Kwota musi być większa od zera.");
  }

  var user = await userManager.FindByIdAsync(userId);
  if (user is null)
  {
    return Results.NotFound();
  }

  user.Money += request.Amount;
  var result = await userManager.UpdateAsync(user);
  if (!result.Succeeded)
  {
    return Results.BadRequest(result.Errors.Select(error => error.Description));
  }

  return Results.Ok(new MoneyResponse(user.Id, user.Money));
});

usersGroup.MapPost("{userId}/money/subtract", async (
    string userId,
    MoneyAdjustmentRequest request,
    UserManager<ApplicationUser> userManager) =>
{
  if (request.Amount <= 0)
  {
    return Results.BadRequest("Kwota musi być większa od zera.");
  }

  var user = await userManager.FindByIdAsync(userId);
  if (user is null)
  {
    return Results.NotFound();
  }

  if (user.Money < request.Amount)
  {
    return Results.BadRequest("Niewystarczające środki.");
  }

  user.Money -= request.Amount;
  var result = await userManager.UpdateAsync(user);
  if (!result.Succeeded)
  {
    return Results.BadRequest(result.Errors.Select(error => error.Description));
  }

  return Results.Ok(new MoneyResponse(user.Id, user.Money));
});

app.MapGet("/health", () => Results.Ok("OK"));

app.Run();

static async Task ApplyMigrationsAsync(WebApplication app)
{
  const int maxRetries = 10;
  const int delayMs    = 3000;

  using var scope = app.Services.CreateScope();
  var dbContext   = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
  var logger      = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

  for (int attempt = 1; attempt <= maxRetries; attempt++)
  {
    try
    {
      await dbContext.Database.MigrateAsync();
      return;
    }
    catch (Exception ex) when (attempt < maxRetries)
    {
      logger.LogWarning("Migration attempt {Attempt}/{Max} failed: {Message}. Retrying in {Delay}ms…",
          attempt, maxRetries, ex.Message, delayMs);
      await Task.Delay(delayMs);
    }
  }

  await dbContext.Database.MigrateAsync(); // ostatnia próba — rzuci wyjątek jeśli nadal nie działa
}

static async Task EnsureRolesAsync(WebApplication app)
{
  using var scope = app.Services.CreateScope();
  var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

  foreach (var role in new[] { "User", "Operator" })
  {
    if (!await roleManager.RoleExistsAsync(role))
    {
      await roleManager.CreateAsync(new IdentityRole(role));
    }
  }
}

static async Task EnsureOperatorUserAsync(WebApplication app)
{
  using var scope = app.Services.CreateScope();
  var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

  const string email = "admin@admin.pl";
  var existingUser = await userManager.FindByEmailAsync(email);
  if (existingUser is not null)
  {
    if (!await userManager.IsInRoleAsync(existingUser, "Operator"))
    {
      await userManager.AddToRoleAsync(existingUser, "Operator");
    }

    if (!await userManager.CheckPasswordAsync(existingUser, "admin"))
    {
      var resetToken = await userManager.GeneratePasswordResetTokenAsync(existingUser);
      await userManager.ResetPasswordAsync(existingUser, resetToken, "admin");
    }
    return;
  }

  var user = new ApplicationUser
  {
    UserName = email,
    Email = email,
    EmailConfirmed = true
  };

  var result = await userManager.CreateAsync(user, "admin");
  if (result.Succeeded)
  {
    await userManager.AddToRoleAsync(user, "Operator");
  }
}

static bool IsValidEmail(string email)
{
  return Regex.IsMatch(email, "^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
}

public record RegisterRequest(string Email, string Password);
public record RegisterResponse(string UserId, string Email);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string AccessToken, DateTime ExpiresAtUtc, IEnumerable<string> Roles);
public record MoneyAdjustmentRequest(decimal Amount);
public record MoneyResponse(string UserId, decimal Money);
public record UserProfileResponse(string UserId, string Email);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
