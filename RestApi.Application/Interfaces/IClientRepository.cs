using RestApi.Domain.Entities;

namespace RestApi.Application.Interfaces;

/// <summary>
/// OAuth 2.0 用戶端憑證存取庫介面。
/// </summary>
public interface IClientRepository
{
    /// <summary>
    /// 依 client_id 查詢用戶端憑證。
    /// </summary>
    /// <param name="clientId">用戶端識別碼。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>找到則回傳 <see cref="ClientCredential"/>；否則回傳 null。</returns>
    Task<ClientCredential?> FindByClientIdAsync(string clientId, CancellationToken cancellationToken = default);
}
