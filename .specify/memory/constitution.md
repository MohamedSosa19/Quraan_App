<!--
SYNC IMPACT REPORT
==================
Version change: 0.0.0 → 1.0.0
Bump rationale: Initial ratification of project constitution (template → concrete).

Modified principles:
- [PRINCIPLE_1_NAME] → I. Quran Integrity & Spiritual Usability
- [PRINCIPLE_2_NAME] → II. Multi-Tenant Isolation (Single-Brand Tenants)
- [PRINCIPLE_3_NAME] → III. BYOK AI & Secret Hygiene (NON-NEGOTIABLE)
- [PRINCIPLE_4_NAME] → IV. Performance, Streaming & Partial Offline
- [PRINCIPLE_5_NAME] → V. API Consistency & Versioning

Added sections:
- Product Truth (preamble)
- Tech Stack & Data Constraints
- Domain Modules & Development Workflow
- Governance

Removed sections: none (all template placeholders replaced)

Templates requiring updates:
- ⚠ .specify/templates/plan-template.md (pending: align Constitution Check gate to new principles)
- ⚠ .specify/templates/spec-template.md (pending: ensure tenant + RTL + BYOK constraints surfaced)
- ⚠ .specify/templates/tasks-template.md (pending: add task categories for tenancy isolation, RTL, secret handling)
- ⚠ README.md (pending: reflect product truth + stack)

Deferred TODOs:
- TODO(RATIFICATION_DATE): Confirm original adoption date — provisionally set to 2026-04-07.
-->

# QuraanKareem.app Constitution

QuraanKareem.app is a Quran-centric multi-tenant SaaS platform delivering Quran text, audio
recitation, Hadith, and Azkar, with AI-powered features built on a strict BYOK (Bring Your Own
Key) model. Each tenant represents a single brand (organization or individual) with white-label
branding. Billing is out of scope for the MVP. The platform prioritizes performance, simplicity,
and spiritual usability above all else.

## Core Principles

### I. Quran Integrity & Spiritual Usability

Quran text MUST be 100% accurate and verified against an authoritative source before ingestion;
the canonical Quran dataset is read-only and immutable in production. Arabic MUST be a
first-class citizen: full RTL support is mandatory in every UI surface, API response, and
stored artifact. The product experience MUST optimize for reverence and clarity over feature
density. Rationale: any textual error or RTL regression breaks user trust in a religious
context and is not recoverable through patches alone.

### II. Multi-Tenant Isolation (Single-Brand Tenants)

Every persisted resource MUST belong to exactly one tenant, and every query MUST be
tenant-scoped at the data layer (no cross-tenant reads or writes). A tenant represents exactly
one brand; multi-brand tenants are prohibited. Branding configuration is stored per tenant and
applied at request time. User-owned data MUST follow a hard delete policy on user request — no
soft-delete tombstones for personal data. Logs are append-only and immutable. Rationale:
white-label trust and GDPR-style deletion guarantees require strict isolation and auditability.

### III. BYOK AI & Secret Hygiene (NON-NEGOTIABLE)

The platform MUST NOT provide or proxy its own AI provider keys; all AI features operate
under BYOK using tenant-supplied credentials for OpenAI or Gemini. AI keys MUST be stored
exclusively in Supabase Vault, NEVER in plain text, NEVER in application tables, NEVER in
logs, and NEVER returned in API responses. AI query history MAY be stored but MUST exclude
secret material. Rationale: BYOK shifts cost and trust boundaries to tenants and removes a
catastrophic blast radius from the platform.

### IV. Performance, Streaming & Partial Offline

Audio recitation MUST be streamed from Bunny CDN by default; full audio downloads are
disabled unless explicitly requested by an authorized tenant feature. Audio files MUST be
referenced via CDN URLs and MUST NOT be stored as database blobs. The client MUST support
partial offline operation via cached Surahs so that previously read content remains available
without network. Rationale: low-latency recitation and resilience in poor-connectivity regions
are core to spiritual usability.

### V. API Consistency & Versioning

All external APIs MUST be RESTful and versioned under `/api/v1/...`. Every response MUST
conform to the canonical envelope:

```json
{ "success": true, "data": {}, "error": null }
```

Breaking changes require a new version prefix (`/api/v2/...`); silent breaking changes within
a version are prohibited. Rationale: a stable, predictable contract is required for
white-label clients building on top of the platform.

## Tech Stack & Data Constraints

Authoritative stack (changes require a constitution amendment):

- Frontend: Angular (with AntiGravity / Gemini 3 Pro tooling)
- Backend: ASP.NET Core for primary domain APIs
- AI Service: FastAPI, isolated from main backend, AI-only responsibilities
- Database: Supabase (PostgreSQL)
- Auth: Supabase Auth
- Object Storage: Supabase Storage
- CDN: Bunny (audio delivery)
- Hosting: Bunny Magic Containers
- AI Providers: OpenAI, Gemini (BYOK only)

Data rules:

- The Quran dataset (Ayahs, translations, recitations metadata) is read-only in production.
- Persistable user data: preferences (theme, reciter, last-read position), AI query history
  (without secrets), tenant branding configuration.
- AI keys MUST live only in Supabase Vault.
- Audio MUST be referenced by CDN URL, never as a DB blob.

## Domain Modules & Development Workflow

The system is organized into the following bounded modules; new modules require an
amendment: **Quran**, **Audio**, **Hadith**, **Azkar**, **AI**, **Auth**, **Branding**,
**Analytics**.

Workflow expectations:

- Every feature spec MUST identify the affected module(s) and tenant-scoping strategy.
- Every PR touching data access MUST demonstrate tenant isolation (test or review note).
- Every PR touching AI MUST confirm no key material is logged, persisted outside Vault, or
  returned to clients.
- Every UI PR MUST verify RTL rendering for Arabic content.
- Logs MUST be treated as immutable; no migration may rewrite historical log records.

## Governance

This constitution supersedes all other development practices, style guides, and informal
conventions. Conflicts MUST be resolved in favor of the constitution.

Amendment procedure: amendments require (1) a written proposal referencing the affected
principles, (2) review and approval by the project owner, and (3) a synchronized update of
dependent templates (`plan-template.md`, `spec-template.md`, `tasks-template.md`) and runtime
guidance docs in the same change set.

Versioning policy (semantic):

- MAJOR: backward-incompatible removal or redefinition of a principle or governance rule.
- MINOR: addition of a new principle, module, or materially expanded section.
- PATCH: clarifications, wording fixes, and non-semantic refinements.

Compliance review: every PR review MUST verify constitutional compliance for the principles
it touches. Complexity or deviations MUST be explicitly justified in the PR description and
approved by the project owner.

**Version**: 1.0.0 | **Ratified**: 2026-04-07 | **Last Amended**: 2026-04-07
