# QuraanKareem.app — Implementation Plan

> Single source of truth for building QuraanKareem.app from an empty repo to a deployable
> MVP. Governed by the constitution at `.specify/memory/constitution.md` (v1.0.0).

---

## 1. Context

QuraanKareem.app is a greenfield Quran-centric multi-tenant SaaS platform delivering Quran
text, audio recitation, Hadith, and Azkar, with AI features built on a strict BYOK model.
The repository currently contains only `.specify/` and `.claude/` scaffolding — no
application code yet. This plan defines the buildout from empty repo to deployable MVP,
organized as **vertical slices** that each touch DB → API → UI → logging, so value lands
incrementally instead of after a long horizontal build.

### Constitutional anchors (must hold for every milestone)

- **Tenant isolation** at the data layer via Supabase RLS; single-brand tenants only.
- **BYOK only** — AI keys live in Supabase Vault, never in plain DB, never in logs, never in
  API responses.
- **Quran dataset is read-only & verified**; RTL is first-class everywhere.
- **Audio is streamed via Bunny CDN**; no DB blobs, no API proxying of audio bytes.
- **All APIs under `/api/v1/...`** with the canonical envelope:
  ```json
  { "success": true, "data": {}, "error": null }
  ```
- **AI calls only originate from the FastAPI service**, never from ASP.NET.

---

## 2. Repository Layout (to be created)

```
apps/
  web/                  # Angular + AntiGravity (Gemini 3 Pro tooling)
services/
  api/                  # ASP.NET Core — domain APIs
  ai/                   # FastAPI — AI-only service
packages/
  shared/               # DTOs, enums, constants
infra/
  supabase/             # SQL migrations, RLS policies, seed scripts
  bunny/                # CDN + Magic Containers config
docs/                   # This plan + future design docs
.specify/memory/        # Constitution (already exists)
```

---

## 3. Shared Environment Contract

```
SUPABASE_URL=
SUPABASE_ANON_KEY=
SUPABASE_SERVICE_KEY=

BUNNY_STORAGE_ZONE=
BUNNY_CDN_URL=

AI_PROVIDERS=OpenAI,Gemini
VAULT_ENABLED=true
```

A `.env.example` mirroring the above MUST live at the repo root.

---

## 4. Shared DTOs (`packages/shared`)

- `Surah`, `Ayah`, `Reciter`, `AudioRef` (CDN URL only — never raw bytes)
- `UserPreferences` — `{ theme, reciter, lastReadAyahId }`
- `AIRequest` / `AIResponse` — no key material in either direction
- `ApiEnvelope<T> = { success: boolean; data: T | null; error: ApiError | null }`
- `ApiError = { code: string; message: string; details?: unknown }`

The envelope type is the canonical wire shape and MUST be enforced by middleware in both
ASP.NET and FastAPI.

---

## 5. Execution Rules

- Vertical slices are built in this order: **Quran → Audio → AI → Hadith → Azkar → Branding
  → Admin**.
- Every slice ships: DB schema (if needed) + API endpoint + UI screen + logging + analytics.
- Every PR touching data access MUST prove tenant scoping (test or review note).
- Every PR touching AI MUST prove no key material is logged or returned.
- Every UI PR MUST verify RTL rendering for Arabic content.
- Quran dataset is treated as immutable in production; changes require a migration + audit.
- Audio MUST be streamed via Bunny CDN (no API proxying, no DB blobs).

---

## 6. Milestones

### Milestone 0 — Scaffolding & Shared Contracts

**Goal:** stand up the repo skeleton and shared contracts so every later slice has a home.

- Create the repo layout in section 2.
- Initialize Angular app (`apps/web`), ASP.NET Core solution (`services/api`), FastAPI
  project (`services/ai`).
- Define `packages/shared` DTOs and the `ApiEnvelope` type.
- Wire response middleware in ASP.NET and FastAPI to enforce the envelope on every response
  (success and error paths).
- Create `.env.example` matching the environment contract.
- Add lint / format / test baselines for each stack (eslint+prettier, dotnet format, ruff).

**Exit criteria:** every service boots locally, returns a hello-world response wrapped in
`ApiEnvelope`, and CI runs lint+test on each push.

---

### Milestone 1 — Database Foundation (Supabase)

