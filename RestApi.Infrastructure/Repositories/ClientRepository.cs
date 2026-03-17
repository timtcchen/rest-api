using RestApi.Application.Interfaces;
using RestApi.Domain.Entities;
using RestApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace RestApi.Infrastructure.Repositories;

/// <summary>
/// EF Core 實作的 OAuth 2.0 用戶端存取庫。
/// </summary>
public class ClientRepository : IClientRepository
{
    private readonly AppDbContext _context;

    public ClientRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public Task<ClientCredential?> FindByClientIdAsync(string clientId, CancellationToken cancellationToken = default)
        => _context.OAuthClients
                   .AsNoTracking()
                   .FirstOrDefaultAsync(c => c.ClientId == clientId, cancellationToken);
}
