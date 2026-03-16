using RestApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddSingleton<TokenService>();

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

// ⚠️ 順序重要：Authentication 必須在 Authorization 之前
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

