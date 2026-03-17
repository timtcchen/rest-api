namespace RestApi.Domain.Entities;

/// <summary>
/// OAuth 2.0 用戶端憑證實體，儲存於資料庫。
///
/// ⚠️ 生產環境安全建議：
///   1. ClientSecret 應使用 bcrypt/Argon2 雜湊後儲存，驗證時比對雜湊值。
///   2. 不應將真實 Secret 明文存放於版本控制；改用環境變數或
///      Azure Key Vault / AWS Secrets Manager 等金鑰管理服務。
///   3. 定期輪換 Client Secret。
/// </summary>
public class ClientCredential
{
    /// <summary>主鍵（資料庫自動遞增）。</summary>
    public int Id { get; set; }

    /// <summary>用戶端唯一識別碼。</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>用戶端密碼（明文，僅供開發範例，生產請使用雜湊）。</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>該 Client 允許請求的 scope 清單（以空白分隔儲存於資料庫）。</summary>
    public List<string> Scopes { get; set; } = new();
}