**Goal:** establish the canonical schema with tenant isolation enforced at the DB layer.

**Tables**

- `tenants` — single brand per tenant
- `profiles` — user ↔ tenant association
- `surahs`, `ayahs`, `reciters` — public read-only Quran dataset
- `audio_files` — metadata + CDN URL refs (no blobs)
- `hadith`, `azkar` — public read-only
- `user_preferences` — per-user settings
- `ai_keys` — stores Vault references only, never plaintext
- `branding` — per-tenant white-label config
- `activity_logs` — append-only audit trail

**Rules**

- Enable **RLS** on every non-public table; users can access only their own rows scoped by
  `tenant_id`.
- `surahs`, `ayahs`, `reciters`, `hadith`, `azkar` are public read-only.
- `ai_keys` rows store only Vault references.
- `activity_logs` is **append-only** — revoke `UPDATE`/`DELETE` at the role level; enforce
  via trigger as a defense-in-depth.
- Storage buckets: `quran-audio`, `app-assets`.
- Migrations live under `infra/supabase/migrations/` (timestamp-prefixed `.sql` files).

**Exit criteria:** running migrations against a fresh Supabase project produces the schema
with RLS enabled, and a cross-tenant read test fails as expected.

---

### Milestone 2 — Backend Core (ASP.NET Core)

**Goal:** ship the read-side of the platform behind a stable, tenant-aware API.

**Modules under `/api/v1`:** `quran`, `audio`, `hadith`, `azkar`, `user`, `analytics`.

**Endpoints (first wave)**

- `GET  /api/v1/quran/surahs`
- `GET  /api/v1/quran/surahs/{id}/ayahs`
- `GET  /api/v1/quran/search?q=`
- `GET  /api/v1/audio/{ayahId}?reciter=` → returns Bunny CDN URL
- `GET  /api/v1/hadith`
- `GET  /api/v1/azkar`
- `GET  /api/v1/user/preferences`
- `PUT  /api/v1/user/preferences`

**Cross-cutting**

- Supabase JWT validation middleware → resolves `(userId, tenantId)`; `tenantId` is
  mandatory on every authenticated request.
- Global exception handler that emits the `ApiEnvelope` error shape.
- Structured JSON logging with redaction rules — never log secrets.
- Redis cache for `surahs` and `ayahs` (read-only Quran data).

**Exit criteria:** all listed endpoints respond with `ApiEnvelope`, two-tenant integration
tests pass, and contract tests assert envelope shape on every route.

---

### Milestone 3 — AI Service (FastAPI)

**Goal:** isolate all AI provider interactions into a dedicated service with strict secret
hygiene.

**Endpoints under `/api/v1/ai`**

- `POST /api/v1/ai/key` — store/replace BYOK; writes to Supabase Vault only.
- `POST /api/v1/ai/ask`
- `POST /api/v1/ai/tafseer`

**Rules**

- ASP.NET MUST NOT call AI providers directly; it proxies user intent to FastAPI.
- FastAPI fetches the tenant key from Vault per-request and never caches plaintext.
- Per-tenant rate limiting.
- Log usage metadata (tokens, provider, latency) — never prompts/keys verbatim unless the
  tenant has opted in for query history (and even then, no key material).

**Exit criteria:** integration test submits a request using a fake Vault key and asserts the
key never appears in logs or response bodies.

---

### Milestone 4 — Frontend Core (Angular + AntiGravity)

**Goal:** deliver the user-facing shell with RTL-first UX and partial-offline support.

**Modules:** `quran`, `audio`, `ai`, `hadith`, `azkar`, `settings`, `admin`.

**Screens:** Quran Reader, Audio Player, AI Chat, Hadith List, Azkar List, Settings (theme +
reciter + font), Admin dashboard.

**Cross-cutting**

- RTL-first layout; Arabic typography pipeline.
- Dark mode + font switching.
- Service worker cache for previously read Surahs (partial offline per Principle IV).
- API client typed against `packages/shared` DTOs and `ApiEnvelope`.

**Exit criteria:** every screen renders correctly RTL; cached Surahs are readable with the
network disabled.

---

### Milestone 5 — Audio System

**Goal:** smooth, ayah-by-ayah recitation playback served entirely from CDN.

