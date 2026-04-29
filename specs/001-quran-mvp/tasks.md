---
description: "Task list for 001-quran-mvp implementation"
---

# Tasks: Quran & Islamic Companion Web App (MVP)

**Input**: Design documents from `/specs/001-quran-mvp/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/openapi.yaml, quickstart.md

**Tests**: INCLUDED. Constitution Principle III mandates test-first; plan.md prescribes contract + integration + unit + Jasmine + Playwright + axe-core suites.

**Organization**: Tasks are grouped by user story (P1 → P3). Phase 2 (Foundational) is shared and blocks all stories.

## Format: `[ID] [P?] [Story?] Description with file path`

- **[P]**: Different files, no dependency on incomplete tasks → safe to parallelize
- **[Story]**: User-story label (US1–US7); omitted on Setup, Foundational, and Polish phases

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Repository scaffolding, dependency installation, and tooling configuration.

- [X] T001 Create backend solution `backend/Quraan.Backend.slnx` with four empty projects: `backend/src/Quraan.Api/Quraan.Api.csproj` (web), `backend/src/Quraan.Application/Quraan.Application.csproj` (classlib), `backend/src/Quraan.Domain/Quraan.Domain.csproj` (classlib), `backend/src/Quraan.Infrastructure/Quraan.Infrastructure.csproj` (classlib) — `.NET SDK 10.0.201` produced the new `.slnx` solution format; all four projects pinned to `net8.0` per Constitution V & plan
- [X] T002 Wire backend project references in the four `.csproj` files: `Application → Domain`, `Infrastructure → Domain`, `Api → Application + Infrastructure` (Domain depends on nothing)
- [X] T003 [P] Create backend test projects `backend/tests/Quraan.UnitTests/Quraan.UnitTests.csproj`, `backend/tests/Quraan.IntegrationTests/Quraan.IntegrationTests.csproj`, `backend/tests/Quraan.ContractTests/Quraan.ContractTests.csproj` and add them to `backend/Quraan.Backend.slnx`
- [X] T004 [P] Add NuGet dependencies per plan.md: `Microsoft.AspNetCore.OpenApi`, `Swashbuckle.AspNetCore`, `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `Konscious.Security.Cryptography.Argon2`, `FluentValidation.AspNetCore`, `Serilog.AspNetCore`, `StackExchange.Redis`, `Microsoft.Extensions.Caching.StackExchangeRedis` in the appropriate projects
- [X] T005 [P] Add backend test NuGets `xunit`, `FluentAssertions`, `Moq`, `Microsoft.AspNetCore.Mvc.Testing`, `Testcontainers.MsSql` (Integration only), `Verify.Xunit` (Contract only) in `backend/tests/Quraan.UnitTests/Quraan.UnitTests.csproj`, `backend/tests/Quraan.IntegrationTests/Quraan.IntegrationTests.csproj`, `backend/tests/Quraan.ContractTests/Quraan.ContractTests.csproj`
- [X] T006 [P] Add `backend/.editorconfig` and `backend/Directory.Build.props` enabling nullable refs, `TreatWarningsAsErrors`, and `Microsoft.CodeAnalysis.NetAnalyzers` for all backend projects (Tests projects exempted from `TreatWarningsAsErrors` via condition on `*Tests` project name)
- [X] T007 Generate Angular workspace at `frontend/` (`ng new frontend --routing --style=scss --strict --standalone --skip-git`); commit `frontend/angular.json`, `frontend/package.json`, `frontend/tsconfig*.json`. **Note**: scaffolded with installed Angular CLI 20.2.0 (latest stable, per Constitution §Tech Stack "Angular (latest stable major)") rather than the v18 named in earlier tasks.md drafts.
- [X] T008 [P] Install frontend runtime dependencies in `frontend/package.json`: `@angular/material@^20`, `@angular/cdk@^20`, `@ngx-translate/core`, `@ngx-translate/http-loader`, `howler`, `@types/howler`. **Deviation**: `ngx-virtual-scroller` skipped (unmaintained; last published years ago, no Angular 20 peer support). Virtual scrolling will use `@angular/cdk/scrolling`'s `<cdk-virtual-scroll-viewport>` (already in `@angular/cdk`) — to be wired into `surah-reader.page.ts` in T089.
- [X] T009 [P] Install frontend dev dependencies in `frontend/package.json`: `nswag`, `@playwright/test`, `axe-core`, `@axe-core/playwright`, `eslint@^9`, `angular-eslint@^20`, `typescript-eslint@^8`, `stylelint`, `stylelint-config-recommended-scss`, `postcss-scss`, `stylelint-use-logical`, `prettier` (eslint pinned to v9 because angular-eslint v20 peers `eslint@^8.57 || ^9`).
- [X] T010 [P] Add `frontend/eslint.config.js` (flat config — angular-eslint v20 default) with `@angular-eslint` recommended rules + accessibility rules and `frontend/.stylelintrc.json` with `stylelint-use-logical` enforcing logical properties (Principle IV).
- [X] T010a [P] Implement custom ESLint rule `frontend/eslint-rules/no-literal-template-strings.js` (Latin + Arabic letter detection in template text nodes), wire it in `frontend/eslint.config.js` with `severity: error`, and add passing + failing fixtures under `frontend/eslint-rules/__tests__/no-literal-template-strings.test.js`. Wired in Setup so US1 PRs are gated from day one (Principle IV; resolves analyzer M5).
- [X] T011 [P] Add bundle-size budgets to `frontend/angular.json` (`production` configuration): initial 600 kB warn / 750 kB error raw — calibrated so ≈ 250 kB gzipped is the error threshold (Principle VI). Lazy-bundle budget will be added in US1's first `loadChildren` route (T085) once a `lazy` chunk exists in the build graph.
- [X] T012 [P] Create scaffold folders at `frontend/src/app/core/{auth,http,i18n,theme}/`, `frontend/src/app/shared/`, `frontend/src/app/features/{quran,search,audio,tafsir,bookmarks,auth,home}/`, `frontend/src/app/api/`, `frontend/src/assets/i18n/`, `frontend/src/assets/fonts/`, `frontend/src/styles/` with `.gitkeep`
- [X] T013 [P] Create `frontend/tests/e2e/` Playwright config `frontend/playwright.config.ts` with axe-core integration and one placeholder spec (`smoke.placeholder.spec.ts`); projects defined for `chromium-en`, `chromium-ar`, `mobile-chromium-slow3g` (helper-driven throttling per spec.md §Network performance baseline), `webkit`.
- [X] T014 [P] Add root `.gitignore` entries for `bin/`, `obj/`, `node_modules/`, `dist/`, `.angular/`, `*.user`, `appsettings.Development.json` user-secrets noise — already comprehensive in committed root `.gitignore`; no changes needed.
- [X] T015 [P] Create CI workflow file `.github/workflows/ci.yml` with three jobs (backend: dotnet test all 3 suites; frontend: npm test + ng build with budgets + npm run e2e + axe; contract drift: compare generated `swagger.json` to `specs/001-quran-mvp/contracts/openapi.yaml` — drift step is wired-up placeholder until T056 implements Swashbuckle file emission).

**Checkpoint**: Solution and workspace compile empty; `dotnet build` and `ng build` both succeed.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Cross-cutting infrastructure every user story depends on — DbContext, all EF entity configurations + the single initial migration, Identity, JWT auth, middleware pipeline, distributed cache, OpenAPI generation, content seeders, Angular shell + interceptors + i18n bootstrap.

**CRITICAL**: No user-story phase may start before this phase completes.

### Domain entities (data-model.md)

