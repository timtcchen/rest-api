using RestApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace RestApi.Infrastructure.Persistence;

/// <summary>
/// EF Core 資料庫上下文，管理 OAuth 2.0 用戶端憑證的持久化。
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ClientCredential> OAuthClients => Set<ClientCredential>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ClientCredential>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.ClientId)
                  .IsUnique();

            entity.Property(e => e.ClientId)
                  .IsRequired()
                  .HasMaxLength(256);

            entity.Property(e => e.ClientSecret)
                  .IsRequired()
                  .HasMaxLength(512);

            // 將 List<string> Scopes 序列化為 JSON 字串儲存於單一欄位
            entity.Property(e => e.Scopes)
                  .HasConversion(
                      v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                      v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                  .HasColumnType("TEXT");
        });
    }
}
