# Phase 0 Research: Quran & Islamic Companion Web App (MVP)

**Branch**: `001-quran-mvp` | **Date**: 2026-04-27

This document records every choice made to resolve unknowns surfaced in
`spec.md` (Assumptions section) and `plan.md` (Technical Context). Each entry
follows: **Decision → Rationale → Alternatives considered**.

---

## R-01 Canonical Quran Arabic text

**Decision**: Use **Tanzil's Uthmani-script Hafs Quran text** (file
`quran-uthmani.txt`) as the single canonical source. Pinned by SHA-256 digest
in `backend/src/Quraan.Infrastructure/Seed/Sources/expected.sha256`. Seeder
recomputes digest on import and aborts if it does not match. Stored in DB with
both the raw form (`Ayah.ArabicText`) and a diacritic-stripped form
(`Ayah.NormalizedArabicText`) for search.

**Rationale**: Tanzil is the most widely cited free, scholarly-reviewed
distribution of the Madinah Mushaf Hafs reading (the dominant reading worldwide
and the default for both Quran.com and Al Quran Cloud). Uthmani script (rather
than Simple) preserves the orthographic conventions readers expect from a
Mushaf.

**Alternatives considered**:
- **King Fahd Complex digital Mushaf** — gold standard but distribution license
  requires explicit per-app permission, blocking time-to-launch.
- **quran.com API as a live source** — couples reading to an external dependency
  and makes Constitution Principle I's "verbatim from a single citable canonical
  source" harder to verify; we want the bytes locally.

---

## R-02 English translation edition

**Decision**: Use **Saheeh International** translation, sourced from Tanzil's
hosted edition (`en.sahih.txt`). Attribution string "Saheeh International,
Almunatada Alislami, public-domain edition" surfaced per Ayah and in the app
footer.

**Rationale**: Saheeh International is one of the most widely used modern
English translations, deliberately readable for non-native speakers, and is
distributed under a permissive licence on Tanzil. It pairs naturally with the
Hafs Uthmani Arabic text.

**Alternatives considered**:
- **Pickthall** — public domain but archaic register; harder for modern English
  learners.
- **Yusuf Ali (modified)** — long footnotes that don't fit the MVP UI.
- **The Clear Quran (Mustafa Khattab)** — excellent quality but stricter
  licensing; defer to a future post-MVP release.

---

## R-03 Tafsir Ibn Kathir source

**Decision**: Use the **abridged Tafsir Ibn Kathir English digest** distributed
by [spa5k/tafsir_api](https://github.com/spa5k/tafsir_api) (which republishes
the public-domain Mawlana Safi-ur-Rahman Mubarakpuri abridgement). Pinned to a
specific commit SHA, file `en-tafisr-ibn-kathir.json`, digest verified at
ingest. Stored as `TafsirEntry` rows keyed by Surah/Ayah. Where the source
omits an entry (rare but possible for some Ayahs of grouped passages), the
TafsirEntry row is absent and the API returns `404` — which the UI renders as
"Tafsir not available for this Ayah" (FR-023).

**Rationale**: A pinned-commit GitHub source gives reproducible builds and
verifiable provenance. The Mubarakpuri abridgement is the canonical Ibn Kathir
text most online platforms use and has explicit public-domain status.

**Alternatives considered**:
- **Live quran.com tafsir endpoint** — couples a content-integrity-critical
  feature (Principle I) to network availability and makes attribution drift
  invisible.
- **Full unabridged Tafsir Ibn Kathir** — too large for MVP UI density and
  licensing of complete English translations is murkier.

---

## R-04 Audio source and per-Ayah timing

**Decision**: Two-tier strategy:
1. **Surah audio URL**: composed deterministically from **Al Quran Cloud**'s
   per-Surah CDN convention
   `https://cdn.islamic.network/quran/audio-surah/128/{reciter-id}/{surah}.mp3`.
   Browser handles HTTP range requests natively (no proxy needed).