- [X] T016 [P] Create `backend/src/Quraan.Domain/Entities/Surah.cs` with properties from data-model.md §Surah
- [X] T017 [P] Create `backend/src/Quraan.Domain/Entities/Ayah.cs` with properties from data-model.md §Ayah
- [X] T018 [P] Create `backend/src/Quraan.Domain/Entities/Translation.cs` and `backend/src/Quraan.Domain/Entities/AyahTranslation.cs` per data-model.md §Translation/§AyahTranslation
- [X] T019 [P] Create `backend/src/Quraan.Domain/Entities/TafsirSource.cs` and `backend/src/Quraan.Domain/Entities/TafsirEntry.cs` per data-model.md §TafsirSource/§TafsirEntry
- [X] T020 [P] Create `backend/src/Quraan.Domain/Entities/Reciter.cs` per data-model.md §Reciter
- [X] T021 [P] Create `backend/src/Quraan.Domain/Entities/RefreshToken.cs` per data-model.md §RefreshToken
- [X] T022 [P] Create `backend/src/Quraan.Domain/Entities/Bookmark.cs` per data-model.md §Bookmark with `IsDeleted`/`DeletedAt` soft-delete fields
- [X] T023 [P] Create `backend/src/Quraan.Domain/Entities/LastReadPosition.cs` per data-model.md §LastReadPosition with `[Timestamp]` rowversion
- [X] T024 [P] Create `backend/src/Quraan.Domain/Common/RevelationPlace.cs` enum and `backend/src/Quraan.Domain/Common/Result.cs` `Result<T>` discriminated-union helper
- [X] T025 [P] Define repository interfaces `backend/src/Quraan.Domain/Repositories/{ISurahRepository,IAyahRepository,ITafsirRepository,IBookmarkRepository,ILastReadRepository,IRefreshTokenRepository}.cs`

### Identity + Auth foundation

- [X] T026 [P] Create `backend/src/Quraan.Infrastructure/Identity/ApplicationUser.cs` inheriting `IdentityUser<Guid>` with `DisplayName`, `PreferredLanguage`, `CreatedAt`, `LastSignInAt`
- [X] T027 [P] Create `backend/src/Quraan.Infrastructure/Identity/Argon2idPasswordHasher.cs` implementing `IPasswordHasher<ApplicationUser>` using `Konscious.Security.Cryptography.Argon2` (memory 19 MiB, iterations 2, parallelism 1, 32-byte salt + hash) per R-06

### EF Core persistence

- [X] T028 Create `backend/src/Quraan.Infrastructure/Persistence/QuraanDbContext.cs` extending `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>` with DbSets for every domain entity and `OnModelCreating` calling `ApplyConfigurationsFromAssembly(typeof(QuraanDbContext).Assembly)`
- [X] T029 [P] Create `backend/src/Quraan.Infrastructure/Persistence/Configurations/SurahConfiguration.cs` (PK, indexes `IX_Surah_EnglishNameNormalized`, `IX_Surah_TransliteratedName`)
- [X] T030 [P] Create `backend/src/Quraan.Infrastructure/Persistence/Configurations/AyahConfiguration.cs` (FK to Surah RESTRICT, unique `IX_Ayah_SurahId_NumberInSurah`, `IX_Ayah_NormalizedArabicText`)
- [X] T031 [P] Create `backend/src/Quraan.Infrastructure/Persistence/Configurations/TranslationConfiguration.cs` and `AyahTranslationConfiguration.cs` (composite PK + `IX_AyahTranslation_NormalizedText`)
- [X] T032 [P] Create `backend/src/Quraan.Infrastructure/Persistence/Configurations/TafsirSourceConfiguration.cs` and `TafsirEntryConfiguration.cs` (`UQ_TafsirEntry_Source_Ayah`)
- [X] T033 [P] Create `backend/src/Quraan.Infrastructure/Persistence/Configurations/ReciterConfiguration.cs`
- [X] T034 [P] Create `backend/src/Quraan.Infrastructure/Persistence/Configurations/RefreshTokenConfiguration.cs` (`IX_RefreshToken_UserId`, `UQ_RefreshToken_TokenHash`, soft-delete query filter)
- [X] T035 [P] Create `backend/src/Quraan.Infrastructure/Persistence/Configurations/BookmarkConfiguration.cs` (`UQ_Bookmark_User_Ayah` filtered on `IsDeleted = 0`, soft-delete query filter, cascade from User)
- [X] T036 [P] Create `backend/src/Quraan.Infrastructure/Persistence/Configurations/LastReadPositionConfiguration.cs` (PK = UserId, rowversion concurrency token, cascade)
- [X] T037 [P] Add SQL Server full-text catalogue migration helper `backend/src/Quraan.Infrastructure/Persistence/Migrations/FullTextCatalogue.cs` per R-05
- [X] T038 Generate initial migration `M001_Initial` via `dotnet ef migrations add M001_Initial -p backend/src/Quraan.Infrastructure -s backend/src/Quraan.Api` covering every entity above and producing `backend/src/Quraan.Infrastructure/Persistence/Migrations/*.cs`

### Repository implementations

- [X] T039 [P] Implement `backend/src/Quraan.Infrastructure/Repositories/SurahRepository.cs`, `AyahRepository.cs`, `TafsirRepository.cs` using EF projections + `AsNoTracking` (Principle VI)
- [X] T040 [P] Implement `backend/src/Quraan.Infrastructure/Repositories/BookmarkRepository.cs`, `LastReadRepository.cs`, `RefreshTokenRepository.cs`

### Application common + caching

- [X] T041 [P] Create `backend/src/Quraan.Application/Common/PageRequest.cs`, `PageResult.cs`, `Result.cs` per plan.md §Quraan.Application/Common
- [X] T042 [P] Create `backend/src/Quraan.Application/Caching/CacheKeyFactory.cs` (`quraan:v1:` prefix per R-07) and `ICachedReader.cs` + `CachedReader.cs` decorator wrapping `IDistributedCache` with TTL helpers (24 h content, 15 min search)
- [X] T043 [P] Create `backend/src/Quraan.Application/Search/ArabicNormalizer.cs` implementing the 7-step normalization pipeline per R-05 (will be consumed by both seeder and search)

### Seeders + content integrity

- [X] T044 Pin source files under `backend/src/Quraan.Infrastructure/Seed/Sources/`: `quran-uthmani.txt`, `en.sahih.txt`, `en-tafisr-ibn-kathir.json`, and write `backend/src/Quraan.Infrastructure/Seed/Sources/expected.sha256` containing the SHA-256 of each file
- [X] T045 Implement `backend/src/Quraan.Infrastructure/Seed/ChecksumVerifier.cs` that recomputes SHA-256 of every `Sources/*` file and aborts on mismatch (Principle I, SC-006)
- [X] T046 [P] Implement `backend/src/Quraan.Infrastructure/Seed/QuranSeeder.cs` parsing `quran-uthmani.txt` + `en.sahih.txt` and inserting `Surah`, `Ayah`, `Translation`, `AyahTranslation` rows; populate `Ayah.NormalizedArabicText` via `ArabicNormalizer`
- [X] T047 [P] Implement `backend/src/Quraan.Infrastructure/Seed/TafsirSeeder.cs` parsing `en-tafisr-ibn-kathir.json` and inserting `TafsirSource` (id=1) + `TafsirEntry` rows
- [X] T048 [P] Implement `backend/src/Quraan.Infrastructure/Seed/ReciterSeeder.cs` inserting Alafasy reciter (id=1, codes per R-04)

### API host + middleware + DI

