# Implementation Plan: Quran & Islamic Companion Web App (MVP)

**Branch**: `001-quran-mvp` | **Date**: 2026-04-27 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-quran-mvp/spec.md`

## Summary

Deliver an MVP web app that lets any visitor read the full Qur'an (Arabic +
English translation) for all 114 Surahs, switch the UI between Arabic (RTL) and
English (LTR), play per-Surah recitation with optional Ayah-level highlight,
search by Surah name or Ayah text, read Tafsir Ibn Kathir per Ayah, and
optionally sign in to bookmark Ayahs that sync across devices.

**Technical approach**: ASP.NET Core 8 Web API (Controllers → Services →
Repositories over EF Core 8 against SQL Server) exposes `/api/v1` REST
endpoints; an Angular (latest LTS) SPA with lazy-loaded feature modules and
`@ngx-translate` for i18n consumes the API, falls back to public Quran audio
sources (Al Quran Cloud + quran.com v4) for streaming + timing markers, and
persists user state via JWT-authenticated bookmark/last-read endpoints. Quran
text (Tanzil Uthmani Hafs), Saheeh International translation, and a public
Tafsir Ibn Kathir digest are seeded once at deploy time, attribution surfaced in
the UI per Principle I.

## Technical Context

**Language/Version**: C# 12 on .NET 8 LTS (backend); TypeScript 5.x on Angular 20 (frontend) — Angular 20 chosen per Constitution §Tech Stack "Angular (latest stable major)"; the .NET SDK installed locally is 10.0.201, which can target `net8.0` natively. Solution file uses the new `.slnx` format introduced in SDK 9+.
**Primary Dependencies**: ASP.NET Core 8 Web API, Entity Framework Core 8, ASP.NET Core Identity, JwtBearer auth, Serilog, Swashbuckle.AspNetCore, StackExchange.Redis, Konscious.Security.Cryptography.Argon2, FluentValidation (backend); Angular 18, Angular Material, RxJS, @ngx-translate/core, howler.js, ngx-virtual-scroller (frontend)
**Storage**: SQL Server 2022 (LocalDB / SQL Express for dev, Azure SQL for prod). EF Core migrations checked into source.
**Testing**: xUnit + FluentAssertions + Moq (backend unit); WebApplicationFactory + Testcontainers.MsSql (backend integration); OpenAPI snapshot (backend contract); Jasmine + Karma (frontend unit); Playwright + axe-core (frontend e2e + a11y)
**Target Platform**: Modern evergreen browsers (Chromium last-2 majors, Firefox ESR, Safari 16+), mobile-first responsive; backend on Linux (Azure App Service) + Azure SQL in production
**Project Type**: Web application (backend Web API + frontend SPA)
**Performance Goals**: API content-read p95 ≤ 300 ms (SC-001/002, Const. VI); audio first sound ≤ 2 s (SC-003); Angular initial route bundle ≤ 250 KB gzipped; language switch render ≤ 500 ms (SC-009). All client-side latency targets measured against the Playwright `Slow 3G` profile (≈400 kbps, 400 ms RTT) per spec.md §Assumptions "Network performance baseline".
**Constraints**: WCAG 2.1 AA for both Arabic (RTL) and English (LTR) UIs (SC-007); Quran text character-for-character match against canonical Tanzil Uthmani Hafs source verified by SHA-256 (SC-006); zero plaintext passwords (SC-010); API URL-segment versioning `/api/v1`; RFC 7807 error responses
**Scale/Scope**: 114 Surahs, ~6,236 Ayahs, ~6,236 Tafsir entries, 1 reciter (Alafasy) for MVP; target 1,000 DAU at launch (~10 req/s peak, cache-served); per-user hard cap 1,000 active bookmarks; 4 backend projects + 3 test projects + 1 Angular workspace

### Detailed dependency notes

- **Backend**: ASP.NET Core 8 Web API; Entity Framework Core 8; ASP.NET Core
  Identity (custom Argon2id hasher); `Microsoft.AspNetCore.Authentication.JwtBearer`;
  Serilog (Console + a centralized sink in deployed envs);
  `Swashbuckle.AspNetCore` for OpenAPI generation;
  `Microsoft.Extensions.Caching.Memory` (dev) and `StackExchange.Redis` via
  `IDistributedCache` (prod); `Konscious.Security.Cryptography.Argon2` for
  password hashing; `FluentValidation` for request validation.
- **Frontend**: Angular 18 (standalone components + functional router),
  Angular Router with `loadChildren` for lazy modules, Angular Material
  (component baseline), RxJS, `@ngx-translate/core` + `@ngx-translate/http-loader`
  for runtime i18n switching, `howler.js` for audio playback abstraction,
  `ngx-virtual-scroller` for long Surah Ayah lists, `nswag` for generating
  the typed API client from `swagger.json`.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Gates derived from `.specify/memory/constitution.md` v1.0.0 Principles I–VII.

| # | Gate | Plan satisfies it by | Status |
|---|---|---|---|
| I | **Sacred Content Integrity** — Quran ingested verbatim from a single citable canonical source with SHA-256 verification on every read path; attribution surfaced for translation + Tafsir; admin/AI-generated religious content not persisted. | Phase 0 selects Tanzil Uthmani Hafs + Saheeh International + public-domain Ibn Kathir digest. Ingest job (`Quraan.Infrastructure.Seed`) verifies source digest against a checked-in `expected.sha256`. **Boot-time gate**: `Program.cs` invokes `ChecksumVerifier.VerifyAll()` against the live DB rows before the Kestrel host begins listening, aborting startup on any mismatch — so every served request transitively passes the integrity check ("fail closed on every read path"). UI footer/per-Ayah popover shows translation + Tafsir attribution. No admin endpoints in MVP; data is read-only post-seed. | ✅ PASS |
| II | **Clean Layered Architecture** — Controllers thin, Services own domain, Repositories own EF; Angular feature modules lazy-loaded. | Solution split into `Quraan.Api` / `Quraan.Application` / `Quraan.Domain` / `Quraan.Infrastructure`; Angular workspace under `frontend/src/app/{core,shared,features/{quran,tafsir,audio,search,bookmarks,auth}}` with each feature lazy-loaded via Angular Router. Domain has no ASP.NET / EF references. | ✅ PASS |
| III | **Test-First Development** — Contract test gates each endpoint; integration tests use real SQL Server, never the in-memory provider; TDD for services and Angular services. | `backend/tests/Contract` snapshot-tests each `/api/v1` endpoint against the OpenAPI spec. `backend/tests/Integration` uses Testcontainers.MsSql. Frontend Jasmine/Karma per service & component; Playwright e2e for the seven user stories. | ✅ PASS |
| IV | **Bilingual & Accessible Experience** — i18n source files, logical CSS properties, Mushaf-grade Arabic font, WCAG 2.1 AA. | `@ngx-translate` with `ar.json` + `en.json`; lint rule (`@angular-eslint/template/i18n`) blocks literal templated strings on merge. KFGQPC Uthmanic Hafs font for Quran text; system UI font for chrome. CSS uses `margin-inline-*`/`padding-inline-*`. Axe-core checks in CI. Locale toggle persisted client-side (anonymous) and server-side (signed-in). | ✅ PASS |
| V | **Security & Privacy by Default** — JWT (≤15 min) + rotating refresh tokens, Argon2id passwords, RBAC, parameterized queries, HTTPS, no PII in logs. | ASP.NET Core Identity + JWT bearer; `Konscious.Security.Cryptography.Argon2` configured to OWASP-current parameters; refresh tokens stored hashed (SHA-256) and revocable. All access via EF Core LINQ — no raw SQL in MVP. HSTS + HTTPS redirection in `Program.cs`. Serilog enricher strips email/IP; logs use opaque user `Guid`. | ✅ PASS |
| VI | **Performance, Scalability & Caching** — p95 ≤ 300 ms reads, response cache for immutable content, paginated lists, EF projection + AsNoTracking, Angular lazy load + bundle budget, audio range-streamed. | `IMemoryCache` (dev) + `IDistributedCache` (Redis, prod) wraps Surah/Ayah/Tafsir reads with 24-hour TTL and explicit invalidation on (future) admin edit. All list endpoints paginated (default 20, max 100) **except `/surahs`**, which returns a fixed bounded array of 114 reference rows; the bounded-list exemption is recorded in §Complexity Tracking with rationale. EF queries use `Select` projection + `AsNoTracking`. Angular `angular.json` `budgets` enforce 250 KB initial / 500 KB lazy gzipped. Audio served as direct CDN URL from Al Quran Cloud / quran.com — browser handles range requests natively. | ✅ PASS |
| VII | **Observability & API Contract Discipline** — Serilog with correlation IDs, `/health/live` + `/health/ready`, OpenAPI as source of truth, URL-segment versioning, RFC 7807 errors. | Serilog configured with `RequestLoggingMiddleware` adding `X-Correlation-ID` (echoed to response and propagated to Angular HTTP interceptor). `AddHealthChecks().AddDbContextCheck<QuraanDbContext>().AddRedis(...)`. Swashbuckle generates `swagger.json` per build; Angular client regenerated from it via `nswag` config. All controllers under `[Route("api/v1/[controller]")]`. Global exception handler emits `ProblemDetails`. | ✅ PASS |

**Result (initial check, pre-Phase 0)**: All gates PASS with no violations.
Complexity Tracking section below remains empty.

**Re-check (post-Phase 1 design)**: All gates re-evaluated against the
artifacts produced in Phases 0 and 1 (`research.md`, `data-model.md`,
`contracts/openapi.yaml`, `quickstart.md`):

- I. Content sources (R-01/02/03) are pinned, digest-verified, and surface
  attribution via `Translation.Attribution`, `TafsirSource.Attribution`,
  and the OpenAPI `attribution` fields on `TranslationInfo` / `TafsirEntry.source` / `AudioRecitation.reciter`.
- II. The four-project backend split and the Angular `features/*` lazy-loaded
  layout in §Project Structure match the principle.
- III. Quickstart §5 names the three backend test projects (Unit, Integration
  with `Testcontainers.MsSql`, Contract via OpenAPI snapshot) and the
  Jasmine/Playwright frontend suites.
- IV. R-08 and R-11 confirm `@ngx-translate` runtime switching, logical CSS,
  axe-core in CI, and contrast checks; OpenAPI exposes
  `UserProfile.preferredLanguage` for cross-device persistence.
- V. R-06 specifies Argon2id + JWT 15 min + rotating refresh with reuse
  detection; `data-model.md` `RefreshToken` stores only the SHA-256 hash;
  OpenAPI describes the `HttpOnly; Secure; SameSite=Strict` cookie scoped
  to `/auth/refresh`.
- VI. R-07 specifies `IDistributedCache` (Redis prod / Memory dev) with
  24 h TTL on content, 15 min on search; OpenAPI paginates every list
  endpoint with `pageSize` capped at 100.
- VII. All OpenAPI paths live under `/api/v1`; every error response is
  `ProblemDetails` (RFC 7807) with a `correlationId` field; `/health/live`
  and `/health/ready` are present.

No new violations introduced by the design phase. Complexity Tracking remains
empty.

## Project Structure

### Documentation (this feature)

```text
specs/001-quran-mvp/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output — OpenAPI spec
│   └── openapi.yaml
├── checklists/
│   └── requirements.md  # From /speckit.specify
└── tasks.md             # /speckit.tasks output (not created here)
```

### Source Code (repository root)

```text
backend/
├── Quraan.Backend.sln
├── src/
│   ├── Quraan.Api/                      # Controllers, DI composition, Program.cs
│   │   ├── Controllers/V1/
│   │   │   ├── SurahsController.cs
│   │   │   ├── AyahsController.cs
│   │   │   ├── SearchController.cs
│   │   │   ├── TafsirController.cs
│   │   │   ├── AudioController.cs       # Returns audio metadata + URLs (proxy/redirect)
│   │   │   ├── BookmarksController.cs
│   │   │   ├── LastReadController.cs
│   │   │   ├── AuthController.cs        # register, login, refresh, logout
│   │   │   └── HealthController.cs
│   │   ├── Middleware/                  # CorrelationId, ProblemDetails, RequestLogging
│   │   ├── Filters/
│   │   ├── appsettings.json
│   │   └── Program.cs
│   ├── Quraan.Application/              # Services, DTOs, validators, mappers
│   │   ├── Surahs/                      # SurahService, SurahDto, SurahQuery
│   │   ├── Ayahs/
│   │   ├── Search/                      # SearchService + ArabicNormalizer
│   │   ├── Tafsir/
│   │   ├── Audio/                       # AudioService — composes external URLs + timing
│   │   ├── Bookmarks/
│   │   ├── LastRead/
│   │   ├── Auth/                        # AuthService, TokenService, PasswordHasher (Argon2id)
│   │   ├── Common/                      # Result<T>, PageRequest, PageResult<T>
│   │   └── Caching/                     # CacheKeyFactory, ICachedReader
│   ├── Quraan.Domain/                   # Entities, value objects, domain interfaces
│   │   ├── Entities/
│   │   ├── Repositories/                # interfaces only
│   │   └── Common/
│   └── Quraan.Infrastructure/           # EF Core, repositories, external clients
│       ├── Persistence/
│       │   ├── QuraanDbContext.cs
│       │   ├── Configurations/          # IEntityTypeConfiguration<T>
│       │   └── Migrations/
│       ├── Repositories/                # implementations
│       ├── Seed/                        # Tanzil + Saheeh + IbnKathir importers + checksum verify
│       │   ├── Sources/                 # Pinned source files + expected.sha256
│       │   ├── QuranSeeder.cs
│       │   ├── TafsirSeeder.cs
│       │   └── ReciterSeeder.cs
│       ├── ExternalAudio/               # AlQuranCloudClient, QuranComClient (timing markers)
│       └── Identity/                    # ApplicationUser, RefreshTokenStore
└── tests/
    ├── Quraan.UnitTests/                # Services & domain (Moq, no DB)
    ├── Quraan.IntegrationTests/         # WebApplicationFactory + Testcontainers.MsSql
    └── Quraan.ContractTests/            # OpenAPI snapshot + endpoint shape tests

frontend/
├── angular.json
├── package.json
├── src/
│   ├── app/
│   │   ├── core/                        # HTTP interceptors, auth guard, error handler
│   │   │   ├── auth/
│   │   │   ├── http/                    # AuthInterceptor, CorrelationIdInterceptor, ProblemDetailsInterceptor
│   │   │   ├── i18n/                    # LanguageService, RtlService
│   │   │   └── theme/
│   │   ├── shared/                      # Reusable dumb components, pipes (ArabicNumberPipe), directives
│   │   ├── features/
│   │   │   ├── quran/                   # SurahListPage, SurahReaderPage, AyahComponent
│   │   │   ├── search/                  # SearchPage, SearchResultsList
│   │   │   ├── audio/                   # AudioPlayer, PlayerService (howler.js)
│   │   │   ├── tafsir/                  # TafsirPanel, TafsirService
│   │   │   ├── bookmarks/               # BookmarksPage, BookmarkButton
│   │   │   ├── auth/                    # SignInPage, RegisterPage, AuthService
│   │   │   └── home/                    # HomePage with Continue-Reading affordance
│   │   ├── api/                         # Auto-generated client from openapi.yaml (nswag)
│   │   ├── app.component.ts
│   │   ├── app.config.ts
│   │   └── app.routes.ts                # All feature routes lazy-loaded
│   ├── assets/
│   │   ├── i18n/{ar.json,en.json}
│   │   └── fonts/                       # KFGQPC Uthmanic Hafs (licensed) + UI font
│   ├── styles/                          # _tokens.scss, _rtl.scss (logical properties)
│   └── index.html
└── tests/
    ├── unit/                            # Jasmine specs colocated under src/app are primary
    └── e2e/                             # Playwright specs per user story
```

**Structure Decision**: Web application layout with **clearly separated**
backend (`backend/`) and frontend (`frontend/`) trees per Constitution
"Solution layout" guidance. Backend uses Clean Architecture in 4 projects
(`Api → Application → Domain` and `Infrastructure → Domain`, `Application` →
`Domain` only) so domain entities can be unit-tested without ASP.NET or EF
Core. Frontend uses Angular standalone-component-style routes with each
`features/*` directory lazy-loaded via `loadChildren` in `app.routes.ts`.
Auto-generated TypeScript API client lives in `frontend/src/app/api/` and is
regenerated from the backend's `swagger.json` on each backend change to keep
the API contract a single source of truth (Constitution VII).

## Complexity Tracking

> No Constitution Check violations. Three deliberate scope/architecture
> deferrals are recorded for transparency; each is a knowing, bounded
> divergence that does not weaken any Principle.

| Deferral | Why Needed | Simpler Alternative Rejected Because |
|----------|------------|--------------------------------------|
| `/api/v1/surahs` returns an unpaginated 114-item array (Principle VI says "All list endpoints MUST be paginated"). | The Surah list is fixed at exactly 114 rows for the foreseeable future of Quranic scholarship; payload is ≈ 14 KB JSON; clients render the full list as a single navigation index. | Adding pagination here forces every client to handle a multi-page UX for a constant-size collection, increasing client complexity and request count without any latency win. The integration test for `/surahs` asserts the array length is exactly 114, so unbounded growth cannot regress this. |
| MVP enforces auth via bare `[Authorize]` rather than `[Authorize(Roles = "User")]` (Principle V mandates RBAC). | MVP scope has only one authenticated role (a regular user); `ScholarEditor` and `Admin` roles ship with the post-MVP Admin Panel (out of MVP scope per spec.md §Out of Scope). | Seeding a single-role RBAC scaffold now would create dead code; the role check is added in the same PR that introduces the second role. Auth-guard tests still verify anonymous → 401 on every protected endpoint. |
| Frontend feature modules `hadith/`, `madhahib/`, `azkar/`, `admin/` listed in Constitution §Solution layout are absent from MVP. | spec.md §Out of Scope explicitly defers these content domains for a later release; introducing empty modules now would clutter the workspace and skew bundle budgets. | Reintroduction is a single `ng generate` per module; the Angular routing table already reserves the path namespaces by convention. |
