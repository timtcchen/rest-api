using System.Text.Json.Serialization;

namespace BpmApi.Models;

/// <summary>
/// OAuth 2.0 Token 回應模型，符合 RFC 6749 Section 5.1 規範。
/// </summary>
public class TokenResponse
{
    /// <summary>核發的 JWT Access Token。</summary>
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>Token 類型，固定為 "Bearer"。</summary>
    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "Bearer";

    /// <summary>Token 有效期（秒）。</summary>
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    /// <summary>實際授予的 scope（空白分隔）。</summary>
    [JsonPropertyName("scope")]
    public string? Scope { get; set; }
}