- [X] T049 Configure `backend/src/Quraan.Api/Program.cs`: bind config, register `QuraanDbContext` (SQL Server), register Identity + custom Argon2id hasher, register JWT bearer (15-min access token, RFC R-06 settings), register `IDistributedCache` (Redis prod / Memory dev), register Serilog with PII-stripping enricher, register Swashbuckle, register all repositories and services via DI, add controllers, add CORS for `http://localhost:4200`, HTTPS redirection + HSTS
- [X] T050 [P] Implement `backend/src/Quraan.Api/Middleware/CorrelationIdMiddleware.cs` reading/issuing `X-Correlation-ID` header
- [X] T051 [P] Implement `backend/src/Quraan.Api/Middleware/RequestLoggingMiddleware.cs` (Serilog enrichers; never logs `Authorization` or request body for `/auth/*`)
- [X] T052 [P] Implement `backend/src/Quraan.Api/Middleware/ProblemDetailsExceptionHandler.cs` translating exceptions to RFC 7807 responses with `correlationId` field (Principle VII)
- [X] T053 [P] Implement `backend/src/Quraan.Api/Filters/ApiVersionRoute.cs` so all controllers use `[Route("api/v1/[controller]")]`
- [X] T054 [P] Add `backend/src/Quraan.Api/Controllers/V1/HealthController.cs` exposing `/health/live` and `/health/ready` with `AddDbContextCheck` + `AddRedis`
- [X] T055 [P] Add `dotnet run -- seed` and `dotnet run -- verify-content` CLI command parsing in `backend/src/Quraan.Api/Program.cs` invoking seeders and `ChecksumVerifier`
- [X] T055a Implement boot-time integrity gate: in `backend/src/Quraan.Api/Program.cs`, after `app.Build()` and before `app.Run()`, invoke `ChecksumVerifier.VerifyAllAsync()` against the live DB rows; on any digest mismatch log a critical event and `Environment.Exit(1)` so Kestrel never begins listening on a corrupted content store. Implements Principle I "fail closed on every read path" by transitively gating every served request.
- [X] T055b [P] Integration test `backend/tests/Quraan.IntegrationTests/Integrity/BootTimeIntegrityTests.cs` that spins up `WebApplicationFactory<Program>` with a deliberately-mutated Ayah row and asserts the host fails to start (Principle I + Principle III; resolves analyzer C3).

### OpenAPI contract generation

- [X] T056 Configure Swashbuckle in `backend/src/Quraan.Api/Program.cs` to emit OpenAPI 3.0 with bearer security scheme, then add an MSBuild `AfterBuild` target in `backend/src/Quraan.Api/Quraan.Api.csproj` running `dotnet swagger tofile --output ../../../specs/001-quran-mvp/contracts/swagger.generated.yaml`
- [X] T057 Add `npm run gen:api` script in `frontend/package.json` running `nswag run nswag.json`; create `frontend/nswag.json` consuming `../specs/001-quran-mvp/contracts/openapi.yaml` and emitting `frontend/src/app/api/quraan-api.client.ts` per R-10

### Frontend shell

- [X] T058 Configure `frontend/src/app/app.config.ts` with `provideRouter`, `provideHttpClient(withInterceptors([...]))`, `provideAnimations()`, `TranslateModule.forRoot` with HTTP loader pointing to `assets/i18n/`
- [X] T059 [P] Implement `frontend/src/app/core/i18n/language.service.ts` exposing `currentLang$: BehaviorSubject<'ar'|'en'>` and `setLanguage(lang)` writing to `localStorage` (R-08); default per `navigator.language`
- [X] T060 [P] Implement `frontend/src/app/core/i18n/rtl.service.ts` subscribing to `language.service` and toggling `document.documentElement.dir`
- [X] T061 [P] Stub `frontend/src/assets/i18n/ar.json` and `frontend/src/assets/i18n/en.json` with shell keys (`app.title`, `nav.surahs`, `nav.search`, `nav.bookmarks`, `nav.signin`, `common.loading`, `common.retry`)
- [X] T062 [P] Implement `frontend/src/app/core/http/auth.interceptor.ts` attaching `Authorization: Bearer` from a `TokenStore` and triggering `/auth/refresh` on 401
- [X] T063 [P] Implement `frontend/src/app/core/http/correlation-id.interceptor.ts` and `frontend/src/app/core/http/problem-details.interceptor.ts` parsing RFC 7807 bodies into a typed `ApiError`
- [X] T064 [P] Set up `frontend/src/styles/_tokens.scss` (WCAG-AA contrast tokens) and `frontend/src/styles/_rtl.scss` (logical-property utilities) referenced from `frontend/src/styles.scss`
- [X] T065 [P] Add KFGQPC Uthmanic Hafs font files to `frontend/src/assets/fonts/` and `@font-face` declaration in `frontend/src/styles/_typography.scss`
- [X] T066 [P] Configure `frontend/src/app/app.routes.ts` with empty `loadChildren` lazy routes for `quran`, `search`, `tafsir`, `audio`, `bookmarks`, `auth`, `home` features
- [X] T067 [P] Add `frontend/tools/check-contrast.mjs` Node script asserting AA ratios in `_tokens.scss` (CI gate per R-11)

### Foundational tests

- [X] T068 [P] Add `backend/tests/Quraan.IntegrationTests/Fixtures/SqlServerFixture.cs` using `Testcontainers.MsSql` and `WebApplicationFactory<Program>` with seeded test data (Principle III: real DB, never InMemory)
- [X] T069 [P] Add `backend/tests/Quraan.ContractTests/OpenApiSnapshotTests.cs` using Verify.Xunit to compare runtime-generated swagger against `specs/001-quran-mvp/contracts/openapi.yaml`
- [X] T070 [P] Add `backend/tests/Quraan.UnitTests/Search/ArabicNormalizerTests.cs` covering all 7 normalization rules per R-05
- [X] T071 [P] Add `backend/tests/Quraan.IntegrationTests/Seed/SeederTests.cs` asserting 114 Surahs, 6,236 Ayahs, ≥6,000 TafsirEntries, and `ChecksumVerifier.Verify()` passes (SC-006)
- [X] T071a [P] Contract test `backend/tests/Quraan.ContractTests/HealthContractTests.cs` covering `GET /api/v1/health/live` (200) and `GET /api/v1/health/ready` (200 happy / 503 when DbContextCheck or RedisCheck fails), snapshotted against `specs/001-quran-mvp/contracts/openapi.yaml` (Principle III non-negotiable; resolves analyzer C1).

**Checkpoint**: `dotnet test` (all 3 suites) green. `npm test` green. `dotnet run -- seed` populates a clean DB. User-story phases may now begin in parallel.

---

## Phase 3: User Story 1 — Read the Quran in Arabic with English Translation (Priority: P1) 🎯 MVP

**Goal**: Anonymous visitors can browse all 114 Surahs and read each Surah's Ayahs side-by-side in Arabic Uthmani + Saheeh International English. Includes prev/next Surah navigation and jump-to-Ayah.

**Independent Test**: Per spec.md US1 — open Surah list (114 entries), open Al-Fatiha (7 Ayahs render bilingually), use "next Surah" to land at Al-Baqarah Ayah 1, jump to Ayah 255 in Al-Baqarah and confirm scroll target.

### Tests for User Story 1

- [X] T072 [P] [US1] Contract test for `GET /surahs` and `GET /surahs/{id}` matching `specs/001-quran-mvp/contracts/openapi.yaml` schemas in `backend/tests/Quraan.ContractTests/SurahsContractTests.cs`
- [X] T073 [P] [US1] Integration test for Surah list (`GET /api/v1/surahs` returns 114 items, ordered, with `arabicName`/`transliteratedName`/`englishName`/`revelationPlace`/`ayahCount`) in `backend/tests/Quraan.IntegrationTests/Surahs/SurahsListTests.cs`
- [X] T074 [P] [US1] Integration test for Al-Fatiha detail (`GET /api/v1/surahs/1?translation=en.sahih` returns 7 Ayahs with Arabic + translation + attribution) in `backend/tests/Quraan.IntegrationTests/Surahs/SurahDetailTests.cs`
- [X] T075 [P] [US1] Integration test for `GET /api/v1/surahs/115` returns 404 ProblemDetails in `backend/tests/Quraan.IntegrationTests/Surahs/SurahNotFoundTests.cs`
- [X] T076 [P] [US1] Unit test `backend/tests/Quraan.UnitTests/Surahs/SurahServiceTests.cs` (Moq for `ISurahRepository` + `ICachedReader`)
- [X] T077 [P] [US1] Frontend unit test `frontend/src/app/features/quran/surah-list.page.spec.ts` (Jasmine) verifying the list renders 114 mocked items and binds Arabic + English names
- [X] T078 [P] [US1] Frontend unit test `frontend/src/app/features/quran/surah-reader.page.spec.ts` verifying jump-to-Ayah scrolls and prev/next navigation triggers route change
- [X] T079 [P] [US1] Playwright e2e `frontend/tests/e2e/us1-read-quran.spec.ts` covering the four scenarios from spec.md US1; throttle the page context to the `Slow 3G` profile per spec.md §Network performance baseline; assert the translation `<span>` renders with `lang="en"` while sibling Arabic text renders with `lang="ar"` and the `mushaf-font` class regardless of UI locale (FR-009 verification); end with axe-core scan.

