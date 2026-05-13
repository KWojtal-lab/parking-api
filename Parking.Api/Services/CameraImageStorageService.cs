namespace Parking.Api.Services;

public class CameraImageStorageService(IConfiguration configuration, IWebHostEnvironment environment)
{
  private readonly string _publicBasePath =
    configuration["CameraImages:PublicBasePath"]?.TrimEnd('/') ?? "/camera-images";

  public string GetPhysicalDirectoryPath()
  {
    var configuredPath = configuration["CameraImages:DirectoryPath"];
    if (!string.IsNullOrWhiteSpace(configuredPath))
    {
      return Path.GetFullPath(configuredPath, environment.ContentRootPath);
    }

    return Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "images", "real"));
  }

  public string EnsureDirectoryExists()
  {
    var path = GetPhysicalDirectoryPath();
    Directory.CreateDirectory(path);
    return path;
  }

  public string GetFileName(int spotNumber) => $"spot_{spotNumber}.png";

  public string GetPhysicalPath(int spotNumber) => Path.Combine(GetPhysicalDirectoryPath(), GetFileName(spotNumber));

  public bool DeleteImage(int spotNumber)
  {
    var path = GetPhysicalPath(spotNumber);
    if (!File.Exists(path))
    {
      return false;
    }

    File.Delete(path);
    return true;
  }

  public bool ImageExists(int spotNumber) => File.Exists(GetPhysicalPath(spotNumber));

  public string GetPublicUrl(int spotNumber) => $"{_publicBasePath}/{GetFileName(spotNumber)}";
}
