using Microsoft.AspNetCore.Identity;
using Parking.Auth.Models;

namespace Parking.Auth.Data;

public static class DbSeeder
{
  public static async Task SeedAsync(IServiceProvider services)
  {
    using var scope = services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    var seedUsers = new List<(ApplicationUser User, string Password)>
    {
      (new ApplicationUser
      {
        Id = "11111111-1111-1111-1111-111111111111",
        UserName = "jan.kowalski@example.com",
        Email = "jan.kowalski@example.com",
        EmailConfirmed = true,
        Money = 25m
      }, "test123"),
      (new ApplicationUser
      {
        Id = "22222222-2222-2222-2222-222222222222",
        UserName = "anna.nowak@example.com",
        Email = "anna.nowak@example.com",
        EmailConfirmed = true,
        Money = 40m
      }, "test123"),
      (new ApplicationUser
      {
        Id = "33333333-3333-3333-3333-333333333333",
        UserName = "piotr.nowak@example.com",
        Email = "piotr.nowak@example.com",
        EmailConfirmed = true,
        Money = 15m
      }, "test123"),
      (new ApplicationUser
      {
        Id = "44444444-4444-4444-4444-444444444444",
        UserName = "kasia.kwiatkowska@example.com",
        Email = "kasia.kwiatkowska@example.com",
        EmailConfirmed = true,
        Money = 55m
      }, "test123"),
      (new ApplicationUser
      {
        Id = "55555555-5555-5555-5555-555555555555",
        UserName = "tomasz.lewandowski@example.com",
        Email = "tomasz.lewandowski@example.com",
        EmailConfirmed = true,
        Money = 5m
      }, "test123"),
      (new ApplicationUser
      {
        Id = "66666666-6666-6666-6666-666666666666",
        UserName = "monika.zielinska@example.com",
        Email = "monika.zielinska@example.com",
        EmailConfirmed = true,
        Money = 120m
      }, "test123"),
      (new ApplicationUser
      {
        Id = "77777777-7777-7777-7777-777777777777",
        UserName = "rafal.wojcik@example.com",
        Email = "rafal.wojcik@example.com",
        EmailConfirmed = true,
        Money = 32m
      }, "test123"),
      (new ApplicationUser
      {
        Id = "88888888-8888-8888-8888-888888888888",
        UserName = "dorota.kaminska@example.com",
        Email = "dorota.kaminska@example.com",
        EmailConfirmed = true,
        Money = 78m
      }, "test123")
    };

    foreach (var (user, password) in seedUsers)
    {
      var existing = await userManager.FindByEmailAsync(user.Email!);
      if (existing is not null)
      {
        continue;
      }

      var result = await userManager.CreateAsync(user, password);
      if (!result.Succeeded)
      {
        continue;
      }

      await userManager.AddToRoleAsync(user, "User");
    }
  }
}
