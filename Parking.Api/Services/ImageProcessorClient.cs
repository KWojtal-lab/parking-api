using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Parking.Api.Services;

public class ImageProcessorClient(HttpClient httpClient, ILogger<ImageProcessorClient> logger)
{
  public async Task GeneratePlateImageAsync(
    string carType,
    string licensePlate,
    int spotNumber,
    CancellationToken cancellationToken)
  {
    var payload = new RenderPlateRequest(carType, licensePlate, spotNumber);

    HttpResponseMessage response;
    try
    {
      response = await httpClient.PostAsJsonAsync("/render-plate", payload, cancellationToken);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Failed to call image processor for spot {SpotNumber}.", spotNumber);
      throw;
    }

    if (response.IsSuccessStatusCode)
    {
      return;
    }

    throw new InvalidOperationException($"Image processor returned {(int)response.StatusCode}.");
  }

  private sealed record RenderPlateRequest(
    [property: JsonPropertyName("car_type")] string CarType,
    [property: JsonPropertyName("license_plate")] string LicensePlate,
    [property: JsonPropertyName("spot_number")] int SpotNumber);
}
