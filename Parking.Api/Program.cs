using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Parking.Api.Data;
using Parking.Api.Services;
using System.Text.Json.Serialization;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
{
  options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddDbContext<ParkingDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));
builder.Services.AddStackExchangeRedisCache(options =>
{
  options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
});
builder.Services.AddScoped<ParkingSettingsProvider>();
builder.Services.AddSingleton<CameraImageStorageService>();
builder.Services.AddHttpClient<ImageProcessorClient>((serviceProvider, client) =>
{
  var configuration = serviceProvider.GetRequiredService<IConfiguration>();
  var baseUrl = configuration["ImageProcessor:BaseUrl"] ?? throw new InvalidOperationException("ImageProcessor:BaseUrl configuration is required.");
  client.BaseAddress = new Uri(baseUrl);
  client.Timeout = TimeSpan.FromSeconds(15);
});
builder.Services.AddHttpClient("AuthApi", (serviceProvider, client) =>
{
  var configuration = serviceProvider.GetRequiredService<IConfiguration>();
  var baseUrl = configuration["Auth:BaseUrl"] ?? "http://auth-api:8081";
  client.BaseAddress = new Uri(baseUrl);
  client.Timeout = TimeSpan.FromSeconds(15);
});
builder.Services.AddAuthentication(options =>
{
  options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
  options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
  var jwtSection = builder.Configuration.GetSection("Jwt");
  var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key configuration is required.");

  options.TokenValidationParameters = new TokenValidationParameters
  {
    ValidIssuer = jwtSection["Issuer"],
    ValidAudience = jwtSection["Audience"],
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
    ValidateIssuer = true,
    ValidateAudience = true,
    ValidateIssuerSigningKey = true,
    ValidateLifetime = true,
    ClockSkew = TimeSpan.FromMinutes(1)
  };
});
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
  options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
  {
    Name = "Authorization",
    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
    Scheme = "Bearer",
    BearerFormat = "JWT",
    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
    Description = "Bearer token for API requests."
  });

  options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
  {
    {
      new Microsoft.OpenApi.Models.OpenApiSecurityScheme
      {
        Reference = new Microsoft.OpenApi.Models.OpenApiReference
        {
          Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
          Id = "Bearer"
        }
      },
      Array.Empty<string>()
    }
  });
  options.TagActionsBy(api =>
  {
    var controller = api.ActionDescriptor.RouteValues["controller"] ?? "Endpoints";
    var controllerNamespace = (api.ActionDescriptor as Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor)
      ?.ControllerTypeInfo?.Namespace ?? string.Empty;

    if (controllerNamespace.Contains(".Controllers.Operator", StringComparison.Ordinal))
    {
      return new[] { $"Operator - {controller}" };
    }

    if (controllerNamespace.Contains(".Controllers.User", StringComparison.Ordinal))
    {
      return new[] { $"User - {controller}" };
    }

    return new[] { controller };
  });
});

var app = builder.Build();

await ApplyMigrationsAsync(app);

if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

var cameraImageStorage = app.Services.GetRequiredService<CameraImageStorageService>();
var cameraImagesDirectory = cameraImageStorage.EnsureDirectoryExists();

app.UseStaticFiles(new StaticFileOptions
{
  FileProvider = new PhysicalFileProvider(cameraImagesDirectory),
  RequestPath = "/camera-images"
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static async Task ApplyMigrationsAsync(WebApplication app)
{

  using var scope = app.Services.CreateScope();
  var dbContext = scope.ServiceProvider.GetRequiredService<ParkingDbContext>();
  await dbContext.Database.MigrateAsync();
}