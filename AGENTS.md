# AGENTS.md

This file provides guidance to Codex (Codex.ai/code) when working with code in this repository.

## Project

CareConnect Egypt — a healthcare platform graduation project. ASP.NET Core Web API (.NET 10, Clean Architecture) backend + Angular 21 standalone-component frontend. Roles: `SuperAdmin`, `Patient`, `Doctor`, `Hospital`, `MedicalServiceProvider`.

The root `README.md` describes an earlier, unimplemented vision (MVC + Bootstrap + OpenAI) and does not reflect the actual codebase — do not trust it for architecture facts. Trust the code and this file instead.

## Commands

### Backend (run from repo root)

```bash
dotnet build CareConnect.slnx                     # build everything
dotnet run --project src/CareConnect.Api           # run the API (Development env picks up appsettings.Development.json)
dotnet test tests/CareConnect.Api.IntegrationTests # run all integration tests
dotnet test tests/CareConnect.Api.IntegrationTests --filter "FullyQualifiedName~AppointmentEndpointsTests"  # single test class
dotnet test tests/CareConnect.Api.IntegrationTests --filter "DisplayName~SomeTestMethodName"                # single test method
```

Migrations (Default Project `CareConnect.Infrastructure`, Startup Project `CareConnect.Api`):

```bash
dotnet ef migrations add <Name> -p src/CareConnect.Infrastructure -s src/CareConnect.Api
dotnet ef database update -p src/CareConnect.Infrastructure -s src/CareConnect.Api
dotnet ef migrations has-pending-model-changes -p src/CareConnect.Infrastructure -s src/CareConnect.Api  # sanity-check the model before deciding a migration is needed
```

Never run `dotnet ef database update` or add a migration unless the user has explicitly asked for it — schema changes touch a real local SQL Server database (`CareConnectEgypt`) with real seeded data.

### Frontend (run from `careconnect-client/`)

```bash
npm install
ng serve      # dev server, http://localhost:4200
ng build      # production build
ng test       # vitest via `ng test` (jsdom)
```

