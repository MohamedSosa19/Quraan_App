<!--
SYNC IMPACT REPORT
==================
Version change: TEMPLATE → 1.0.0 (initial ratification)
Bump rationale: First concrete ratification of the project constitution; no prior
semantic version existed (template placeholders only).

Modified principles:
  - [PRINCIPLE_1_NAME] → I. Sacred Content Integrity (NON-NEGOTIABLE)
  - [PRINCIPLE_2_NAME] → II. Clean Layered Architecture
  - [PRINCIPLE_3_NAME] → III. Test-First Development (NON-NEGOTIABLE)
  - [PRINCIPLE_4_NAME] → IV. Bilingual & Accessible Experience
  - [PRINCIPLE_5_NAME] → V. Security & Privacy by Default
  - (added) VI. Performance, Scalability & Caching
  - (added) VII. Observability & API Contract Discipline

Added sections:
  - Technology Stack & Standards (replaces [SECTION_2_NAME])
  - Development Workflow & Quality Gates (replaces [SECTION_3_NAME])
  - Governance (filled in)

Removed sections: None.

Templates requiring updates:
  - .specify/templates/plan-template.md          ⚠ pending — "Constitution Check"
    section is a placeholder; update to enumerate the seven gates derived from
    Principles I–VII when the next plan is authored.
  - .specify/templates/spec-template.md          ✅ no change required (technology-
    agnostic; principles are enforced at plan/tasks stage).
  - .specify/templates/tasks-template.md         ⚠ pending — sample task list still
    uses Python paths; replace with .NET (backend/src) + Angular (frontend/src)
    paths and add explicit categories for accessibility, localization, and security
    tasks aligned with Principles IV and V.
  - .specify/templates/agent-file-template.md    ✅ no change required.
  - .specify/templates/checklist-template.md     ✅ no change required.
  - .specify/templates/commands/*.md             ✅ directory does not exist; nothing
    to reconcile.

Follow-up TODOs: None deferred.
-->

# Quraan App Constitution

## Core Principles

### I. Sacred Content Integrity (NON-NEGOTIABLE)

The platform serves the Qur'an, authenticated Hadith, and classical Tafsir; this
content is not ordinary application data and MUST be treated as immutable scripture.

- Quranic text MUST be ingested verbatim from a single, citable canonical source
  (e.g., Tanzil/Madinah Mushaf) and stored with checksum verification on every
  read path. Any divergence from the source on load MUST fail closed.
- Translations, Tafsir, and Hadith records MUST carry attribution metadata
  (author, edition, source URL, license) and MUST surface that attribution in the
  UI wherever the content is shown.
- Editorial changes to scripture, Hadith, or Tafsir MUST be performed only via
  the Admin Panel by users with the `ScholarEditor` role, MUST be append-only
  (revision history retained), and MUST be reviewable.
- AI-generated or paraphrased religious interpretation MUST NOT be presented as
  Tafsir or Hadith. AI may assist with search and translation UX, but generated
  output MUST be visually distinguished and never persisted into canonical tables.

**Rationale:** Users place religious trust in the platform; a single corrupted Ayah
or misattributed Hadith is a reputational and ethical failure that no amount of
feature polish can offset.

### II. Clean Layered Architecture

The backend follows a strict Controller → Service → Repository → EF Core layering;
the Angular frontend follows a feature-module structure with a clear smart/dumb
component split.

- Controllers MUST contain no business logic — only request validation, mapping
  to/from DTOs, and delegation to services.
- Services MUST encapsulate domain rules and MUST depend on repository
  abstractions (interfaces), never on `DbContext` directly.
- Repositories MUST own all EF Core query composition; LINQ queries MUST NOT leak
  out of the data layer.
- Domain entities MUST NOT depend on ASP.NET, EF Core, or HTTP types.
- Frontend feature modules (`quran/`, `hadith/`, `tafsir/`, `madhahib/`, `azkar/`,
  `admin/`, `auth/`) MUST be lazy-loaded and MUST own their routes, components,
  services, and stores.
- Cross-cutting code (auth, theming, i18n, HTTP interceptors) lives in `core/`
  and is imported only by the root module.

**Rationale:** A multi-module Islamic content platform will accumulate features
for years; without enforced layering, controllers turn into god-objects and EF
queries scatter across the codebase, blocking testability and performance tuning.

### III. Test-First Development (NON-NEGOTIABLE)

TDD is mandatory for every backend service, every API endpoint, and every Angular
service or non-trivial component.

- Backend tests use xUnit + FluentAssertions; integration tests use
  `WebApplicationFactory` against a real SQL Server LocalDB or SQL container —
  the in-memory provider MUST NOT be used for repository tests because it does
  not honor relational semantics.
- Frontend tests use Jasmine + Karma for unit tests and Playwright (or Cypress)
  for end-to-end flows on Quran reading, audio playback, and authentication.
- Every new HTTP endpoint MUST have a contract test (request/response shape)
  before implementation. Contract test failure blocks merge.
- Red-Green-Refactor: a commit that introduces production code without a
  preceding failing test for that behavior MUST be rejected in review.

**Rationale:** Religious-text correctness, bilingual rendering, and audio sync
have many edge cases (missing Ayahs, RTL+LTR mixing, partial downloads); only
test-first discipline catches regressions before they reach worshipping users.

### IV. Bilingual & Accessible Experience

Arabic and English are first-class peers; accessibility is a release gate, not a
finishing pass.

- Every user-visible string MUST be sourced from i18n resource files (Angular
  i18n / `@ngx-translate`); literal strings in templates or components MUST fail
  CI lint.
- RTL layout MUST be driven by the active locale, not by per-component overrides;
  CSS MUST use logical properties (`margin-inline-start`, `padding-inline-end`).
- Arabic typography MUST use a verified Mushaf-grade font (e.g., KFGQPC Uthmanic
  Hafs) for Quranic Ayahs and a separate UI font for chrome.
- The application MUST meet WCAG 2.1 AA: contrast ratios verified for both light
  and dark themes, all interactive elements keyboard-reachable, audio playback
  controls operable without a mouse, screen-reader labels in both languages.
- Locale switching MUST be dynamic (no full reload) and MUST persist per user.

**Rationale:** Half the target audience reads right-to-left; an LTR-first design
that is "translated later" produces a second-class Arabic experience and excludes
users with assistive technology needs.

### V. Security & Privacy by Default

Authentication, authorization, and data protection are baseline requirements on
every endpoint and every UI route.

- Authentication uses ASP.NET Core Identity with JWT access tokens (short-lived,
  ≤15 min) plus refresh tokens (rotating, revocable, stored hashed).
- Passwords MUST be hashed with Argon2id or PBKDF2 with parameters meeting
  current OWASP guidance; plaintext or reversibly-encrypted passwords MUST NOT
  exist anywhere, including logs and admin tooling.
- Authorization is role-based (`User`, `ScholarEditor`, `Admin`) and enforced at
  the controller via `[Authorize(Roles = ...)]`; service-layer guards MUST also
  re-check sensitive mutations.
- All database access MUST go through EF Core parameterized queries; raw SQL is
  permitted only via reviewed `FromSqlInterpolated` calls.
- HTTPS MUST be enforced (HSTS enabled in production); CORS MUST be allowlisted
  per environment.
- Secrets (JWT signing keys, DB connection strings, audio CDN keys) MUST live in
  user-secrets locally and Azure Key Vault (or equivalent) in deployed
  environments. Secrets MUST NOT be committed.
- PII (email, name, IP) MUST NOT appear in application logs; user identifiers in
  logs use the opaque user GUID.

**Rationale:** A platform holding bookmarks, reading history, and (for
ScholarEditors) editorial credentials is a credible target; weak defaults invite
breaches that damage trust in a religious context.

### VI. Performance, Scalability & Caching

Content read paths are dominant traffic and MUST be fast on mobile networks.

- Content read endpoints (Surah, Ayah list, Hadith chapter, Tafsir for an Ayah)
  MUST meet a p95 latency target of ≤300 ms server-side under nominal load.
- Quran, Hadith, Tafsir, and Madhahib content is effectively immutable and MUST
  be served from a response cache (in-memory + distributed cache for production)
  with explicit cache invalidation on admin edits.
- All list endpoints MUST be paginated; unbounded `ToList()` over content tables
  MUST fail review.
- EF Core queries on hot paths MUST use projection (`Select` to DTO),
  `AsNoTracking` for reads, and explicit `Include` only when needed; N+1
  patterns MUST be caught in integration tests.
- Angular feature modules MUST be lazy-loaded; route bundles MUST stay under 250
  KB gzipped at the time of merge.
- Audio MUST stream via HTTP range requests from a CDN-friendly URL; the client
  MUST NOT download a full Surah before playback begins.

**Rationale:** The user base spans regions with constrained bandwidth; an app
that takes 4 seconds to open Al-Fatiha will not be used regardless of feature
completeness.

### VII. Observability & API Contract Discipline

Production behavior MUST be inspectable, and the public API surface MUST be a
deliberate, versioned contract.

- Structured logging via Serilog with a correlation ID per request, propagated
  to the frontend and surfaced in error toasts for support traceability.
- Health endpoints (`/health/live`, `/health/ready`) MUST report database and
  cache reachability and MUST be wired into the deployment platform's probes.
- OpenAPI/Swagger documentation MUST be auto-generated from controllers and
  DTOs; the spec is the source of truth for the Angular API client and MUST be
  regenerated on each backend change.
- The API is versioned via URL segment (`/api/v1/...`); breaking changes
  (removed fields, changed types, semantic changes) MUST ship under a new
  version (`/api/v2/...`) and MUST NOT modify the existing version in place.
- Every error response MUST follow RFC 7807 (`application/problem+json`) with a
  stable `type` URI; ad-hoc error shapes MUST NOT be introduced.

**Rationale:** A multi-module platform with admin, mobile-web, and (likely)
future native clients cannot afford silent breaking changes or opaque incidents.

## Technology Stack & Standards

The following stack is ratified; substitutions require a constitutional amendment.

- **Backend runtime**: .NET 8 LTS, C# 12, ASP.NET Core Web API.
- **Persistence**: SQL Server 2022 (Azure SQL in production), Entity Framework
  Core 8 with migrations checked into source.
- **Frontend**: Angular (latest stable major), TypeScript strict mode enabled,
  RxJS, Angular Router with lazy feature modules, Angular Material or
  PrimeNG for the component baseline.
- **Authentication**: ASP.NET Core Identity + JWT (access + refresh).
- **Logging**: Serilog with sinks for console (dev) and a centralized provider
  (e.g., Application Insights / Seq) in deployed environments.
- **API documentation**: Swashbuckle / NSwag generating OpenAPI 3.x.
- **Testing**: xUnit + FluentAssertions + Moq (backend); Jasmine + Karma +
  Playwright (frontend).
- **Source control & CI**: Git with feature branches; CI MUST run build, unit
  tests, integration tests, lint, and Angular bundle-size budget checks.
- **Solution layout** (web application — backend + frontend):
  - `backend/src/Quraan.Api` (Controllers, DI composition root)
  - `backend/src/Quraan.Application` (Services, DTOs, validators)
  - `backend/src/Quraan.Domain` (entities, domain interfaces)
  - `backend/src/Quraan.Infrastructure` (EF Core `DbContext`, repositories,
    external integrations)
  - `backend/tests/{Unit,Integration,Contract}`
  - `frontend/src/app/{core,shared,features/{quran,tafsir,hadith,madhahib,azkar,auth,admin}}`
  - `frontend/tests/{unit,e2e}`

Database design MUST include at minimum: `Surahs`, `Ayahs`, `Translations`,
`Reciters`, `AudioRecitations`, `TafsirSources`, `TafsirEntries`,
`HadithCollections`, `HadithChapters`, `Hadiths`, `Madhahib`, `MadhabTopics`,
`AzkarCategories`, `Azkar`, `Users`, `Roles`, `Bookmarks`, `ReadingHistory`,
`AzkarCompletions`. All foreign keys MUST be enforced at the database level; soft
delete is the default for user-generated rows.

## Development Workflow & Quality Gates

- **Branching**: trunk-based with short-lived feature branches; branch names
  follow `###-feature-slug` per Spec Kit numbering.
- **Pull requests**: every PR MUST link to a spec or task ID, MUST pass CI, and
  MUST receive at least one review. PRs touching `Domain` or `Infrastructure`
  MUST receive review from a backend owner; PRs touching shared `core/` or i18n
  MUST receive review from a frontend owner.
- **Constitution check**: every implementation plan MUST include a
  Constitution Check section asserting compliance with Principles I–VII;
  unresolved gate failures block plan approval.
- **Migrations**: every schema change ships as a single, named EF Core migration
  in the same PR as the code that uses it. Down-migrations MUST be reviewable.
- **Definition of Done** for any user-story task:
  1. Tests written first and passing.
  2. Localization keys added for both `ar` and `en`.
  3. Accessibility check (keyboard + screen reader) for any new UI.
  4. OpenAPI spec regenerated and committed.
  5. No new lint, type, or bundle-budget violations.
  6. Manual smoke check in both light and dark themes when UI changes.
- **Release versioning**: the application uses semantic versioning
  (`MAJOR.MINOR.PATCH`). Public API version (`/api/vN`) is bumped per
  Principle VII rules and is independent of the application release version.

## Governance

This constitution supersedes ad-hoc conventions and prior implementation notes.
Where this document and other guidance disagree, this document wins until
formally amended.

- **Amendment procedure**: any contributor may propose an amendment by opening a
  PR that modifies this file together with a Sync Impact Report. Amendments
  require approval from at least one backend owner and one frontend owner.
- **Versioning policy** for this constitution:
  - **MAJOR**: a principle is removed, redefined incompatibly, or the technology
    stack is changed in a backward-incompatible way.
  - **MINOR**: a new principle or a new mandatory section is added, or an
    existing principle is materially expanded.
  - **PATCH**: clarifications, wording fixes, or non-semantic refinements.
- **Compliance review**: every plan and every PR description MUST state how the
  change satisfies (or knowingly diverges from) the affected principles.
  Knowing divergences MUST be recorded in the plan's Complexity Tracking table
  with a justification.
- **Runtime guidance**: implementation details, agent prompts, and stack-specific
  how-tos live in `.specify/templates/` and feature-level `plan.md` /
  `quickstart.md` artifacts. Those documents elaborate on, but never override,
  this constitution.

**Version**: 1.0.0 | **Ratified**: 2026-04-27 | **Last Amended**: 2026-04-27
