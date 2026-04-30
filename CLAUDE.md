# Quraan_App Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-04-30

## Active Technologies
- C# 12 on .NET 8 LTS (backend); TypeScript 5.x on Angular 18 LTS (frontend) + ASP.NET Core 8 Web API, Entity Framework Core 8, ASP.NET Core Identity, JwtBearer auth, Serilog, Swashbuckle.AspNetCore, StackExchange.Redis, Konscious.Security.Cryptography.Argon2, FluentValidation (backend); Angular 18, Angular Material, RxJS, @ngx-translate/core, howler.js, ngx-virtual-scroller (frontend) (001-quran-mvp)
- SQL Server 2022 (LocalDB / SQL Express for dev, Azure SQL for prod). EF Core migrations checked into source. (001-quran-mvp)



## Project Structure

```text
backend/
  src/
    Quraan.Api/             # ASP.NET Core 8 Web API host (Controllers, Middleware, Logging)
    Quraan.Application/     # Use-cases, services, DTOs, validators (no infra deps)
    Quraan.Domain/          # Entities, repository interfaces, domain primitives
    Quraan.Infrastructure/  # EF Core, Identity, repositories, seeding, external clients
  tests/
    Quraan.UnitTests/        # In-memory unit tests (no Docker, no DB)
    Quraan.ContractTests/    # WebApplicationFactory wire-shape tests (in-memory DB)
    Quraan.IntegrationTests/ # Real SQL Server via Testcontainers (Docker required)
frontend/
  src/app/
    core/                   # http interceptors, last-read service, language service
    features/               # auth, audio, bookmarks, home, quran, search, tafsir
  tests/e2e/                # Playwright specs + a11y manual checklists
specs/001-quran-mvp/
  contracts/openapi.yaml    # Source of truth for API contract (drift-tested)
```

## Commands

```bash
# Backend
dotnet build backend/Quraan.sln
dotnet test  backend/tests/Quraan.UnitTests        # fast; no Docker needed
dotnet test  backend/tests/Quraan.ContractTests    # fast; in-memory DB
dotnet test  backend/tests/Quraan.IntegrationTests # requires Docker (Testcontainers)
dotnet run   --project backend/src/Quraan.Api -- seed            # seed Surahs/Ayahs
dotnet run   --project backend/src/Quraan.Api -- verify-content  # checksum gate

# Frontend
cd frontend
npm ci
npx ng test --watch=false --browsers=ChromeHeadless
npx ng build
npx playwright test                                  # e2e (against running stack)
```

## Code Style

General: Follow standard conventions. Backend tests use `xunit` + `FluentAssertions`;
frontend uses Karma/Jasmine + `provideHttpClientTesting()`. All API errors return
RFC 7807 ProblemDetails (`application/problem+json`).

## Recent Changes
- 001-quran-mvp: Added C# 12 on .NET 8 LTS (backend); TypeScript 5.x on Angular 18 LTS (frontend) + ASP.NET Core 8 Web API, Entity Framework Core 8, ASP.NET Core Identity, JwtBearer auth, Serilog, Swashbuckle.AspNetCore, StackExchange.Redis, Konscious.Security.Cryptography.Argon2, FluentValidation (backend); Angular 18, Angular Material, RxJS, @ngx-translate/core, howler.js, ngx-virtual-scroller (frontend)



<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
