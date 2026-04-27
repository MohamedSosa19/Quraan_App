# Quickstart: Quran & Islamic Companion Web App (MVP)

**Branch**: `001-quran-mvp` | **Date**: 2026-04-27

This is the developer setup + smoke-test guide for the MVP. Run it after
`/speckit.tasks` and `/speckit.implement` produce the code; until then it
documents the *intended* developer workflow so the implementation can be
verified against it.

---

## 1. Prerequisites

| Tool                  | Version           | Notes                                    |
|-----------------------|-------------------|------------------------------------------|
| .NET SDK              | 8.0 LTS           | `dotnet --version` → `8.0.x`             |
| Node.js               | 20 LTS            | For Angular 18                           |
| Angular CLI           | latest 18.x       | `npm i -g @angular/cli@18`               |
| SQL Server            | LocalDB or Express 2022 | Connection string in `appsettings.Development.json` |
| Docker                | latest            | For Testcontainers (integration tests) and Redis |
| Git                   | any recent        |                                          |

Optional but recommended: VS Code + the `C# Dev Kit`, `Angular Language Service`,
and `Markdown All in One` extensions.

---

## 2. One-time setup

```bash
# Clone and switch to the feature branch
git clone <repo-url> quraan-app
cd quraan-app
git checkout 001-quran-mvp

# --- Backend ---
cd backend
dotnet restore
# Configure dev secrets (JWT key + DB conn string)
dotnet user-secrets init --project src/Quraan.Api
dotnet user-secrets set "Jwt:SigningKey" "$(openssl rand -base64 64)" --project src/Quraan.Api
dotnet user-secrets set "ConnectionStrings:Default" "Server=(localdb)\\MSSQLLocalDB;Database=Quraan_Dev;Trusted_Connection=True;TrustServerCertificate=True" --project src/Quraan.Api

# Apply EF Core migrations
dotnet ef database update --project src/Quraan.Infrastructure --startup-project src/Quraan.Api

# Seed Quran + translation + Tafsir + reciter
dotnet run --project src/Quraan.Api -- seed
# (Verifies SHA-256 of source files at backend/src/Quraan.Infrastructure/Seed/Sources/expected.sha256)

# --- Redis (for distributed cache; optional in dev — falls back to in-memory) ---
docker run -d --name quraan-redis -p 6379:6379 redis:7-alpine

# --- Frontend ---
cd ../frontend
npm ci
npm run gen:api    # generates src/app/api/* from backend swagger.json
```

---

## 3. Run the app

Two terminals:

```bash
# Terminal 1 — backend
cd backend
dotnet run --project src/Quraan.Api
# → Listening on http://localhost:5080
# Swagger UI at http://localhost:5080/swagger

# Terminal 2 — frontend
cd frontend
npm start
# → Angular dev server at http://localhost:4200
```

Open <http://localhost:4200>. Surah list should appear in under 1 s.

---

## 4. Smoke tests (one per user story)

Run these in a browser to manually verify each user story end-to-end. Each
maps to the acceptance scenarios in `spec.md`.

### US1 — Read the Quran (P1)
1. Open `http://localhost:4200`.
2. Confirm the home page shows a "Continue reading" affordance is hidden (no
   prior read) and a Surah-list link.
3. Click "Surahs" → confirm 114 entries with Arabic + transliterated +
   English names.
4. Click *Al-Fatiha* → confirm 7 Ayahs render in Arabic Uthmani script with
   English (Saheeh Intl.) translation.
5. Use "Next Surah" → *Al-Baqarah* opens at Ayah 1.
6. In Al-Baqarah, type Ayah `255` in the jump-to-Ayah input → confirm Ayat
   al-Kursi scrolls into view.

### US2 — Bilingual UI + RTL (P1)
1. From any page, open the language toggle and switch to **العربية**.
2. Confirm: chrome strings flip to Arabic, page direction is RTL, Quran
   Arabic text is unchanged, English translation is unchanged.
3. Toggle back to **English** → page becomes LTR, no full reload (network tab
   shows no document request).
4. Refresh the page → app opens in English (last selection persisted in
   `localStorage`).

### US3 — Audio playback (P2)
1. Open Surah *An-Naba* (78).
2. Press Play → audio starts within 2 s.
3. Confirm currently-playing Ayah is highlighted and the highlight advances.
4. Press Pause → audio stops; control becomes Resume; press Resume → playback
   continues from same position.
5. Disconnect network → press Play on a fresh Surah → confirm a user-visible
   error message + Retry button.

### US4 — Search (P2)
1. Click the search bar.
2. Type `mercy` → results return in under 1 s; Surah names + Ayahs containing
   "mercy" appear with reference (e.g., 1:3, 7:156).
