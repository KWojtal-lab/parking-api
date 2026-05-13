using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Parking.Api.Controllers.User;

[ApiController]
[Authorize(Roles = "User")]
[Route("api/users/money")]
public sealed class MoneyController(IHttpClientFactory httpClientFactory) : ControllerBase
{
  private readonly HttpClient _httpClient = httpClientFactory.CreateClient("AuthApi");

  [HttpGet]
  public async Task<IActionResult> GetMoney(CancellationToken cancellationToken)
  {
    var userId = GetUserId();
    if (userId is null)
    {
      return Unauthorized();
    }

    var request = new HttpRequestMessage(HttpMethod.Get, $"/api/users/{userId}/money");
    CopyAuthorizationHeader(request);

    return await ForwardAsync(request, cancellationToken);
  }

  [HttpPost("add")]
  public async Task<IActionResult> AddMoney(
    [FromBody] MoneyAdjustmentRequest body,
    CancellationToken cancellationToken)
  {
    var userId = GetUserId();
    if (userId is null)
    {
      return Unauthorized();
    }

    var request = new HttpRequestMessage(HttpMethod.Post, $"/api/users/{userId}/money/add")
    {
      Content = CreateJsonContent(body)
    };
    CopyAuthorizationHeader(request);

    return await ForwardAsync(request, cancellationToken);
  }

  private static StringContent CreateJsonContent<T>(T body)
  {
    var payload = JsonSerializer.Serialize(body);
    return new StringContent(payload, Encoding.UTF8, "application/json");
  }

  private void CopyAuthorizationHeader(HttpRequestMessage request)
  {
    if (Request.Headers.TryGetValue("Authorization", out var value))
    {
      request.Headers.TryAddWithoutValidation("Authorization", value.ToString());
    }
  }

  private string? GetUserId()
  {
    return User.FindFirstValue(ClaimTypes.NameIdentifier);
  }

  private async Task<IActionResult> ForwardAsync(HttpRequestMessage request, CancellationToken cancellationToken)
  {
    using var response = await _httpClient.SendAsync(request, cancellationToken);
    if (response.Content is null || response.StatusCode == HttpStatusCode.NoContent)
    {
      return StatusCode((int)response.StatusCode);
    }

    var payload = await response.Content.ReadAsStringAsync(cancellationToken);
    var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";

    return new ContentResult
    {
      StatusCode = (int)response.StatusCode,
      ContentType = contentType,
      Content = payload
    };
  }
}

public sealed record MoneyAdjustmentRequest(decimal Amount);