### Implementation for User Story 1

- [X] T080 [P] [US1] Create DTOs `backend/src/Quraan.Application/Surahs/SurahSummaryDto.cs`, `SurahDetailDto.cs`, `AyahDto.cs`, `TranslationInfoDto.cs` matching `SurahSummary`/`SurahDetail`/`Ayah`/`TranslationInfo` schemas
- [X] T081 [US1] Implement `backend/src/Quraan.Application/Surahs/SurahService.cs` with `GetAllAsync()` and `GetByIdAsync(int, string translation)` returning DTOs via `ICachedReader` (24-h TTL)
- [X] T082 [US1] Implement `backend/src/Quraan.Application/Ayahs/AyahService.cs` with `GetAsync(int surahId, int numberInSurah, string translation)` for `GET /surahs/{id}/ayahs/{n}`
- [X] T083 [US1] Implement `backend/src/Quraan.Api/Controllers/V1/SurahsController.cs` exposing `GET /api/v1/surahs` and `GET /api/v1/surahs/{surahId}` per OpenAPI; thin pass-through to `SurahService`
- [X] T084 [US1] Implement `backend/src/Quraan.Api/Controllers/V1/AyahsController.cs` exposing `GET /api/v1/surahs/{surahId}/ayahs/{numberInSurah}`
- [X] T085 [P] [US1] Create Angular feature module entry `frontend/src/app/features/quran/quran.routes.ts` with two lazy routes (`''` → SurahListPage, `':surahId'` → SurahReaderPage)
- [X] T086 [P] [US1] Implement `frontend/src/app/features/quran/quran-api.service.ts` wrapping the generated `QuraanApiClient` for `getSurahs()`/`getSurah(id, translation)`/`getAyah(...)`
- [X] T087 [P] [US1] Implement `frontend/src/app/features/quran/surah-list.page.ts` + `.html` + `.scss` rendering 114 Surahs with Arabic + transliterated + English names + Ayah count, mobile-first responsive, KFGQPC font for Arabic
- [X] T088 [US1] Implement `frontend/src/app/features/quran/ayah.component.ts` (presentational) showing Arabic Uthmani + English translation in a two-column layout that flips with `dir`
- [X] T089 [US1] Implement `frontend/src/app/features/quran/surah-reader.page.ts` rendering Surah header (Arabic + transliterated + English name, revelation place, Ayah count), prev/next navigation buttons, jump-to-Ayah input, and a virtualized list of `AyahComponent` via `@angular/cdk/scrolling` (per Phase 1 deviation note on T008)
- [X] T090 [US1] Implement `frontend/src/app/features/home/home.page.ts` with a "Surahs" link as the entry point (Continue-Reading affordance is added in Polish — T140)
- [X] T091 [US1] Wire `frontend/src/app/app.routes.ts` to load the `home` route at `''` and the `quran` feature at `surahs`; update `frontend/src/assets/i18n/{ar,en}.json` with US1 keys (`surahList.title`, `reader.next`, `reader.previous`, `reader.jumpToAyah`, `surah.revelationPlace.meccan`, `surah.revelationPlace.medinan`)

**Checkpoint**: SC-001 verified (Surah open under 1 s on cached reads), SC-006 verified (Ayah text matches `expected.sha256`), all US1 acceptance scenarios pass.

---

## Phase 4: User Story 2 — Use the App in Arabic or English with RTL Support (Priority: P1)

**Goal**: Runtime UI language switch (no full reload) flipping `dir` between RTL and LTR, with persistence (anonymous → localStorage; signed-in → server). Quranic Arabic + English translation always render in their native script regardless of UI locale.

**Independent Test**: Per spec.md US2 — toggle from English (LTR) to Arabic (RTL), confirm chrome strings flip, layout direction flips, no page reload (check network tab), Quran view unaffected; refresh page and confirm persisted choice.

### Tests for User Story 2

- [X] T092 [P] [US2] Frontend unit test `frontend/src/app/core/i18n/language.service.spec.ts` covering `setLanguage('ar')` updates `currentLang$`, persists to localStorage, and survives reload
- [X] T093 [P] [US2] Frontend unit test `frontend/src/app/core/i18n/rtl.service.spec.ts` asserting `document.documentElement.dir` toggles in response to `currentLang$`
- [X] T094 [P] [US2] Contract test for `GET /users/me` and `PATCH /users/me { preferredLanguage }` matching `UserProfile`/`UserProfilePatch` schemas in `backend/tests/Quraan.ContractTests/UsersContractTests.cs`
- [X] T095 [P] [US2] Integration test `backend/tests/Quraan.IntegrationTests/Users/UserProfileTests.cs` for `PATCH /api/v1/users/me { preferredLanguage: "ar" }` persists and survives `GET /me`
- [X] T096 [P] [US2] Playwright e2e `frontend/tests/e2e/us2-bilingual-rtl.spec.ts` covering toggle → RTL flip → no reload → persistence on refresh; **timed assertion**: capture `performance.now()` immediately before the toggle click and again on the next animation frame after `dir` flips, asserting the delta ≤ 500 ms (SC-009); axe-core scans both locales (SC-007). **Audio-continuity clause deferred to US3** (T108) since `AudioPlayerService` does not exist yet.

### Implementation for User Story 2

- [X] T097 [P] [US2] Create DTOs `backend/src/Quraan.Application/Users/UserProfileDto.cs` and `UserProfilePatchDto.cs` matching OpenAPI schemas
- [X] T098 [US2] Implement `backend/src/Quraan.Application/Users/UserProfileService.cs` with `GetMeAsync(Guid userId)` and `PatchMeAsync(Guid userId, UserProfilePatchDto)` updating `ApplicationUser.PreferredLanguage`/`DisplayName` via new `IUserProfileRepository` (Domain) → `UserProfileRepository` (Infrastructure) so Application stays free of Identity references
- [X] T099 [US2] Implement `backend/src/Quraan.Api/Controllers/V1/UsersController.cs` exposing `GET /api/v1/users/me` and `PATCH /api/v1/users/me`, both `[Authorize]`
- [X] T100 [P] [US2] Implement `frontend/src/app/shared/components/language-toggle/language-toggle.component.ts` standalone component placed in the app chrome calling `LanguageService.setLanguage(...)`
- [X] T101 [US2] Translate every existing UI string discovered in US1 to ar.json + en.json; ESLint rule `no-literal-template-strings` was already wired in T010a (Phase 1) and verified clean across all `src/app/**/*.html`
- [X] T102 [US2] Hook left as `LanguageService.registerServerSyncSink(...)` + `applyServerPreference(...)` extension points; the actual `auth.service.ts` wire-up (sign-in sync + PATCH-on-toggle-while-authed) lands with US6 (T159)
- [X] T103 [US2] Verified all US1 components (`surah-list`, `surah-reader`, `ayah`, `home`, `app` shell) use `margin-inline-*`/`padding-inline-*`/`margin-block-*`/`padding-block-*` exclusively — grep for `(margin|padding)-(left|right|top|bottom):` returned zero matches

**Checkpoint**: SC-009 verified (toggle ≤ 500 ms, no reload), SC-007 axe-core passes for both locales.

---

## Phase 5: User Story 3 — Listen to a Surah's Recitation (Priority: P2)

**Goal**: Per-Surah audio streaming from Al Quran Cloud CDN with optional per-Ayah highlight from quran.com timing API; play / pause / resume / error + retry.

**Independent Test**: Per spec.md US3 — open Surah An-Naba, press Play (audio within 2 s — SC-003), confirm highlight advances if timing available, pause / resume preserves position, simulate offline → retry control surfaces.

### Tests for User Story 3