- Audio assets uploaded to Supabase Storage, fronted by Bunny CDN.
- API returns CDN URLs only — no proxying, no DB blobs.
- Player features: ayah-by-ayah playback, auto-next, reciter switching, resume from
  `lastReadAyahId`.

**Exit criteria:** end-to-end test plays an ayah via Bunny CDN URL returned by the API; auto-
next advances correctly across surah boundaries.

---

### Milestone 6 — AI Integration End-to-End

**Goal:** wire Angular ↔ ASP.NET ↔ FastAPI ↔ provider with strict secret hygiene.

- Flow: Angular → ASP.NET (`/api/v1/ai/*` proxy) → FastAPI → provider → response.
- Features: Ask-about-Ayah, Generate Tafseer, Smart Search.
- Verify in tests: no key material appears in ASP.NET logs, FastAPI logs, or HTTP responses.

**Exit criteria:** a tenant can register a BYOK key, ask a question about an ayah, and
receive a streamed answer; secret-leak tests pass.

---

### Milestone 7 — Branding System

**Goal:** white-label support per tenant.

- `branding` table per tenant: logo URL, theme colors, app name.
- Frontend resolves branding at bootstrap from tenant context.
- Admin screen to update branding; assets stored in `app-assets` bucket.

**Exit criteria:** two seeded tenants render with distinct logos, palettes, and app names
without code changes.

---

### Milestone 8 — Analytics & Logging

**Goal:** observable platform usage via append-only logs.

- Track: ayah plays, AI usage (counts only), active users, errors.
- Admin dashboard: most-played Surahs, AI usage stats, user activity.
- All writes go to `activity_logs` (append-only).

**Exit criteria:** dashboard renders real metrics from `activity_logs`; attempts to mutate
historical log rows fail at the DB layer.

---

### Milestone 9 — Deployment

**Goal:** production rollout on Bunny Magic Containers with CI/CD.

- Bunny Magic Containers for `services/api`, `services/ai`, `apps/web`.
- CI/CD pipeline: lint → test → build → deploy per service.
- Environment configs per stage; secrets via Bunny env + Supabase Vault.
- CDN integration verified end-to-end (audio + static assets).

**Exit criteria:** smoke test hitting `/api/v1/quran/surahs` succeeds on each environment
after rollout; audio streams from Bunny CDN end-to-end.

---

## 7. Critical Files (First Wave)

- `infra/supabase/migrations/0001_init.sql` — tables, RLS policies, append-only log triggers.
- `services/api/QuraanKareem.Api.sln` + `Program.cs` with envelope middleware + JWT auth.
- `services/ai/app/main.py` — FastAPI app with Vault client + provider adapters.
- `apps/web/angular.json` + RTL-aware shell layout.
- `packages/shared/src/index.ts` — DTOs + `ApiEnvelope`.
- `.specify/templates/plan-template.md`, `spec-template.md`, `tasks-template.md` — sync to
  constitution v1.0.0 (tenant isolation, BYOK, RTL, envelope checks).
- `.env.example` at repo root.

---

## 8. Verification (Per Slice)

- **DB:** run migrations against a fresh Supabase project; assert RLS denies cross-tenant
  reads using two seeded tenants.
- **API:** contract tests asserting every response matches `ApiEnvelope`; auth tests
  asserting requests without tenant context are rejected.
- **AI:** integration test that submits a request with a fake Vault key and asserts the key
  never appears in logs or response bodies.
- **Audio:** end-to-end test that `GET /audio/{ayahId}` returns a Bunny CDN URL and that the
  URL streams successfully from the browser.
- **Frontend:** RTL snapshot tests for Arabic screens; offline test that cached Surahs render
  with the network disabled.
- **Deploy:** smoke test hitting `/api/v1/quran/surahs` on each environment after rollout.

---

## 9. Open Questions / Deferred Decisions

- Confirm constitution `RATIFICATION_DATE` (currently provisional `2026-04-07`).
- Confirm whether AI query history is opt-in per tenant or platform default.
- Decide Redis hosting (Bunny container vs managed) before Milestone 2.
- Decide tenant onboarding flow (self-serve vs admin-provisioned) before Milestone 7.
- Decide which authoritative Quran text source to ingest in Milestone 1.
