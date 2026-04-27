# Phase 1 Data Model: Quran & Islamic Companion Web App (MVP)

**Branch**: `001-quran-mvp` | **Date**: 2026-04-27

EF Core 8 entities mapped to SQL Server 2022. All entity primary keys are
`Guid` except where the natural key (e.g., Surah number) is small, immutable,
and part of the public API. Foreign keys are enforced at the database level;
delete behaviour is `Restrict` on content tables and `Cascade` on user-owned
data. Soft delete (`IsDeleted` + `DeletedAt`) is applied only to user-owned
rows (`Bookmark`, `RefreshToken`); content rows are hard-deleted only via a
seeder rebuild.

---

## Entity catalogue

### `Surah` (content, immutable post-seed)

| Column                   | Type              | Notes                                        |
|--------------------------|-------------------|----------------------------------------------|
| `Id` (PK)                | `tinyint`         | 1–114 (natural key — exposed in URLs)        |
| `ArabicName`             | `nvarchar(64)`    | e.g., "الفاتحة"                              |
| `TransliteratedName`     | `nvarchar(64)`    | e.g., "Al-Fatiha"                            |
| `EnglishName`            | `nvarchar(64)`    | e.g., "The Opening"                          |
| `EnglishNameNormalized`  | `nvarchar(64)`    | lowercase; for `LIKE` search (FR-015)        |
| `RevelationPlace`        | `nvarchar(16)`    | enum-as-string: `Meccan` \| `Medinan`        |
| `AyahCount`              | `smallint`        | Denormalized for list view                   |
| `OrderInRevelation`      | `tinyint`         | Optional metadata, not surfaced in MVP UI    |

**Indexes**: `IX_Surah_EnglishNameNormalized`, `IX_Surah_TransliteratedName`.

**Relationships**: 1 → many `Ayah` (FK `Ayah.SurahId`).

**Validation**: `AyahCount = COUNT(Ayahs WHERE SurahId = Id)` — verified by
seeder integrity check.

---

### `Ayah` (content, immutable post-seed)

