using Microsoft.AspNetCore.Mvc;

namespace RestApi.Api.Models;

/// <summary>
/// OAuth 2.0 Client Credentials 授權請求模型。
/// 符合 RFC 6749 規範，使用 application/x-www-form-urlencoded 格式傳送。
/// </summary>
public class TokenRequest
{
    /// <summary>授權類型，Client Credentials Flow 固定為 "client_credentials"。</summary>
    [FromForm(Name = "grant_type")]
    public string GrantType { get; set; } = string.Empty;

    /// <summary>用戶端識別碼。</summary>
    [FromForm(Name = "client_id")]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// 用戶端密碼。
    /// ⚠️ 生產環境請勿明文儲存，應使用 bcrypt hash 或 Azure Key Vault。
    /// </summary>
    [FromForm(Name = "client_secret")]
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// 請求的 scope（空白分隔），若省略則授予該 client 所有允許的 scope。
    /// </summary>
    [FromForm(Name = "scope")]
    public string? Scope { get; set; }
}
