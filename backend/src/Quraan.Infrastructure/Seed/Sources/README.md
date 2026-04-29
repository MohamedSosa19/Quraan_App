# Seed Sources (READ ME BEFORE RUNNING `dotnet run -- seed`)

These three files are NOT committed to source control because each is bound by
a redistribution licence we want to respect. They MUST be downloaded once and
verified against `expected.sha256` before the seeder will run.

| File | Where to download |
|------|-------------------|
| `quran-uthmani.txt` | <https://tanzil.net/download/> — choose **Quran Text → Uthmani → Plain text (with diacritics)** |
| `en.sahih.txt` | <https://tanzil.net/download/> — choose **Translation → English → Saheeh International → Plain text** |
| `en-tafisr-ibn-kathir.json` | Pin a specific commit of <https://github.com/spa5k/tafsir_api> and grab `tafsir/en-tafisr-ibn-kathir.json` from `db/`. Record the commit SHA in `PROVENANCE.md`. |

After downloading:

```bash
# Recompute the digests
sha256sum quran-uthmani.txt en.sahih.txt en-tafisr-ibn-kathir.json > expected.sha256
# (or on Windows: CertUtil -hashfile <name> SHA256)
```

Commit `expected.sha256` (NOT the source files) so CI fails closed if the
binary on disk drifts from the digest the team agreed on (Constitution I).

The pipe-delimited format Tanzil uses is `surah|ayah|text` per line, with one
final blank line — `QuranSeeder` parses it accordingly. The Ibn Kathir JSON is
an array of `{sura, aya, text}` objects — `TafsirSeeder` parses it accordingly.
