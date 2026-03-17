using RestApi.Application.Interfaces;
using RestApi.Application.Services;
using RestApi.Infrastructure;
using RestApi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;

// ── Serilog 初始化（Bootstrap Logger，用於啟動階段的錯誤捕捉）────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog：從 appsettings.json 的 "Serilog" 區段讀取完整設定 ────────────
    builder.Host.UseSerilog((context, services, configuration) =>
        configuration.ReadFrom.Configuration(context.Configuration)
                     .ReadFrom.Services(services)
                     .Enrich.FromLogContext());

    // ── 服務註冊 ──────────────────────────────────────────────────────────────────

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    // Swagger 設定：加入 Bearer Token 授權支援，使 Swagger UI 可直接測試受保護的 API
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "RestApi",
            Version = "v1",
            Description = "REST API - OAuth 2.0 Client Credentials Flow 範例（純 .NET 實作，不依賴 IdentityServer）"
        });

        // 定義 Bearer Token 安全方案
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "請先呼叫 POST /connect/token 取得 access_token，\n" +
                          "然後在此輸入 Bearer {access_token}"
        });

        // 套用 Bearer Token 安全需求至所有端點
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // TokenService 以 Singleton 注入（無狀態，安全）
    builder.Services.AddSingleton<ITokenService, TokenService>();

    // Infrastructure 層（EF Core + IClientRepository）
    builder.Services.AddInfrastructure(builder.Configuration);

    // ── JWT 驗證設定 ──────────────────────────────────────────────────────────────

    var jwtSettings = builder.Configuration.GetSection("Jwt");
    // ⚠️ 生產環境請勿將 SecretKey 存放在 appsettings.json，應使用環境變數或金鑰管理服務
    var secretKey = jwtSettings["SecretKey"]
        ?? throw new InvalidOperationException("JWT SecretKey is not configured.");

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero  // 不允許時間誤差，Token 過期即失效
        };
    });

    builder.Services.AddAuthorization();

    // ── 建立 App ──────────────────────────────────────────────────────────────────

    var app = builder.Build();

    // 執行 EF Core migration 並植入初始資料
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();
        await DataSeeder.SeedAsync(db, logger);
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "RestApi v1");
            options.RoutePrefix = string.Empty; // 將 Swagger UI 設為根路徑
        });
    }

    app.UseHttpsRedirection();

    // Serilog 請求日誌（結構化記錄每一個 HTTP 請求）
    app.UseSerilogRequestLogging();

    // ⚠️ 順序重要：Authentication 必須在 Authorization 之前
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "應用程式啟動失敗。");
}
finally
{
    Log.CloseAndFlush();
}