The Angular default `environment.ts` points at `https://localhost:7122/api` (the API's HTTPS profile). If running the API over plain HTTP (`http://localhost:5290`, the default `dotnet run` profile), either trust the dev cert (`dotnet dev-certs https --trust`) and use the HTTPS port, or point `environment.ts`/`environment.development.ts` at the HTTP port instead.

## Architecture

### Backend: Clean Architecture, 4 projects

`CareConnect.Domain` → `CareConnect.Application` → `CareConnect.Infrastructure` → `CareConnect.Api`. Domain has zero dependencies; Application defines interfaces that Infrastructure implements; Api wires DI and exposes controllers. Each layer's own `DependencyInjection.cs` (`AddApplication()`, `AddInfrastructure()`) registers its own services — check both when wiring something new.

**Response/Result pattern** (always follow this, never break the chain):
- Services never touch `HttpContext`. They return `Result<T>` (`CareConnect.Application.Common.Models.Result`) with a `ResultStatus` (`Success`, `ValidationFailed`, `Unauthorized`, `Forbidden`, `NotFound`, `Conflict`).
- Controllers extend `ApiControllerBase` and call `FromResult(result)`, which maps `ResultStatus` → HTTP status and wraps the payload in `ApiResponse<T>` (`{ success, message, data, errors }`) — the single envelope shape the Angular client always unwraps.
- `ApiControllerBase.CurrentUserId` reads the user id claim; controllers never accept a caller's own id from the route/body.

**Ownership pattern**: every "my own X" service method takes the caller's `userId` (never a client-supplied profile id) and resolves the owning profile from it internally. When a route *does* take a target id (e.g. `PATCH /hospital/blood-requests/{id}/approve`), the service loads the row and checks it belongs to the caller's own profile — a missing row and someone-else's row both return the identical 404, so there's no oracle for probing other accounts' ids.

**Validation**: FluentValidation validators live under `Application/Validation/{Area}/`, registered via `AddValidatorsFromAssembly` — any argument type with a matching registered validator (body-bound or query-bound) is auto-validated by `FluentValidationFilter` before the action runs; controllers never start with a manual `ModelState` check.

**EF Core conventions**:
- Nothing is ever hard-deleted. Lookup entities (Specialty, InsuranceCompany) use an `IsActive` toggle; workflow entities (Appointment, DoctorHospitalAffiliation, InsuranceRequest, BloodRequest) use a terminal status enum. Every enum has explicit numeric values.
- Every FK back to `ApplicationUser`/profile tables uses `DeleteBehavior.Restrict` — multiple paths to the same root would otherwise trip SQL Server's "multiple cascade paths" error, and history must survive an account deactivation anyway.
- Race conditions (double-booking, duplicate active request) get a filtered unique index as a last line of defense (e.g. `IX_Appointments_Doctor_Date_StartTime_ActiveUnique` filtered to `Pending`/`Confirmed`), *in addition to* an application-level recheck inside a transaction immediately before `SaveChangesAsync`.
- Reads use `.AsNoTracking()` and project straight to DTOs via `Expression<Func<Entity, Dto>>` static methods — controllers and services never return EF entities directly.
- `PagedQueryParameters` (page/pageSize clamping, max 100) is the base class for every list-query DTO.

**Seeding**: `DatabaseSeeder` (`Infrastructure/Persistence/Seed/`) runs unconditionally on every startup in every environment (roles, SuperAdmin bootstrap from `SuperAdmin:Email/Password` config, specialties, insurance companies) — fully idempotent, matches on natural keys (email, Name), never touches existing rows it didn't create. `DemoDataSeeder` is separate, Development-only, gated by `DemoData:Enabled` in config, and does the heavier realistic dataset (hospitals, doctors, affiliations, schedules, appointments, blood stock, insurance requests) — also fully idempotent by natural key, never re-decrements BloodStock on re-runs. Both are wired into `Program.cs` after `WebApplication.Build()`.

Note: this project also has local **user-secrets** (`UserSecretsId` in `CareConnect.Api.csproj`) that override `appsettings.Development.json` — if a config value doesn't seem to be taking effect, check `dotnet user-secrets list --project src/CareConnect.Api` before assuming the JSON file is wrong.

### Frontend: Angular 21, standalone components only (no NgModules)

Structure under `careconnect-client/src/app/`:
- `core/models/` — one `*.model.ts` per feature area, mirroring the matching backend DTO shape (including enum string-union types matching C# enum names, e.g. `AppointmentStatus = 'Pending' | 'Confirmed' | ...`).
- `core/services/` — one Angular service per backend controller/resource, thin HTTP wrappers returning `Observable<T>` (unwrapping `ApiResponse`/`PagedResult` via `map`).
- `core/guards/` — `authGuard`, `roleGuard(...roles)`, `guestGuard`.
- `core/interceptors/` — `jwtInterceptor` (single-flight refresh-token retry), `errorInterceptor` (`friendlyMessageOf`/`validationErrorsOf` helpers used everywhere for error display).
- `features/{role}/{area}/` — page components, one folder per feature area per role (e.g. `features/hospital/blood-stock/`).
- `layouts/main-layout/` — the signed-in chrome; `NAV_BY_ROLE` there is the single place top-nav links are added per role.
- `shared/` — reusable dialogs (`ConfirmDialog` generic yes/no, `ReasonDialog` generic single-reason-field, plus a few page-local multi-field dialogs that live next to the page that uses them instead).

Conventions: `ChangeDetectionStrategy.OnPush` everywhere, Signals for local state, Reactive Forms, `withComponentInputBinding()` so route path params *and* query params bind directly to component `input()`s. Global SCSS utility classes (`.cc-card-grid`, `.cc-filters`, `.cc-status-chip` `--active`/`--pending`/`--inactive`, `.cc-empty-state`, `.cc-loading`, `.cc-notice`) live in `src/styles.scss` — reuse them rather than writing new page-specific layout CSS.

Browser geolocation (used by hospital-nearby search) is only ever requested from a direct user click (button handler), never automatically — see `GeolocationService` and its callers.

### Testing

Integration tests (`tests/CareConnect.Api.IntegrationTests/`) host the real API via `WebApplicationFactory<Program>` (see `CareConnectApiFactory`) against an in-memory SQLite database created with `EnsureCreated` — no migration involved, and it can never touch the real developer database. All tests share one `[Collection(nameof(ApiCollection))]` fixture and therefore run sequentially against the same seeded instance; use `TestHttp.UniqueEmail(prefix)` for any new user a test creates so tests never collide on unique email/phone indexes. `TestHttp.ReadEnvelopeAsync<T>()` decodes the `ApiResponse` envelope from an `HttpResponseMessage`.

Several features (Insurance, Blood Bank, Hospital Location/Discovery, the demo data seeder) were built under an explicit "do not add automated tests" instruction and have no integration test coverage — verification for those was manual/code-review only. Don't assume test coverage implies feature completeness or vice versa.
