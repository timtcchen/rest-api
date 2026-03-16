# bpmapi

BPM API — .NET 8 Web API with OAuth 2.0 Client Credentials Flow (pure .NET implementation, no IdentityServer required).

## Features

- **OAuth 2.0 Client Credentials Flow** — `POST /connect/token`
- **JWT Bearer Authentication** using `Microsoft.AspNetCore.Authentication.JwtBearer`
- **Multi-client support** with per-client configurable scopes
- **Swagger UI** with Bearer Token support for interactive testing
- **Protected API example** — `GET /api/weather` (requires `read` scope), `POST /api/weather` (requires `write` scope)

## Project Structure

```
BpmApi/
├── Controllers/
│   ├── OAuthController.cs       # Token issuance endpoint (POST /connect/token)
│   └── WeatherController.cs     # Protected API example
├── Models/
│   ├── TokenRequest.cs          # OAuth 2.0 token request (form fields)
│   ├── TokenResponse.cs         # OAuth 2.0 token response
│   └── ClientCredential.cs      # Client config model
├── Services/
│   └── TokenService.cs          # JWT generation logic
├── appsettings.json             # JWT & OAuthClients config (placeholder values)
├── appsettings.Development.json # Dev-only config (overrides appsettings.json)
└── Program.cs                   # Auth/Swagger setup
```

## Getting Started

```bash
dotnet restore
dotnet run
```

Swagger UI will be available at: `https://localhost:5001` (or `http://localhost:5000`)

## Testing with Postman

### Step 1 — Get a Token

```
POST /connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials
client_id=service-a
client_secret=dev-secret-service-a-replace-in-production
scope=read write
```

Response:
```json
{
  "access_token": "eyJhbGciOiJIUzI1NiIs...",
  "token_type": "Bearer",
  "expires_in": 3600,
  "scope": "read write"
}
```

### Step 2 — Call the Protected API

```
GET /api/weather
Authorization: Bearer <access_token>
```

### Testing with Swagger UI

1. Open Swagger UI at the root URL
2. Call `POST /connect/token` to get a token
3. Click **Authorize** (lock icon), enter `Bearer <access_token>`
4. Call `GET /api/weather` or `POST /api/weather`

## Multi-Client & Scope Support

Configured in `appsettings.Development.json`:

| ClientId    | Scopes         |
|-------------|----------------|
| `service-a` | `read`, `write` |
| `service-b` | `read`          |

## ⚠️ Production Security Recommendations

| Item | Recommendation |
|------|---------------|
| **JWT SecretKey** | Use a cryptographically random key (≥ 32 chars); store in environment variables or Azure Key Vault, never in source code |
| **Client Secrets** | Hash with bcrypt/Argon2 and store in a database; do not store plaintext in config files |
| **Transport** | Enforce HTTPS only |
| **Token Revocation** | Implement a token blacklist (e.g., Redis) or use short expiry |
| **Rate Limiting** | Apply rate limiting to `POST /connect/token` to prevent brute-force attacks |
| **Secret Rotation** | Rotate client secrets periodically |
