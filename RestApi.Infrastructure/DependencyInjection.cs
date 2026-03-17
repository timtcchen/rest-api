using RestApi.Application.Interfaces;
using RestApi.Infrastructure.Persistence;
using RestApi.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace RestApi.Infrastructure;

/// <summary>
/// Infrastructure 層的 DI 擴充方法。
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// 將 Infrastructure 層的服務（EF Core、Repository）註冊至 DI 容器。
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=restapi.db";

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<IClientRepository, ClientRepository>();

        return services;
    }
}