- [X] T104 [P] [US3] Contract test for `GET /audio/{surahId}` matching `AudioRecitation` schema in `backend/tests/Quraan.ContractTests/AudioContractTests.cs`
- [X] T105 [P] [US3] Integration test `backend/tests/Quraan.IntegrationTests/Audio/QuranComClientTests.cs` with in-process stub `HttpMessageHandler` (replaces WireMock — same coverage, no extra dependency) covering: timings present, timings absent (`hasTimings=false, ayahTimings=[]` per FR-013), upstream 503 → API returns 502 ProblemDetails (FR-014)
- [X] T106 [P] [US3] Unit tests `backend/tests/Quraan.UnitTests/Audio/AudioServiceTests.cs` (6 tests) and `AlQuranCloudClientTests.cs` (4 tests) covering URL composition for Alafasy (`https://cdn.islamic.network/quran/audio-surah/128/ar.alafasy/{surahId}.mp3`) and AudioService composition with caching (R-04)
- [X] T107 [P] [US3] Frontend unit test `frontend/src/app/features/audio/audio-player.service.spec.ts` (7 tests) using a `HOWL_FACTORY` injection seam to substitute a fake Howl, covering play/pause/resume, 250 ms poll-driven currentAyah, FR-013 (no highlight without timings), FR-014 error state, and the SC-003 onPlayStarted hook
- [X] T108 [P] [US3] Playwright e2e `frontend/tests/e2e/us3-audio.spec.ts` covering Play / Pause / Resume / 502 → Retry on Surah 78. Audio API is stubbed via `page.route` so timing measurements isolate the client play→onplay path; **timed assertion**: capture `performance.now()` at Play click and again when the Pause button appears (Howl fires `onplay`), asserting the delta ≤ 2,000 ms (SC-003); throttled to Slow 3G.

### Implementation for User Story 3

- [X] T109 [P] [US3] Create DTOs `backend/src/Quraan.Application/Audio/AudioRecitationDto.cs`, `AyahTimingDto.cs`, `ReciterInfoDto.cs` matching `AudioRecitation` schema
- [X] T110 [P] [US3] Create `backend/src/Quraan.Domain/Repositories/IAudioTimingProvider.cs` interface (with `AudioTimingResult` + `AudioAyahTiming` value records and the `UpstreamAudioFailureException` type used by FR-014)
- [X] T111 [P] [US3] Implement `backend/src/Quraan.Infrastructure/ExternalAudio/AlQuranCloudClient.cs` composing the R-04 audio URL. **Deviation note**: no HTTP call is made to Al Quran Cloud server-side (the browser streams MP3 directly from CDN), so the typed-HttpClient + Polly setup specified in the original task is applied to `QuranComClient` only — that's where actual remote fetches happen.
- [X] T112 [P] [US3] Implement `backend/src/Quraan.Infrastructure/ExternalAudio/QuranComClient.cs` fetching `GET /api/v4/recitations/{recitationId}/by_chapter/{chapter}`, mapping `verse_timings` → `AyahTimingDto[]`, returning empty + `hasTimings=false` on 404 / parse-error per FR-013, throwing `UpstreamAudioFailureException` on 5xx / network error / timeout. Registered as a typed `HttpClient` in `Program.cs` with a 5 s timeout and Polly transient-error retry (2 attempts).
- [X] T113 [US3] Implement `backend/src/Quraan.Application/Audio/AudioService.cs` composing `AudioRecitationDto` from `IReciterRepository` + `IAudioUrlBuilder` + `IAudioTimingProvider`; cache the timing array in `IDistributedCache` keyed `quraan:v1:audio:{reciter}:{surah}` for 24 h (R-04, R-07); upstream 5xx surfaces as `UpstreamAudioFailureException` for the controller layer
- [X] T114 [US3] Implement `backend/src/Quraan.Api/Controllers/V1/AudioController.cs` exposing `GET /api/v1/audio/{surahId}?reciter=ar.alafasy` returning `200` / `404` / `502` per OpenAPI; catches `UpstreamAudioFailureException` to map FR-014 → 502 problem+json
- [X] T115 [P] [US3] Implement `frontend/src/app/features/audio/audio-player.service.ts` wrapping `howler.Howl` via a `HOWL_FACTORY` injection seam; exposes signal-based `state`, `currentAyah`, `hasTimings`, `errorMessage`, plus `play(surahId)`, `pause()`, `resume()`, `retry()`. The `currentAyah` derivation runs from a 250 ms `setInterval` while playing per R-09. Also exposes an `onPlayStarted` Subject for the SC-003 e2e timing hook.
- [X] T116 [P] [US3] Implement `frontend/src/app/features/audio/audio-player.component.ts` (Play / Pause / Resume button with `data-testid` for e2e, error banner with Retry, aria-live status announcing the current ayah)
- [X] T117 [US3] Wire `AudioPlayerComponent` into `frontend/src/app/features/quran/surah-reader.page.ts` (mounted above the cdk virtual scroll viewport); `AyahComponent` now takes an `[isPlaying]` input toggled when its `numberInSurah` matches `AudioPlayerService.currentAyah` — no auto-scroll per R-09
- [X] T118 [US3] Add US3 keys to `frontend/src/assets/i18n/{ar,en}.json` (`audio.play`, `audio.pause`, `audio.resume`, `audio.retry`, `audio.ayahLabel`, `audio.error.unreachable`)

**Checkpoint**: SC-003 verified (audio start ≤ 2 s on a typical mobile network), FR-013 verified (no stale highlight without timings), FR-014 verified (Retry surfaces on upstream failure).

---

## Phase 6: User Story 4 — Find a Surah or Ayah by Searching (Priority: P2)

**Goal**: Single search box returning Surah-name matches + Ayah text matches (Arabic and English); diacritic-insensitive Arabic search via `ArabicNormalizer`; case-insensitive English; clicking a result opens the Surah scrolled to the matched Ayah.

**Independent Test**: Per spec.md US4 — type `mercy` (Ayah results), `Yaseen` (Surah Ya-Sin), `الرحمن` (Ayahs containing الرَّحْمَٰنِ), `qwertyuiop` (no-results message). Click a result → Surah opens at the matched Ayah.

### Tests for User Story 4

- [ ] T119 [P] [US4] Contract test for `GET /search` matching `SearchResponse` schema in `backend/tests/Quraan.ContractTests/SearchContractTests.cs`
- [ ] T120 [P] [US4] Integration test `backend/tests/Quraan.IntegrationTests/Search/SearchServiceTests.cs` covering: `q=mercy` returns Ayah hits with `matchedIn=translation` + highlight snippet; `q=Yaseen` returns Surah Ya-Sin; `q=الرحمن` (no diacritics) matches `الرَّحْمَٰنِ` per FR-017; `q=qwertyuiop` returns empty arrays + `totalAyahMatches=0`; pagination respects `pageSize` cap of 100
- [ ] T121 [P] [US4] Unit test `backend/tests/Quraan.UnitTests/Search/SearchServiceTests.cs` for ranking, English lowercasing, query trimming, and 1-char min length validation
- [ ] T122 [P] [US4] Frontend unit test `frontend/src/app/features/search/search.page.spec.ts` covering empty-state, no-results message, and result-click navigation
- [ ] T123 [P] [US4] Playwright e2e `frontend/tests/e2e/us4-search.spec.ts` covering all six queries from US4 acceptance scenarios

### Implementation for User Story 4

- [ ] T124 [P] [US4] Create DTOs `backend/src/Quraan.Application/Search/SearchResponseDto.cs`, `AyahMatchDto.cs` matching OpenAPI `SearchResponse`/`AyahMatch`
- [ ] T125 [US4] Implement `backend/src/Quraan.Application/Search/SearchService.cs`: normalize the query, run a `LIKE %normalized%` against `Surah.EnglishNameNormalized` + `Surah.TransliteratedName` (Surah matches), `Ayah.NormalizedArabicText` (Arabic Ayah matches → `matchedIn=arabic`), `AyahTranslation.NormalizedText` (English Ayah matches → `matchedIn=translation`); produce `highlightSnippet` (60 chars centered on match); cache via `ICachedReader` for 15 min (R-07)
- [ ] T126 [US4] Implement `backend/src/Quraan.Api/Controllers/V1/SearchController.cs` exposing `GET /api/v1/search?q=&page=&pageSize=&translation=` enforcing `pageSize ≤ 100`, returning 400 ProblemDetails for empty `q`
- [ ] T127 [P] [US4] Implement `frontend/src/app/features/search/search.routes.ts` with one lazy `SearchPage` route at `''`
- [ ] T128 [P] [US4] Implement `frontend/src/app/features/search/search.page.ts` + `.html` + `.scss` with debounced input (300 ms), separate Surah-matches and Ayah-matches sections, no-results message, paginated Ayah-matches list
- [ ] T129 [US4] Wire result clicks to navigate to `/surahs/{surahId}` and `surah-reader.page.ts` reads `?ayah={n}` query param to scroll-to-Ayah (extending T089)
- [ ] T130 [US4] Add US4 keys to `frontend/src/assets/i18n/{ar,en}.json` (`search.placeholder`, `search.noResults`, `search.surahMatches`, `search.ayahMatches`, `search.matchedIn.arabic`, `search.matchedIn.translation`)

