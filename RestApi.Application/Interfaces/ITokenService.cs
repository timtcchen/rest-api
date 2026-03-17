using RestApi.Domain.Entities;

namespace RestApi.Application.Interfaces;

/// <summary>
/// JWT Token 產生服務介面。
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// 依據已驗證的 ClientCredential 與請求的 scope 產生 JWT Access Token。
    /// </summary>
    /// <param name="client">已驗證的用戶端憑證資訊。</param>
    /// <param name="requestedScope">用戶端請求的 scope（空白分隔），null 表示請求所有允許的 scope。</param>
    /// <returns>包含 access_token、token_type、expires_in、scope 的回應物件。</returns>
    DTOs.TokenResponse GenerateToken(ClientCredential client, string? requestedScope);
}
