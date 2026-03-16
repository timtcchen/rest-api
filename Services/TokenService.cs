using RestApi.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RestApi.Services;

/// <summary>
/// JWT Token 產生服務，負責依 Client Credentials 產生符合 OAuth 2.0 規範的 JWT。
///
/// 不依賴任何第三方 IdentityServer，僅使用原生 .NET 與 Microsoft.IdentityModel 套件。
/// </summary>
public class TokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config)
    {
        _config = config;
    }

    /// <summary>
    /// 依據已驗證的 ClientCredential 與請求的 scope 產生 JWT Access Token。
    /// </summary>
    /// <param name="client">已驗證的用戶端憑證資訊。</param>
    /// <param name="requestedScope">用戶端請求的 scope（空白分隔），null 表示請求所有允許的 scope。</param>
    /// <returns>包含 access_token、token_type、expires_in、scope 的回應物件。</returns>
    public TokenResponse GenerateToken(ClientCredential client, string? requestedScope)
    {
        var jwtSettings = _config.GetSection("Jwt");
        var secretKey = jwtSettings["SecretKey"]
            ?? throw new InvalidOperationException("JWT SecretKey is not configured.");
        var issuer = jwtSettings["Issuer"]
            ?? throw new InvalidOperationException("JWT Issuer is not configured.");
        var audience = jwtSettings["Audience"]
            ?? throw new InvalidOperationException("JWT Audience is not configured.");
        var expiresIn = int.TryParse(jwtSettings["ExpiresInSeconds"], out var parsed) ? parsed : 3600;

        // 計算實際授予的 scope：取請求 scope 與 client 允許 scope 的交集
        // 若未指定 scope，則授予該 client 所有允許的 scope
        var grantedScopes = string.IsNullOrWhiteSpace(requestedScope)
            ? client.Scopes
            : requestedScope.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(s => client.Scopes.Contains(s))
                .ToList();

        var now = DateTime.UtcNow;

        // 建立 JWT Claims
        var claims = new List<Claim>
        {
            // client_id：識別發行對象
            new Claim("client_id", client.ClientId),
            // scope：授予的存取範圍
            new Claim("scope", string.Join(" ", grantedScopes)),
            // jti：Token 唯一識別碼，可用於防止重放攻擊
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            // iat：Token 核發時間（Unix timestamp）
            new Claim(JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(now).ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var signingCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var jwtToken = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: now,
            expires: now.AddSeconds(expiresIn),
            signingCredentials: signingCredentials
        );

        return new TokenResponse
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(jwtToken),
            ExpiresIn = expiresIn,
            Scope = string.Join(" ", grantedScopes)
        };
    }
}