**Checkpoint**: SC-002 verified (95% of queries return results in under 1 s) — measure on Integration tests.

---

## Phase 7: User Story 5 — Read Tafsir for an Ayah (Priority: P3)

**Goal**: Per-Ayah Tafsir Ibn Kathir panel with attribution; graceful "not available" message for Ayahs without an entry.

**Independent Test**: Per spec.md US5 — open Al-Fatiha, open Tafsir on Ayah 2 (text + "Tafsir Ibn Kathir (Mubarakpuri abridged)" attribution), move to Ayah 3 (panel updates), open Tafsir on an Ayah without a seed entry → "not available" message.

### Tests for User Story 5

- [ ] T131 [P] [US5] Contract test for `GET /tafsir/{surahId}/{numberInSurah}` matching `TafsirEntry` schema, including 404 on missing entry, in `backend/tests/Quraan.ContractTests/TafsirContractTests.cs`
- [ ] T132 [P] [US5] Integration test `backend/tests/Quraan.IntegrationTests/Tafsir/TafsirServiceTests.cs` covering: `GET /tafsir/1/2` returns body + source + attribution; a known-empty Ayah returns 404 ProblemDetails (FR-023)
- [ ] T133 [P] [US5] Frontend unit test `frontend/src/app/features/tafsir/tafsir-panel.component.spec.ts` covering "loaded", "not-available", and "loading" states
- [ ] T134 [P] [US5] Playwright e2e `frontend/tests/e2e/us5-tafsir.spec.ts`

### Implementation for User Story 5

- [ ] T135 [P] [US5] Create DTOs `backend/src/Quraan.Application/Tafsir/TafsirEntryDto.cs`, `TafsirSourceDto.cs` matching OpenAPI schema
- [ ] T136 [US5] Implement `backend/src/Quraan.Application/Tafsir/TafsirService.cs` with `GetForAyahAsync(int surahId, int numberInSurah, string source)` returning DTO or `null`; cache 24 h via `ICachedReader`
- [ ] T137 [US5] Implement `backend/src/Quraan.Api/Controllers/V1/TafsirController.cs` exposing `GET /api/v1/tafsir/{surahId}/{numberInSurah}?source=ibn-kathir-en` returning 200 or 404 ProblemDetails
- [ ] T138 [P] [US5] Implement `frontend/src/app/features/tafsir/tafsir-api.service.ts` wrapping the generated client and `frontend/src/app/features/tafsir/tafsir-panel.component.ts` (drawer / side panel) showing body + attribution or the "not available" message
- [ ] T139 [US5] Wire a Tafsir affordance into `frontend/src/app/features/quran/ayah.component.ts` opening the `TafsirPanelComponent` for the tapped Ayah; navigation between Ayahs while panel open updates content (subscribe to `currentAyah$` of a new `TafsirPanelService`)
- [ ] T140 [US5] Add US5 keys to `frontend/src/assets/i18n/{ar,en}.json` (`tafsir.title`, `tafsir.notAvailable`, `tafsir.attribution`, `tafsir.close`)

**Checkpoint**: FR-021/022/023 satisfied; attribution always rendered (Principle I).

---

## Phase 8: User Story 6 — Create an Account and Sign In (Priority: P3)

**Goal**: Email + password registration + sign-in + session persistence (15-min JWT + rotating refresh cookie); account lockout (5 / 15 min) and per-IP rate limit (10 / min); generic error responses; sign-out revokes refresh token.

**Independent Test**: Per spec.md US6 — register, refresh browser (still signed in), sign out (`/bookmarks` redirects to sign-in), 5 wrong attempts on an email → same generic error for 15 min, 11th attempt from same IP in 60 s → HTTP 429 without password check.

### Tests for User Story 6

- [ ] T141 [P] [US6] Contract test for `POST /auth/register`, `POST /auth/login`, `POST /auth/refresh`, `POST /auth/logout` matching all schemas + `Set-Cookie` header in `backend/tests/Quraan.ContractTests/AuthContractTests.cs`; include 429 ProblemDetails + `Retry-After` header assertion for both `register` and `login` (FR-029b, OpenAPI `Problem429`); include 409 ProblemDetails assertion for register on duplicate email (FR-024a).
- [ ] T142 [P] [US6] Integration test `backend/tests/Quraan.IntegrationTests/Auth/RegisterLoginTests.cs` covering happy path: register → 201 + access token in body + refresh cookie; subsequent `GET /users/me` with bearer succeeds
- [ ] T143 [P] [US6] Integration test `backend/tests/Quraan.IntegrationTests/Auth/InvalidCredentialsTests.cs` asserting wrong password and unknown email both return identical 401 ProblemDetails (FR-029)
- [ ] T144 [P] [US6] Integration test `backend/tests/Quraan.IntegrationTests/Auth/AccountLockoutTests.cs` for FR-029a: 5 wrong attempts → account locked 15 min → 6th attempt during lockout returns the same generic 401 (no lockout disclosure); successful login resets the counter
- [ ] T145 [P] [US6] Integration test `backend/tests/Quraan.IntegrationTests/Auth/IpRateLimitTests.cs` for FR-029b: 11th attempt from same IP within a 60-s window → 429 ProblemDetails before any credential check (asserted by spying on `IPasswordHasher`)
- [ ] T146 [P] [US6] Integration test `backend/tests/Quraan.IntegrationTests/Auth/RefreshRotationTests.cs` covering refresh-token rotation (old hash revoked, new hash active) and reuse detection (presenting an already-rotated token revokes the entire chain — R-06)
- [ ] T147 [P] [US6] Unit test `backend/tests/Quraan.UnitTests/Auth/Argon2idPasswordHasherTests.cs` round-trips a known password and rejects a tampered hash
- [ ] T148 [P] [US6] Unit test `backend/tests/Quraan.UnitTests/Auth/TokenServiceTests.cs` for JWT 15-min lifetime + correct claims + signature verification
- [ ] T149 [P] [US6] Frontend unit test `frontend/src/app/features/auth/auth.service.spec.ts` covering token storage, automatic refresh on 401, and sign-out clearing state
- [ ] T150 [P] [US6] Playwright e2e `frontend/tests/e2e/us6-auth.spec.ts` covering register / sign-in / persistence across browser-context reload / sign-out / lockout banner; assert `quraan-refresh` cookie has `HttpOnly; Secure; SameSite=Strict` flags

### Implementation for User Story 6

