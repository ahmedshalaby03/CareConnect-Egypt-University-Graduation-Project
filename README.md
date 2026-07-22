# CareConnect Egypt

Academic healthcare platform. **Step 1 — foundation and authentication only.**

Appointments, insurance, blood bank, maps and AI are intentionally out of scope in this step.

## Solution layout

```
CareConnectEgypt/
├── CareConnect.slnx
├── src/
│   ├── CareConnect.Domain          # Entities, role/claim constants (no dependencies)
│   ├── CareConnect.Application      # Interfaces, DTOs, validation, Result/ApiResponse
│   ├── CareConnect.Infrastructure   # EF Core, Identity, JWT, services, seeding
│   └── CareConnect.Api              # Controllers, auth/authz wiring, Swagger, Serilog
├── tests/
│   └── CareConnect.Api.IntegrationTests   # 37 end-to-end tests over an in-memory SQLite DB
└── careconnect-client               # Angular 21 standalone app
```

## Prerequisites

- .NET 10 SDK
- SQL Server (LocalDB is fine for development)
- Node.js 20+ and npm
- `dotnet-ef` global tool: `dotnet tool install --global dotnet-ef`

## Backend configuration

Secrets are **not** committed. For development, values are read from user secrets first,
then `appsettings.Development.json`. The `Jwt:Key`, `SuperAdmin:Email` and
`SuperAdmin:Password` used at runtime are set in user secrets (see below).

Required keys:

| Key | Purpose | Example (development) |
| --- | --- | --- |
| `ConnectionStrings:DefaultConnection` | SQL Server connection | `Server=(localdb)\MSSQLLocalDB;Database=CareConnectEgypt;Trusted_Connection=True;TrustServerCertificate=True` |
| `Jwt:Key` | HMAC signing key, **≥ 32 chars** | a 64+ char random string |
| `Jwt:Issuer` / `Jwt:Audience` | token issuer / audience | `CareConnectEgypt` / `CareConnectEgyptClient` |
| `SuperAdmin:Email` | seeded admin login | `admin@careconnect.com` |
| `SuperAdmin:Password` | seeded admin password | `ChangeThisPassword123!` |
| `Cors:AllowedOrigins` | allowed browser origins | `["http://localhost:4200"]` |

Set the sensitive ones as user secrets (run from the repo root):

```bash
dotnet user-secrets set "Jwt:Key" "<a-64-char-random-string>" --project src/CareConnect.Api
dotnet user-secrets set "SuperAdmin:Email" "admin@careconnect.com" --project src/CareConnect.Api
dotnet user-secrets set "SuperAdmin:Password" "ChangeThisPassword123!" --project src/CareConnect.Api
```

## Database — run migrations manually

Migrations are **not** applied automatically. Create and apply the initial migration yourself:

```bash
dotnet ef migrations add InitialIdentityAndProfiles -p src/CareConnect.Infrastructure -s src/CareConnect.Api
```

```bash
dotnet ef database update -p src/CareConnect.Infrastructure -s src/CareConnect.Api
```

Roles and the SuperAdmin account are seeded automatically on API start-up (idempotent). The
schema is never touched by seeding — only the migration commands above change the database.

## Run

Backend (serves Swagger at the root in Development):

```bash
dotnet run --project src/CareConnect.Api
```

- HTTP: <http://localhost:5290>  ·  Swagger: <http://localhost:5290/swagger>
- HTTPS: <https://localhost:7122>

Frontend:

```bash
npm start --prefix careconnect-client
```

- App: <http://localhost:4200> (the API's CORS policy already allows this origin)

## Tests

```bash
dotnet test
```

37 integration tests boot the real API over an in-memory SQLite database (no SQL Server and
no migration needed) and cover registration for all four roles, login, `/me`, refresh-token
rotation and reuse detection, inactive-user lockout, and SuperAdmin authorization.

## Seeded SuperAdmin credentials (development)

- Email: `admin@careconnect.com`
- Password: `ChangeThisPassword123!`

**Change this password before any non-local deployment.** Registration cannot create a
SuperAdmin — the role is seed-only.

## API surface

```
POST /api/auth/register
POST /api/auth/login
POST /api/auth/refresh-token
POST /api/auth/revoke-token       (auth)
POST /api/auth/change-password    (auth)
POST /api/auth/logout             (auth)
GET  /api/auth/me                 (auth)

GET   /api/super-admin/users                         (SuperAdmin) — search, role/status filter, paging
PATCH /api/super-admin/users/{userId}/toggle-status  (SuperAdmin)
```
