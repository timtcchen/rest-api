using RestApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace RestApi.Infrastructure.Persistence;

/// <summary>
/// 負責在應用程式啟動時為資料庫植入初始 OAuth 2.0 用戶端資料。
/// </summary>
public static class DataSeeder
{
    /// <summary>
    /// 執行 migration 並植入初始資料（僅在資料不存在時插入）。
    /// </summary>
    public static async Task SeedAsync(AppDbContext context, ILogger logger)
    {
        await context.Database.MigrateAsync();

        if (await context.OAuthClients.AnyAsync())
        {
            logger.LogInformation("資料庫已有 OAuth 用戶端資料，跳過 seed。");
            return;
        }

        // ⚠️ 生產環境請勿使用明文 Secret；應使用 bcrypt/Argon2 雜湊或由環境變數注入
        var clients = new List<ClientCredential>
        {
            new ClientCredential
            {
                ClientId = "service-a",
                ClientSecret = "dev-secret-service-a-replace-in-production",
                Scopes = new List<string> { "read", "write" }
            },
            new ClientCredential
            {
                ClientId = "service-b",
                ClientSecret = "dev-secret-service-b-replace-in-production",
                Scopes = new List<string> { "read" }
            }
        };

        context.OAuthClients.AddRange(clients);
        await context.SaveChangesAsync();

        logger.LogInformation("已植入 {Count} 筆 OAuth 用戶端資料。", clients.Count);
    }
}