- [ ] T151 [P] [US6] Create DTOs `backend/src/Quraan.Application/Auth/RegisterRequestDto.cs`, `LoginRequestDto.cs`, `AuthResponseDto.cs`, `AccessTokenResponseDto.cs` matching OpenAPI
- [ ] T152 [P] [US6] Add `FluentValidation` validators `RegisterRequestValidator.cs` (email format; password ≥ 12 chars; preferredLanguage in `[ar,en]`) and `LoginRequestValidator.cs` in `backend/src/Quraan.Application/Auth/Validators/`
- [ ] T153 [US6] Implement `backend/src/Quraan.Application/Auth/TokenService.cs` issuing JWT (15-min, HMAC-SHA256, claims `sub`/`email`/`name`) and opaque 256-bit refresh tokens (returning the cleartext to the controller and persisting only `SHA-256(token)` per R-06)
- [ ] T154 [US6] Implement `backend/src/Quraan.Infrastructure/Identity/RefreshTokenStore.cs` implementing `IRefreshTokenRepository` with rotation, reuse detection (revoke entire chain), and 30-day expiration cleanup
- [ ] T155 [US6] Implement `backend/src/Quraan.Application/Auth/AuthService.cs` with `RegisterAsync`, `LoginAsync` (uses `SignInManager` for lockout via Identity's `AccessFailedCount` + `LockoutEnd` configured to 5 attempts / 15 min per FR-029a), `RefreshAsync`, `LogoutAsync`; all failure paths return identical generic ProblemDetails (FR-029) so the controller has no branching
- [ ] T156 [US6] Implement IP rate-limit middleware `backend/src/Quraan.Api/Middleware/SignInIpRateLimitMiddleware.cs` using `Microsoft.AspNetCore.RateLimiting` fixed-window policy (10 / minute / IP) applied only to `/api/v1/auth/login` and `/api/v1/auth/register`, returning 429 ProblemDetails before invoking the controller (FR-029b)
- [ ] T157 [US6] Implement `backend/src/Quraan.Api/Controllers/V1/AuthController.cs` with `POST register/login/refresh/logout`; set `quraan-refresh` cookie `HttpOnly; Secure; SameSite=Strict; Path=/api/v1/auth/refresh; Max-Age=2592000`; on logout, revoke + clear cookie
- [ ] T158 [US6] Configure Identity options in `backend/src/Quraan.Api/Program.cs`: `Lockout.MaxFailedAccessAttempts=5`, `Lockout.DefaultLockoutTimeSpan=TimeSpan.FromMinutes(15)`, `Password.RequiredLength=12`, `User.RequireUniqueEmail=true`
- [ ] T159 [P] [US6] Implement `frontend/src/app/features/auth/auth.service.ts` (sign-in/up/out, `accessToken$` BehaviorSubject, automatic refresh on 401 via the existing `auth.interceptor.ts` hook, in-memory access-token storage — never localStorage per R-06)
- [ ] T160 [P] [US6] Implement `frontend/src/app/features/auth/sign-in.page.ts` and `register.page.ts` with reactive forms, FluentValidation-equivalent client validation, and a generic error banner (never reveals lockout / unknown email)
- [ ] T161 [US6] Implement `frontend/src/app/core/auth/auth.guard.ts` and apply it to `bookmarks` and `lastread`-protected routes; unauthenticated → redirect to `/auth/sign-in?returnUrl=…`
- [ ] T162 [US6] Implement `frontend/src/app/features/auth/auth.routes.ts` (`sign-in`, `register`) and wire into `app.routes.ts` lazy loader
- [ ] T163 [US6] Add US6 keys to `frontend/src/assets/i18n/{ar,en}.json` (`auth.signIn.title`, `auth.register.title`, `auth.email`, `auth.password`, `auth.submit`, `auth.error.generic`, `auth.signOut`)

**Checkpoint**: FR-024–029b satisfied; SC-010 (zero plaintext passwords) provable by Serilog enricher tests; both lockout and IP-rate-limit verified by integration tests above.

---

## Phase 9: User Story 7 — Bookmark Ayahs and Revisit Them (Priority: P3)

**Goal**: Signed-in users add / list / remove bookmarks; bookmarks sync across devices; anonymous users see a sign-in prompt instead of silent loss.

**Independent Test**: Per spec.md US7 — sign in, bookmark Al-Mulk:1, navigate elsewhere, open Bookmarks list (entry visible), tap → Surah opens scrolled to Ayah; sign in on a second profile → same bookmark visible (SC-008); anonymous click → sign-in prompt.

### Tests for User Story 7

- [ ] T164 [P] [US7] Contract test for `GET /bookmarks`, `POST /bookmarks`, `DELETE /bookmarks/{id}` matching schemas + 401 / 409 paths in `backend/tests/Quraan.ContractTests/BookmarksContractTests.cs`
- [ ] T165 [P] [US7] Integration test `backend/tests/Quraan.IntegrationTests/Bookmarks/BookmarksCrudTests.cs` covering happy path + duplicate (`409 already-bookmarked`) + delete soft-flips `IsDeleted`
- [ ] T166 [P] [US7] Integration test `backend/tests/Quraan.IntegrationTests/Bookmarks/BookmarkLimitTests.cs` asserting FR-030a — the 1,000-active-bookmarks cap returns `409` with RFC 7807 `type=https://quraan.app/problems/bookmark-limit-reached`, distinct from the generic 409 `already-bookmarked` response.
- [ ] T167 [P] [US7] Integration test `backend/tests/Quraan.IntegrationTests/Bookmarks/AnonymousAccessTests.cs` asserting all `/bookmarks*` endpoints return 401 ProblemDetails when unauthenticated
- [ ] T168 [P] [US7] Frontend unit test `frontend/src/app/features/bookmarks/bookmarks.page.spec.ts`
- [ ] T169 [P] [US7] Playwright e2e `frontend/tests/e2e/us7-bookmarks.spec.ts` covering bookmark / list / open-from-list / cross-device sync (two browser contexts) / anonymous sign-in prompt

### Implementation for User Story 7

- [ ] T170 [P] [US7] Create DTOs `backend/src/Quraan.Application/Bookmarks/BookmarkDto.cs`, `BookmarkPageDto.cs`, `CreateBookmarkRequestDto.cs` matching OpenAPI
- [ ] T171 [US7] Implement `backend/src/Quraan.Application/Bookmarks/BookmarkService.cs` with `ListAsync(userId, page, pageSize)`, `AddAsync(userId, surahId, numberInSurah)` (enforces 1,000 active cap, throws typed `BookmarkLimitReachedException` mapped to 409 with the documented `type` URI), `RemoveAsync(userId, bookmarkId)` (soft delete)
- [ ] T172 [US7] Implement `backend/src/Quraan.Api/Controllers/V1/BookmarksController.cs` `[Authorize]` exposing `GET /api/v1/bookmarks?page=&pageSize=`, `POST /api/v1/bookmarks`, `DELETE /api/v1/bookmarks/{bookmarkId}` per OpenAPI
- [ ] T173 [P] [US7] Implement `frontend/src/app/features/bookmarks/bookmarks-api.service.ts` wrapping the generated client + `frontend/src/app/features/bookmarks/bookmarks.page.ts` (paginated list with Surah / Ayah reference + remove button)
- [ ] T174 [P] [US7] Implement `frontend/src/app/features/bookmarks/bookmark-button.component.ts` standalone component for use inside `AyahComponent`; if anonymous, navigate to `/auth/sign-in?returnUrl=…&pendingBookmark={surahId}:{ayahNumber}` and replay the `POST /bookmarks` after sign-in (FR-034)
- [ ] T175 [US7] Wire `BookmarkButton` into `frontend/src/app/features/quran/ayah.component.ts` next to the existing Tafsir affordance; reflect bookmarked state via a `BookmarksStore` signal seeded from `GET /bookmarks` on sign-in
- [ ] T176 [US7] Add US7 keys to `frontend/src/assets/i18n/{ar,en}.json` (`bookmarks.title`, `bookmarks.empty`, `bookmarks.add`, `bookmarks.remove`, `bookmarks.signInToSave`)

**Checkpoint**: SC-008 verified (bookmark visible on a second device within 5 s — Playwright cross-context); FR-030–034 satisfied.

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: Tasks that span more than one user story or finalize MVP-quality gates.

- [ ] T176a [P] Contract test `backend/tests/Quraan.ContractTests/LastReadContractTests.cs` covering `GET /api/v1/lastread/me` (200 with body, 200 with `null`, 401) and `PUT /api/v1/lastread/me` (200, 400, 401), snapshotted against `specs/001-quran-mvp/contracts/openapi.yaml`. **Must precede T177** (Principle III non-negotiable; resolves analyzer C2).
- [ ] T176b [P] Anonymous-access positive integration test `backend/tests/Quraan.IntegrationTests/AnonymousAccess/AnonymousReadPathsTests.cs` asserting `GET /api/v1/surahs`, `GET /api/v1/surahs/1`, `GET /api/v1/audio/1`, `GET /api/v1/search?q=mercy`, `GET /api/v1/tafsir/1/1` all return 200 with no `Authorization` header (FR-027 positive verification; resolves analyzer M1).
- [ ] T177 Implement last-read tracking (FR-035): `backend/src/Quraan.Application/LastRead/LastReadService.cs`, `backend/src/Quraan.Api/Controllers/V1/LastReadController.cs` (`GET/PUT /api/v1/lastread/me`, `[Authorize]`, last-write-wins on `updatedAt` per data-model.md `rowversion`); `frontend/src/app/core/last-read.service.ts` writing localStorage for anonymous users (R-13), POSTing on sign-in to merge; `frontend/src/app/features/home/home.page.ts` shows "Continue reading Surah X, Ayah Y" affordance
- [ ] T178 [P] Add integration tests `backend/tests/Quraan.IntegrationTests/LastRead/LastReadTests.cs` covering anonymous-on-sign-in merge (server keeps the more-recent of local vs. server `updatedAt`)
- [ ] T179 [P] Add Playwright e2e `frontend/tests/e2e/last-read.spec.ts` covering anonymous → sign-in merge and home Continue-Reading affordance
- [ ] T180 [P] Wire Serilog PII-stripping enricher `backend/src/Quraan.Api/Logging/PiiStrippingEnricher.cs` (redacts `Authorization`, `Cookie`, `password`, `email` keys); add unit test `backend/tests/Quraan.UnitTests/Logging/PiiStrippingEnricherTests.cs` proving SC-010 (no plaintext passwords / emails in logs)
- [ ] T181 [P] Add CI gate `tools/check-contrast.mjs` invocation to `.github/workflows/ci.yml` and run Stylelint logical-property rule + ESLint a11y rules
- [ ] T182 [P] Performance smoke `backend/tests/Quraan.IntegrationTests/Performance/ContentReadPerfTests.cs` asserting p95 ≤ 300 ms across 100 cached `GET /surahs/1` calls (Principle VI / SC-001)
- [ ] T182a [P] Performance smoke `backend/tests/Quraan.IntegrationTests/Performance/SearchLatencyTests.cs` asserting p95 ≤ 1,000 ms across 100 mixed `GET /api/v1/search` calls — query mix: English word (`mercy`), Arabic with diacritics (`الرَّحْمَٰنِ`), Arabic without diacritics (`الرحمن`), Surah-name (`Yaseen`), no-results (`qwertyuiop`); each query exercised both cache-hit and cache-cold (SC-002).
- [ ] T183 [P] Add bundle-size CI step in `.github/workflows/ci.yml` running `ng build --configuration production` and asserting `angular.json` budgets (initial 250 KB / lazy 500 KB) — Principle VI
- [ ] T184 [P] Verify keyboard-only operability + screen-reader narration for both locales; document the manual pass at `frontend/tests/e2e/checklists/a11y.md` (R-11)
- [ ] T185 [P] Confirm `swagger.json` produced by `dotnet build` matches `specs/001-quran-mvp/contracts/openapi.yaml` exactly; CI step in `.github/workflows/ci.yml` fails on drift (R-10, Principle VII)
- [ ] T186 [P] Update `CLAUDE.md` Project Structure / Commands sections to record the actual `dotnet`, `npm`, `ng`, and `dotnet ef` commands used in development (currently has placeholder)
- [ ] T187 Run the full quickstart.md §4 smoke-test sweep manually on localhost; record findings in PR description; fix any blocker uncovered

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)** → no dependencies; start immediately.
- **Phase 2 (Foundational)** → depends on Phase 1; **blocks all user stories**.
- **Phase 3 (US1, P1) 🎯 MVP** → depends on Phase 2; no upstream story dependencies.
- **Phase 4 (US2, P1)** → depends on Phase 2; references existing US1 components for translation coverage but US2 ships independently.
- **Phase 5 (US3, P2)** → depends on Phase 2 (and on US1 for the `SurahReaderPage` mount point — T117).
- **Phase 6 (US4, P2)** → depends on Phase 2 (and on US1 for the navigate-to-Ayah behavior — T129).
- **Phase 7 (US5, P3)** → depends on Phase 2 (and on US1 for the Tafsir affordance mount in `AyahComponent` — T139).
- **Phase 8 (US6, P3)** → depends on Phase 2 only.
- **Phase 9 (US7, P3)** → depends on Phase 8 (auth guard + signed-in state) and on US1 (`AyahComponent` to host `BookmarkButton`).
- **Phase 10 (Polish)** → T176a (lastread contract test) and T176b (anonymous-access test) require only Phase 2 and may run in parallel with later user-story work; **T177 must wait for T176a to fail-then-pass**. T177 also depends on US1 (home + reader) and US6 (auth) for the last-read merge. T182a (search latency) requires US4 implementation. Other polish tasks need only Phase 2.

