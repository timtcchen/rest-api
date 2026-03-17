using RestApi.Api.Models;
using RestApi.Application.DTOs;
using RestApi.Application.Interfaces;
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
    private readonly IClientRepository _clientRepository;
    private readonly ITokenService _tokenService;
    private readonly ILogger<OAuthController> _logger;

    public OAuthController(
        IClientRepository clientRepository,
        ITokenService tokenService,
        ILogger<OAuthController> logger)
    {
        _clientRepository = clientRepository;
        _tokenService = tokenService;
        _logger = logger;
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
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>包含 access_token 的 TokenResponse。</returns>
    [HttpPost("token")]
    [Consumes("application/x-www-form-urlencoded")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> TokenAsync(
        [FromForm] TokenRequest request,
        CancellationToken cancellationToken)
    {
        // Step 1：驗證 grant_type，僅支援 client_credentials
        if (request.GrantType != "client_credentials")
            return BadRequest(new { error = "unsupported_grant_type" });

        // Step 2：從資料庫查詢 client，驗證 client_id / client_secret
        // ⚠️ 生產環境建議：ClientSecret 應使用 bcrypt 雜湊儲存，驗證時比對雜湊值
        var client = await _clientRepository.FindByClientIdAsync(request.ClientId, cancellationToken);
        if (client is null || !SecretEquals(client.ClientSecret, request.ClientSecret))
        {
            _logger.LogWarning("認證失敗：client_id={ClientId}", request.ClientId);
            return Unauthorized(new { error = "invalid_client" });
        }

        // Step 3：驗證 scope，確認請求的 scope 均在 client 允許範圍內
        if (!string.IsNullOrWhiteSpace(request.Scope))
        {
            var requestedScopes = request.Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var invalidScopes = requestedScopes.Except(client.Scopes).ToList();
            if (invalidScopes.Count > 0)
            {
                _logger.LogWarning("無效的 scope 請求：client_id={ClientId}, InvalidScopes={InvalidScopes}",
                    request.ClientId, string.Join(" ", invalidScopes));
                return BadRequest(new { error = "invalid_scope" });
            }
        }

        // Step 4：產生並回傳 JWT Token
        var tokenResponse = _tokenService.GenerateToken(client, request.Scope);
        _logger.LogInformation("Token 核發成功：client_id={ClientId}", request.ClientId);
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