3. Click a result → opens that Surah scrolled to that Ayah.
4. Type `Yaseen` → Surah Ya-Sin (36) appears in surah-name matches.
5. Type `الرحمن` → results include Ayahs containing الرَّحْمَٰنِ (diacritics
   ignored).
6. Type `qwertyuiop` → "no results" message appears.

### US5 — Tafsir (P3)
1. Open Al-Fatiha, click the Tafsir affordance on Ayah 2.
2. Confirm Tafsir Ibn Kathir text appears with attribution
   "Tafsir Ibn Kathir (Mubarakpuri abridged)" visible.
3. Move to Ayah 3 → Tafsir panel updates.
4. Find an Ayah without a Tafsir entry (use one the seed log lists as
   "no entry") → confirm "Tafsir not available for this Ayah" message.

### US6 — Auth (P3)
1. Click Sign In → Register tab. Submit email + password (≥ 12 chars).
2. Confirm you land back on the home page in a signed-in state (header shows
   user menu).
3. Open DevTools → Application → Cookies. Verify a `quraan-refresh` cookie
   with `HttpOnly; Secure; SameSite=Strict`.
4. Close the browser, reopen → still signed in (refresh cookie + automatic
   token refresh).
5. Sign out → user menu disappears; visiting `/bookmarks` redirects to sign-in.

### US7 — Bookmarks (P3)
1. While signed in, open *Al-Mulk* (67), click the bookmark icon on Ayah 1.
2. Open the Bookmarks page → confirm `Al-Mulk : 1` appears with a recent
   timestamp.
3. Click it → opens Surah Al-Mulk scrolled to Ayah 1.
4. Sign in on a second browser profile with the same account → bookmark is
   present (cross-device sync, SC-008).
5. As an anonymous user, click the bookmark icon on any Ayah → confirm the
   sign-in prompt (no silent loss).

---

## 5. Automated test suites

```bash
# Backend
cd backend
dotnet test tests/Quraan.UnitTests          # fast, no DB
dotnet test tests/Quraan.IntegrationTests   # spins up SQL Server via Testcontainers
dotnet test tests/Quraan.ContractTests      # snapshot-tests OpenAPI spec

# Frontend
cd ../frontend
npm test                  # Jasmine + Karma unit
npm run e2e               # Playwright e2e (requires backend running)
npm run a11y              # Axe-core checks bundled into Playwright
```

CI runs all of the above plus:
- Bundle-size budget check (`ng build --configuration production` + budgets in `angular.json`)
- `swagger.json` drift check vs `specs/001-quran-mvp/contracts/openapi.yaml`
- ESLint, Stylelint (logical-property rule), and the no-literal-string template rule
- Contrast-ratio check (`tools/check-contrast.mjs`)

---

## 6. Constitution-compliance verification

Per Constitution Principle I, run after seed:

```bash
cd backend
dotnet run --project src/Quraan.Api -- verify-content
# → Recomputes SHA-256 of every Ayah row's ArabicText against expected.sha256.
# → Exits non-zero on any mismatch (CI fails the build).
```

---

## 7. Common troubleshooting

| Symptom                                             | Fix                                                         |
|-----------------------------------------------------|-------------------------------------------------------------|
| `dotnet ef` not found                               | `dotnet tool install --global dotnet-ef --version 8.*`      |
| Seed aborts with "digest mismatch"                  | Source file in `Seed/Sources/` was modified — restore it    |
| Frontend can't reach backend                        | Backend port 5080 blocked or `proxy.conf.json` not loaded   |
| Audio Play does nothing on Safari iOS               | Tap the Play button directly (autoplay policy); not a bug   |
| Arabic shapes look broken                           | KFGQPC font failed to load — check `assets/fonts/`          |
| `npm run gen:api` fails                             | Backend must be running (or `swagger.json` regenerated)     |
| Refresh doesn't extend session                      | Cookie missing — confirm `Secure` requires HTTPS in prod    |

---

## 8. Where things live

| Concern                          | Location                                                       |
|----------------------------------|----------------------------------------------------------------|
| Quran/Tafsir/Reciter sources     | `backend/src/Quraan.Infrastructure/Seed/Sources/`              |
| EF migrations                    | `backend/src/Quraan.Infrastructure/Persistence/Migrations/`    |
| Controllers                      | `backend/src/Quraan.Api/Controllers/V1/`                       |
| Services + DTOs                  | `backend/src/Quraan.Application/<feature>/`                    |
| OpenAPI contract (committed)     | `specs/001-quran-mvp/contracts/openapi.yaml`                   |
| Generated Angular API client     | `frontend/src/app/api/`                                        |
| i18n catalogues                  | `frontend/src/assets/i18n/{ar,en}.json`                        |
| Mushaf font                      | `frontend/src/assets/fonts/`                                   |
| Theme tokens                     | `frontend/src/styles/_tokens.scss`                             |
