using RestApi.Api.Models;
using RestApi.Application.DTOs;
using RestApi.Application.Interfaces;
using RestApi.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

namespace RestApi.Api.Controllers;

/// <summary>
/// OAuth 2.0 授權端點控制器。
/// 提供 Client Credentials Flow 的 Token 核發功能，不依賴任何外部 IdentityServer。
///
/// 測試方式（Postman 或 curl）：
///   POST /connect/token
///   Content-Type: application/x-www-form-urlencoded
///   Body: grant_type=client_credentials&amp;client_id=service-a&amp;client_secret=secret-a&amp;scope=read write
/// </summary>
[ApiController]
[Route("connect")]
[Tags("OAuth 2.0")]
public class OAuthController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly ITokenService _tokenService;

    public OAuthController(IConfiguration config, ITokenService tokenService)
    {
        _config = config;
        _tokenService = tokenService;
    }

    /// <summary>
    /// 核發 OAuth 2.0 Access Token（Client Credentials Flow）。
    /// </summary>
    /// <remarks>
    /// 請求格式（application/x-www-form-urlencoded）：
    ///
    ///     grant_type=client_credentials
    ///     client_id=service-a
    ///     client_secret=secret-a
    ///     scope=read write   （選填；省略時授予所有允許的 scope）
    ///
    /// 錯誤回應符合 RFC 6749 Section 5.2 規範：
    /// - 400 unsupported_grant_type：grant_type 不為 client_credentials
    /// - 401 invalid_client：client_id 或 client_secret 不符
    /// - 400 invalid_scope：請求的 scope 超出允許範圍
    /// </remarks>
    /// <param name="request">Token 請求參數。</param>
    /// <returns>包含 access_token 的 TokenResponse。</returns>
    [HttpPost("token")]
    [Consumes("application/x-www-form-urlencoded")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Token([FromForm] TokenRequest request)
    {
        // Step 1：驗證 grant_type，僅支援 client_credentials
        if (request.GrantType != "client_credentials")
            return BadRequest(new { error = "unsupported_grant_type" });

        // Step 2：從設定檔取得所有已知 Clients，驗證 client_id / client_secret
        // ⚠️ 生產環境建議：從資料庫查詢並使用 bcrypt 比對 hashed secret
        var clients = _config.GetSection("OAuthClients")
                             .Get<List<ClientCredential>>() ?? new List<ClientCredential>();

        // 先依 client_id 找到 client，再使用常數時間比較避免 timing attack
        var client = clients.FirstOrDefault(c => c.ClientId == request.ClientId);
        if (client is null || !SecretEquals(client.ClientSecret, request.ClientSecret))
            return Unauthorized(new { error = "invalid_client" });

        // Step 3：驗證 scope，確認請求的 scope 均在 client 允許範圍內
        if (!string.IsNullOrWhiteSpace(request.Scope))
        {
            var requestedScopes = request.Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var invalidScopes = requestedScopes.Except(client.Scopes).ToList();
            if (invalidScopes.Count > 0)
                return BadRequest(new { error = "invalid_scope" });
        }

        // Step 4：產生並回傳 JWT Token
        var tokenResponse = _tokenService.GenerateToken(client, request.Scope);
        return Ok(tokenResponse);
    }

    /// <summary>
    /// 使用常數時間比較（constant-time comparison）比對兩個字串，防止 timing attack。
    /// </summary>
    private static bool SecretEquals(string storedSecret, string providedSecret)
    {
        var storedBytes = Encoding.UTF8.GetBytes(storedSecret);
        var providedBytes = Encoding.UTF8.GetBytes(providedSecret);
        return CryptographicOperations.FixedTimeEquals(storedBytes, providedBytes);
    }
}
