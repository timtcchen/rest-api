using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RestApi.Api.Controllers;

/// <summary>
/// 受 JWT 保護的 Weather API 範例控制器，示範如何搭配 OAuth 2.0 Client Credentials Flow 保護 API 端點。
///
/// 使用方式：
///   1. 先呼叫 POST /connect/token 取得 access_token
///   2. 在 Authorization Header 加入 Bearer {access_token}
///   3. 呼叫此 Controller 的端點
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]  // 整個 Controller 需要有效的 JWT Bearer Token
[Tags("Weather (Protected API)")]
public class WeatherController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild",
        "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    /// <summary>
    /// 取得天氣預報（需要 "read" scope）。
    /// </summary>
    /// <remarks>
    /// 需要有效的 Bearer Token，且 Token 的 scope 必須包含 "read"。
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult Get()
    {
        // 從 JWT Claims 取得 client_id 與 scope
        var clientId = User.FindFirst("client_id")?.Value ?? "unknown";
        var scope = User.FindFirst("scope")?.Value ?? "";

        // 檢查是否具有 "read" scope
        if (!HasScope(scope, "read"))
            return Forbid(); // 403 Forbidden

        var forecast = Enumerable.Range(1, 5).Select(index => new
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        }).ToArray();

        return Ok(new
        {
            Message = $"Hello, {clientId}! 以下是你的天氣預報。",
            GrantedScope = scope,
            Forecast = forecast
        });
    }

    /// <summary>
    /// 新增天氣資料（需要 "write" scope）。
    /// </summary>
    /// <remarks>
    /// 需要有效的 Bearer Token，且 Token 的 scope 必須包含 "write"。
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult Post()
    {
        var clientId = User.FindFirst("client_id")?.Value ?? "unknown";
        var scope = User.FindFirst("scope")?.Value ?? "";

        // 檢查是否具有 "write" scope
        if (!HasScope(scope, "write"))
            return Forbid(); // 403 Forbidden - Token 沒有 write 權限

        return Ok(new
        {
            Message = $"Hello, {clientId}! 寫入操作成功。",
            GrantedScope = scope
        });
    }

    /// <summary>
    /// 檢查 JWT scope 字串中是否包含指定的 scope。
    /// </summary>
    /// <param name="scopeClaim">JWT 中的 scope claim 值（空白分隔）。</param>
    /// <param name="requiredScope">需要的 scope 名稱。</param>
    private static bool HasScope(string scopeClaim, string requiredScope) =>
        scopeClaim.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                  .Contains(requiredScope);
}