2. **Per-Ayah timing markers**: fetched on demand from **quran.com v4 API**
   `GET https://api.quran.com/api/v4/recitations/{recitation_id}/by_chapter/{chapter}`
   which returns `verse_timings: [{verse_key, timestamp_from, timestamp_to}, …]`.
   Cached server-side in `IDistributedCache` keyed by `(reciterId, surahId)`
   for 24h. Cache miss falls back to "no highlight" mode (FR-013).

Reciter for MVP: **Mishary Rashid Alafasy** (Al Quran Cloud reciter id `ar.alafasy`,
quran.com recitation id `7`). One reciter only in MVP (Assumption: "single
default reciter").

The `AudioController` returns a single response combining the audio URL and
timing array; clients never call the external APIs directly (keeps API keys,
rate-limit handling, and CORS centralized server-side).

**Rationale**: This split exploits each provider's strength — Al Quran Cloud's
CDN is the simplest fast path for audio bytes; quran.com publishes the
highest-quality timing data and a structured public API. Centralizing the
fetch in the backend lets us cache aggressively and degrade gracefully
(Constitution VI + FR-013).

**Alternatives considered**:
- **Self-host audio files** — bandwidth cost and licensing of reciter audio
  recordings is non-trivial; out of scope for MVP.
- **Per-Ayah audio URLs (one HTTP request per Ayah)** — defeats
  smooth-playback UX and SC-003 (audio start ≤ 2 s) due to per-Ayah connect
  overhead.
- **Use only Al Quran Cloud (skip timing)** — gives Surah audio but no Ayah
  highlight; we want the highlight when available (US3 acceptance scenario 4).

---

## R-05 Arabic search normalization

**Decision**: Implement a `ArabicNormalizer` static class
(`Quraan.Application.Search.ArabicNormalizer`) that, before both ingest and
query, applies the following pipeline:

1. Strip all combining marks in `U+064B–U+065F` (tashkeel: fatha, kasra,
   damma, shadda, sukun, etc.).
2. Strip the dagger alif `U+0670` and small high marks `U+06D6–U+06ED`.
3. Strip tatweel `U+0640`.
4. Normalize Alif variants → bare Alif: `أ إ آ ٱ → ا` (`U+0623, 0625, 0622, 0671 → 0627`).
5. Normalize Yāʾ variants: `ى → ي` (`U+0649 → 064A`).
6. Normalize Tāʾ marbūṭa `ة → ه` (`U+0629 → 0647`) for query-time only — kept
   distinct in stored normalized text so display is unaffected.
7. Lowercase ASCII (for transliterated names).

Stored once per Ayah in `Ayah.NormalizedArabicText` (computed at seed time).
At query time, the same pipeline runs on the user query and EF Core executes a
`LIKE '%query%'` against `NormalizedArabicText`. With ≤ 6,236 rows, this is
fast enough for SC-002 even without a full-text index; a SQL Server full-text
catalogue is added in `M001` migration as a future-proofing measure (and
gives us `CONTAINS(...)` for word-boundary search later).

**Rationale**: Quran Ayah count is small enough that a `LIKE` scan against a
normalized column hits SC-002 well within 1 s, and it gives us total control
over the normalization rules. SQL Server full-text doesn't natively understand
all the Quranic diacritic marks, so pre-normalization in C# is the safer
correctness path (Principle I).

**Alternatives considered**:
- **Elasticsearch / OpenSearch** — explicitly out of scope per the PM brief.
- **SQL Server full-text only** — depends on accent/diacritic insensitive
  collation behaviour for Arabic, which is inconsistent across SQL Server
  versions; pre-normalization avoids the surprise.
- **In-memory search index** — adds a startup-warmup gate and a second source
  of truth; not justified at this scale.

---

## R-06 Authentication & token strategy

**Decision**:
- **Identity store**: ASP.NET Core Identity with EF Core, custom
  `ApplicationUser : IdentityUser<Guid>` (PK is `Guid` to avoid integer
  enumeration in URLs/logs).
- **Password hashing**: replace Identity's default `PasswordHasher<TUser>` with
  one backed by **Konscious.Security.Cryptography.Argon2** using OWASP-current
  parameters (memory 19 MiB, iterations 2, parallelism 1, 32-byte salt,
  32-byte hash). Hash format `$argon2id$…` stored in the existing
  `PasswordHash` column.
- **Access token**: JWT signed with HMAC-SHA256, **15-minute** lifetime,
  symmetric key from configuration (Azure Key Vault in prod, user-secrets in
  dev). Claims: `sub` = user GUID, `email`, `name`, `role`.
- **Refresh token**: opaque 256-bit token returned alongside the access token,
  stored in DB as **SHA-256 hash** (table `RefreshTokens` with columns
  `UserId, TokenHash, IssuedAt, ExpiresAt, RevokedAt, ReplacedByTokenHash`).
  30-day lifetime, **rotating** — every refresh issues a new refresh token
  and revokes the old one. Reuse of a revoked token revokes the entire chain
  (refresh-token-reuse detection).
- **Transport**: refresh token returned in a `HttpOnly; Secure; SameSite=Strict`
  cookie scoped to `/api/v1/auth/refresh`. Access token returned in JSON body
  for the SPA to attach as `Authorization: Bearer …`. CSRF on the refresh
  endpoint is mitigated by the strict SameSite cookie.
- **Anonymous use**: every endpoint that doesn't carry `[Authorize]` works
  without a token (FR-027). The `/api/v1/bookmarks/*` and
  `/api/v1/lastread/me` endpoints require auth; anonymous last-read is stored
  client-side in `localStorage`.

**Rationale**: Argon2id is the OWASP-recommended password hash; the default
PBKDF2 in Identity is acceptable but Argon2id is materially stronger and the
constitution's "current OWASP guidance" wording allows us to choose. Short
access tokens + rotating refresh + reuse detection is the standard secure
pattern for SPAs that don't have a server-side session store. SameSite=Strict
on the refresh cookie removes CSRF without needing antiforgery tokens on the
refresh path.

**Alternatives considered**:
- **Cookie-based session auth** — simpler but doesn't fit a future native-mobile
  client cleanly; the constitution names JWT explicitly.
- **PBKDF2 (Identity default)** — acceptable but weaker; Argon2id is a small
  one-time setup cost.
- **Long-lived JWTs (no refresh)** — can't be revoked on sign-out (FR-021),
  violating Principle V.
- **Both tokens in `localStorage`** — XSS-exfiltrable; cookies for refresh keep
  the long-lived secret out of JS reach.

---

## R-07 Caching strategy for content reads

**Decision**:
- Wrap the four content read paths (`SurahService.GetAll`, `SurahService.GetById`,
  `AyahService.GetBySurah`, `TafsirService.GetByAyah`, `SearchService.Search`)
  in a `CachedReader` decorator that uses `IDistributedCache` (Redis) in
  production and `IMemoryCache` in dev/tests.
- TTL: **24 hours** for content reads, **15 minutes** for search results
  (search is parameterized by a query string; cache key includes the
  normalized query).
- Cache key prefix `quraan:v1:` so a future schema change can be invalidated
  by bumping the prefix.
- No write-side invalidation needed in MVP because there are no admin edit
  endpoints; the cache simply expires and is repopulated on first read after
  deploy.
- The cache is bypassed for authenticated personalized endpoints (bookmarks,
  last-read).

**Rationale**: All Quran/Tafsir content is effectively immutable after seed,
so a long TTL is safe and dramatically reduces DB load. Redis is the standard
.NET distributed cache; `IDistributedCache` interface lets dev environments
fall back to in-memory without code changes.

**Alternatives considered**:
- **HTTP `Cache-Control` only (browser/CDN cache)** — works for unauthenticated
  endpoints but doesn't help server-side under burst traffic; combine with
  server cache, not replace.
- **EF Core second-level cache (e.g., EFCore.SecondLevel.Cache)** — adds a
  package with its own invalidation semantics; explicit caching at the
  service boundary is clearer.

---

## R-08 i18n strategy on the frontend

**Decision**: Use **`@ngx-translate/core`** with HTTP loader pulling
`assets/i18n/{ar,en}.json` per locale.

- `LanguageService` (in `core/i18n/`) exposes `currentLang$: BehaviorSubject<'ar' | 'en'>`
  and a `setLanguage(lang)` method.
- `RtlService` listens to `currentLang$` and toggles `dir="rtl"` /`dir="ltr"`
  on the document root.
- All component CSS uses logical properties (`margin-inline-start` etc.) — a
  Stylelint rule (`stylelint-use-logical`) enforces this in CI.
- Persistence: anonymous → `localStorage.setItem('lang', 'ar' | 'en')`.
  Authenticated → on sign-in, prefer the server-stored `User.PreferredLanguage`;
  on language change, PATCH `/api/v1/users/me { preferredLanguage }`.
- A custom ESLint rule (`@angular-eslint/template-no-call-expression` + a
  hand-rolled rule banning literal `>[A-Za-z]+<` in templates) blocks
  unlocalized strings on merge.

**Rationale**: `@ngx-translate` is the most widely supported i18n library for
runtime-switchable Angular apps (Angular's built-in `@angular/localize` is
oriented towards build-time multi-bundle output, which forces a page reload to
switch — violating FR-006 "without a full page reload").

**Alternatives considered**:
- **`@angular/localize` with two builds** — page reload on toggle is a UX
  regression and complicates routing.
- **`Transloco`** — comparable feature set; chosen `@ngx-translate` for
  ecosystem familiarity.

---

## R-09 Audio playback abstraction

**Decision**: Use **`howler.js`** wrapped in an `AudioPlayerService`
(RxJS-based) under `frontend/src/app/features/audio/`. Howler handles HTML5
Audio fallback, mobile autoplay quirks, and provides `seek()`/`pos()` we use
to drive Ayah-highlight.

- `AudioPlayerService` exposes `play(surahId)`, `pause()`, `resume()`,
  `seek(ayahNumber)`, and a `currentAyah$: Observable<number | null>`.
- The "current Ayah" derivation: subscribe to a 250 ms `interval` while
  playing, ask Howler `pos()`, look up the Ayah whose `[from, to]` window
  contains the current position. If timings array is empty, `currentAyah$`
  emits `null` permanently for that session.
- The `SurahReaderPage` subscribes to `currentAyah$` and applies a
  `.is-playing` class on the matching `<app-ayah>`; CSS handles the visual
  highlight (no DOM scroll-to so we don't fight the user's reading position
  in MVP — they can manually scroll).

**Rationale**: Howler abstracts away the most common mobile-audio edge cases
(iOS autoplay-on-tap, Android suspend/resume) that raw `<audio>` elements
mishandle. RxJS observable surface composes well with the rest of the app.

**Alternatives considered**:
- **Raw HTML5 `<audio>` element** — minimal but iOS/Safari edge cases bite.
- **plyr.io / video.js** — heavier; aimed at video.

---

## R-10 OpenAPI / API client generation

**Decision**: Backend generates `swagger.json` via **Swashbuckle.AspNetCore**
on every build (`dotnet build` runs an `AfterBuild` target that calls
`dotnet swagger tofile … swagger.json`). Frontend has an npm script
`npm run gen:api` that calls **NSwag** (`nswag/openapi2tsclient`) against the
generated spec to produce `frontend/src/app/api/quraan-api.client.ts` and
matching DTOs. CI fails if `swagger.json` differs from the committed copy at
`specs/001-quran-mvp/contracts/openapi.yaml`, ensuring the contract never
silently drifts.

**Rationale**: One source of truth (the C# controllers + DTOs), a generated
client (eliminating hand-written DTO drift), and a CI gate to keep the
committed contract artifact honest. Aligns with Constitution Principle VII.

**Alternatives considered**:
- **Hand-written `HttpClient` services in Angular** — fast initially, but
  every DTO field rename becomes a silent runtime break.
- **OpenAPI Generator (Java)** — works but adds a JVM dependency to the
  frontend toolchain; NSwag is .NET-native and already familiar.

---

## R-11 Accessibility tooling

**Decision**:
- Linting: `eslint-plugin-jsx-a11y` equivalents for Angular templates
  (`@angular-eslint/eslint-plugin-template` with the `accessibility-*` rules
  enabled).
- Runtime: `axe-core` integrated into Playwright e2e tests — every e2e suite
  runs an Axe check at the end and fails the build on any new violation.
- Manual: documented keyboard-only test pass per release in
  `frontend/tests/e2e/checklists/a11y.md`.
- Contrast: theme tokens in `styles/_tokens.scss` are pre-validated against
  WCAG AA contrast ratios; a small Node script (`tools/check-contrast.mjs`)
  asserts ratios at build time.

**Rationale**: Constitution Principle IV requires WCAG 2.1 AA and SC-007
verifies it for both Arabic and English. Axe-core in CI provides a
machine-checkable gate; the manual checklist covers things axe-core can't
detect (focus order, screen-reader narration quality).

**Alternatives considered**:
- **Manual-only review** — slow, regression-prone.
- **Lighthouse CI** — useful but axe-core in Playwright integrates more
  naturally with the e2e suite already running per-story.

---

## R-12 Password reset (deferred decision from spec Assumptions)

**Decision**: **Defer to post-MVP.** No password-reset flow ships in MVP. The
sign-in page surfaces "Forgot your password?" only as a `mailto:` link to a
support address (configurable per environment); no token issuance, no email
sending, no SMTP integration in MVP scope. This is a conscious user-experience
trade made to keep MVP delivery focused; users who lose access support a
manual reset via the support inbox until the post-MVP email-verification +
reset feature ships.

**Rationale**: Email-based reset requires: SMTP integration, email-template
i18n, anti-abuse rate limiting, time-bounded reset tokens with single-use
semantics, and a verification-on-register flow to ensure the email is reachable.
Each is a small piece of work; together they would push MVP scope. The spec
flagged this assumption explicitly and the PM brief did not list reset as a
locked feature.

**Alternatives considered**:
- **Ship a basic email reset** — additional ~3-5 days of implementation +
  SMTP/SendGrid setup; acceptable but not chosen for MVP.
- **Magic-link sign-in (passwordless)** — would replace passwords entirely;
  out of scope for an "email + password" assumption.

---

## R-13 Anonymous last-read storage

**Decision**: Anonymous users' last-read pointer lives in `localStorage` under
key `quraan.lastRead = { surahId, ayahNumber, updatedAt }`. The home page
reads this on load to show "Continue reading Surah X, Ayah Y". On sign-in,
the SPA POSTs the local value to `/api/v1/lastread/me` — the server replaces
its row with the **more recent** of the two timestamps (last-write-wins by
`updatedAt`). After sign-in, the local key is cleared.

**Rationale**: Last-read is low-stakes personalization; no need to burn an
anonymous-account row in the DB or generate a device ID. Keeps anonymous use
truly stateless server-side.

**Alternatives considered**:
- **Server-side anonymous device records** — adds a junk row per browser
  visit; privacy + cost cost without value.
- **Don't track anonymous last-read** — fails FR-035 explicitly ("per device
  for anonymous users").

---

## Resolved unknowns summary

| Spec Assumption / Plan unknown | Resolved by |
|---|---|
| Canonical Quran edition | R-01 |
| English translation edition | R-02 |
| Tafsir source | R-03 |
| Audio source + Ayah timing approach | R-04 |
| Arabic search normalization rules | R-05 |
| Auth / token strategy specifics | R-06 |
| Caching strategy | R-07 |
| Frontend i18n implementation | R-08 |
| Audio playback library | R-09 |
| OpenAPI contract workflow | R-10 |
| Accessibility tooling | R-11 |
| Password-reset scope decision | R-12 |
| Anonymous last-read persistence | R-13 |

**No NEEDS CLARIFICATION markers remain.**