### Within Each User Story

- Tests MUST be written and FAIL before the corresponding implementation tasks (Principle III).
- DTOs → Services → Controllers (backend); API service → page components (frontend).

### Parallel Opportunities

- All [P]-marked Setup tasks (T003–T015) — different files, no ordering.
- All [P]-marked Foundational tasks: domain entities (T016–T025), EF configurations (T029–T037), repositories (T039–T040), application common (T041–T043), seeders (T046–T048), middleware (T050–T053), interceptors + i18n + theme (T059–T067), foundational tests (T068–T071).
- After Phase 2 completes, with multiple developers, US1, US2, US3, US4, US5, US6 can all run in parallel; US7 starts when US6 + US1 are done.
- Within each story, every [P]-marked test, DTO, and component task can be done concurrently.

---

## Parallel Example: User Story 1

```bash
# Tests — write & fail first (parallel):
Task: T072 Contract test in backend/tests/Quraan.ContractTests/SurahsContractTests.cs
Task: T073 Integration test SurahsListTests.cs
Task: T074 Integration test SurahDetailTests.cs
Task: T075 Integration test SurahNotFoundTests.cs
Task: T076 Unit test SurahServiceTests.cs
Task: T077 Frontend unit test surah-list.page.spec.ts
Task: T078 Frontend unit test surah-reader.page.spec.ts
Task: T079 Playwright e2e us1-read-quran.spec.ts

# Implementation — DTOs + frontend skeleton in parallel, then services, then controllers/pages:
Task: T080 DTOs in backend/src/Quraan.Application/Surahs/
Task: T085 Quran feature routes
Task: T086 quran-api.service.ts
Task: T087 surah-list.page.ts
```

---

## Implementation Strategy

### MVP-First (recommended for first deploy)

1. Phase 1 → Phase 2 → Phase 3 (US1) → Phase 4 (US2). Both stories are P1; the app is now demoable.
2. Stop and validate against quickstart.md §4 US1 + US2.
3. Tag MVP-readable build.

### Incremental Delivery (post-MVP)

1. After US1 + US2: add US3 (audio) → demo.
2. Add US4 (search) → demo.
3. Add US5 (Tafsir) → demo.
4. Add US6 (auth) → demo.
5. Add US7 (bookmarks) → demo (full P3 feature set complete).
6. Polish phase finalizes last-read, performance gates, bundle / contrast / drift checks.

### Parallel Team Strategy

- Whole team finishes Phase 1 + Phase 2 together (foundation is shared).
- After Phase 2: assign US1, US2, US3, US4, US5, US6 to separate developers; US7 starts when US6 + US1 land.
- Polish runs as a final bundling effort once all stories are merged.

---

## Notes

- [P] = different files, no incomplete dependency.
- [Story] = `US1`–`US7`; only present in Phases 3–9.
- Every backend test path is real-DB (Testcontainers.MsSql), never InMemory (Principle III).
- Every Quran-text path is read-only post-seed, attribution-rendered, SHA-256-verified (Principle I).
- Every frontend feature is lazy-loaded via `loadChildren` and respects the bundle budget (Principle VI).
- Every endpoint is under `/api/v1`, errors as `application/problem+json` (Principle VII).