| Column                   | Type                | Notes                                                         |
|--------------------------|---------------------|---------------------------------------------------------------|
| `Id` (PK)                | `int`               | 1–6236 (Tanzil's global Ayah index — exposed in URLs)         |
| `SurahId` (FK → Surah)   | `tinyint`           | `ON DELETE RESTRICT`                                          |
| `NumberInSurah`          | `smallint`          | 1-based                                                       |
| `ArabicText`             | `nvarchar(1024)`    | Tanzil Uthmani Hafs, with diacritics                          |
| `NormalizedArabicText`   | `nvarchar(1024)`    | Output of `ArabicNormalizer` — search (FR-017)                |
| `JuzNumber`              | `tinyint`           | 1–30                                                          |
| `HizbQuarter`            | `tinyint`           | 1–240                                                         |
| `Sajda`                  | `bit`               | True if recommended/obligatory prostration                    |

**Indexes**: `IX_Ayah_SurahId_NumberInSurah` (unique), `IX_Ayah_NormalizedArabicText`
(`LIKE`-friendly), and a SQL Server full-text catalogue on
`NormalizedArabicText` for future word-boundary search (R-05).

**Relationships**:
- many → 1 `Surah`
- 1 → many `AyahTranslation` (one row per `(Ayah, Translation)`)
- 1 → 0..1 `TafsirEntry` per `TafsirSource` (MVP: only Ibn Kathir)
- 1 → many `Bookmark`

---

### `Translation` (content, immutable post-seed)

| Column           | Type             | Notes                                              |
|------------------|------------------|----------------------------------------------------|
| `Id` (PK)        | `tinyint`        | Small enum-table; MVP seeds id=1 (Saheeh Intl.)    |
| `Code`           | `nvarchar(32)`   | e.g., `"en.sahih"`                                 |
| `Language`       | `nvarchar(8)`    | BCP-47, e.g., `"en"`                               |
| `Name`           | `nvarchar(128)`  | "Saheeh International"                             |
| `Attribution`    | `nvarchar(256)`  | License + source line shown in UI                  |

---

### `AyahTranslation` (content)

| Column                          | Type                | Notes                              |
|---------------------------------|---------------------|------------------------------------|
| `AyahId` (PK part, FK → Ayah)   | `int`               |                                    |
| `TranslationId` (PK part, FK)   | `tinyint`           |                                    |
| `Text`                          | `nvarchar(2048)`    | Translation body                   |
| `NormalizedText`                | `nvarchar(2048)`    | Lowercased, for English search     |

**Indexes**: composite PK; `IX_AyahTranslation_NormalizedText` for FR-016.

---

### `TafsirSource` (content, immutable post-seed)

| Column        | Type             | Notes                                       |
|---------------|------------------|---------------------------------------------|
| `Id` (PK)     | `tinyint`        | MVP seeds id=1 (`ibn-kathir-en`)            |
| `Code`        | `nvarchar(32)`   | e.g., `"ibn-kathir-en"`                     |
| `Name`        | `nvarchar(128)`  | "Tafsir Ibn Kathir (Mubarakpuri abridged)"  |
| `Language`    | `nvarchar(8)`    | BCP-47                                      |
| `Attribution` | `nvarchar(256)`  | License/source line                         |

---

### `TafsirEntry` (content)

| Column                          | Type                | Notes                                      |
|---------------------------------|---------------------|--------------------------------------------|
| `Id` (PK)                       | `int`               |                                            |
| `TafsirSourceId` (FK)           | `tinyint`           | MVP always 1                               |
| `AyahId` (FK → Ayah)            | `int`               |                                            |
| `Body`                          | `nvarchar(max)`     | HTML-safe plain-text or sanitized HTML     |

**Indexes**: `UQ_TafsirEntry_Source_Ayah` (unique). Sparse — not every Ayah
has an entry (FR-023).

---

### `Reciter` (content)

| Column          | Type             | Notes                                  |
|-----------------|------------------|----------------------------------------|
| `Id` (PK)       | `tinyint`        | MVP seeds id=1 (Alafasy)               |
| `Code`          | `nvarchar(32)`   | e.g., `"ar.alafasy"`                   |
| `Name`          | `nvarchar(128)`  | "Mishary Rashid Alafasy"               |
| `ArabicName`    | `nvarchar(128)`  | "مشاري راشد العفاسي"                    |
| `AlQuranCloudId`| `nvarchar(32)`   | external id for Al Quran Cloud         |
| `QuranComId`    | `int`            | external recitation id at quran.com    |
| `Attribution`   | `nvarchar(256)`  | License/source line                    |

---

### `ApplicationUser` (Identity)

Inherits `IdentityUser<Guid>`. Adds:

| Column                    | Type             | Notes                                  |
|---------------------------|------------------|----------------------------------------|
| `DisplayName`             | `nvarchar(128)`  | Optional                               |
| `PreferredLanguage`       | `nvarchar(8)`    | `"ar"` \| `"en"`, default `"en"`       |
| `CreatedAt`               | `datetime2`      | UTC                                    |
| `LastSignInAt`            | `datetime2`      | UTC, nullable                          |

(Standard Identity columns: `Id`, `Email`, `EmailConfirmed`, `PasswordHash`,
`SecurityStamp`, `ConcurrencyStamp`, `LockoutEnd`, etc.)

---

### `RefreshToken` (auth, user-owned)

| Column                | Type             | Notes                                                    |
|-----------------------|------------------|----------------------------------------------------------|
| `Id` (PK)             | `Guid`           |                                                          |
| `UserId` (FK)         | `Guid`           | `ON DELETE CASCADE`                                      |
| `TokenHash`           | `binary(32)`     | SHA-256 of opaque token; the token itself never persists |
| `IssuedAt`            | `datetime2`      | UTC                                                      |
| `ExpiresAt`           | `datetime2`      | UTC, IssuedAt + 30d                                      |
| `RevokedAt`           | `datetime2`      | UTC, nullable                                            |
| `ReplacedByTokenHash` | `binary(32)`     | nullable; set on rotation                                |
| `CreatedByIp`         | `nvarchar(64)`   | optional, for audit only — not surfaced in logs          |

**Indexes**: `IX_RefreshToken_UserId`, `UQ_RefreshToken_TokenHash`.

**State transitions**:
1. **Active**: `RevokedAt IS NULL AND ExpiresAt > now`.
2. **Rotated**: `RevokedAt = now`, `ReplacedByTokenHash` set, new active row issued.
3. **Revoked-on-signout**: `RevokedAt = now`, `ReplacedByTokenHash = NULL`.
4. **Reuse-detected**: a refresh against an already-rotated row revokes the
   *entire chain* (all rows for that user with `RevokedAt IS NULL`).
5. **Expired**: cleanup job deletes rows where `ExpiresAt < now - 30d`.

---

### `Bookmark` (user-owned)

| Column            | Type        | Notes                              |
|-------------------|-------------|------------------------------------|
| `Id` (PK)         | `Guid`      |                                    |
| `UserId` (FK)     | `Guid`      | `ON DELETE CASCADE`                |
| `AyahId` (FK)     | `int`       | `ON DELETE RESTRICT`               |
| `CreatedAt`       | `datetime2` | UTC                                |
| `IsDeleted`       | `bit`       | Soft delete                        |
| `DeletedAt`       | `datetime2` | UTC, nullable                      |

**Indexes**: `UQ_Bookmark_User_Ayah` (unique on `UserId, AyahId` filtered to
`IsDeleted = 0` so a user can re-bookmark after removing).

**Validation**: hard cap of 1,000 active bookmarks per user enforced in
`BookmarkService.AddAsync` (returns `409 Conflict` with RFC 7807 problem type
`https://quraan.app/problems/bookmark-limit-reached`).

---

### `LastReadPosition` (user-owned, 1 per user)

| Column            | Type        | Notes                              |
|-------------------|-------------|------------------------------------|
| `UserId` (PK, FK) | `Guid`      | `ON DELETE CASCADE`                |
| `SurahId` (FK)    | `tinyint`   |                                    |
| `AyahNumberInSurah` | `smallint` |                                   |
| `UpdatedAt`       | `datetime2` | UTC                                |

Single row per user. Anonymous last-read lives in browser `localStorage`
(see R-13) and never reaches this table until sign-in.

---

## Entity-relationship diagram (text form)

```
Surah ──< Ayah ──< AyahTranslation >── Translation
              │
              ├──< TafsirEntry >── TafsirSource
              │
              └──< Bookmark >── ApplicationUser ──< RefreshToken
                                  │
                                  └── 1 ──── LastReadPosition

Reciter   (referenced by AudioController logic; no FK from content tables)
```

---

## Mapping notes

- **EF Core configurations** live in
  `backend/src/Quraan.Infrastructure/Persistence/Configurations/*.cs`, one
  `IEntityTypeConfiguration<T>` per entity. `QuraanDbContext.OnModelCreating`
  scans the assembly for them.
- **Collations**: the database default is `Latin1_General_100_CI_AS_SC_UTF8`
  (UTF-8 with case-insensitive English collation). `nvarchar` columns
  containing Arabic text rely on the `nvarchar` Unicode storage rather than
  collation for correctness.
- **Migrations**: a single `M001_Initial` migration creates the entire
  schema; a `M002_Seed` migration is intentionally NOT used because seeding
  is content-volume work owned by `Quraan.Infrastructure.Seed.*Seeder`
  classes invoked from a `dotnet run --project Quraan.Api -- seed` command.
- **Concurrency**: `ApplicationUser` uses Identity's `ConcurrencyStamp`.
  `LastReadPosition` uses a `rowversion` column (added as `Timestamp`) to
  resolve cross-device write races (newest `UpdatedAt` wins).
- **Soft delete query filter**: `Bookmark` and `RefreshToken` have a global
  query filter `b => !b.IsDeleted` so service code never sees deleted rows
  unless it explicitly opts out via `IgnoreQueryFilters()`.
