# Feature Specification: Quran & Islamic Companion Web App (MVP)

**Feature Branch**: `001-quran-mvp`
**Created**: 2026-04-27
**Status**: Draft
**Input**: PM brief — locked MVP scope: Quran reading (Arabic + English translation),
search, per-Surah audio with basic Ayah highlight, Tafsir Ibn Kathir per Ayah,
bookmarks, optional JWT-based authentication, and dynamic Arabic/English UI
toggle with RTL support. Out of scope: Hadith, Azkar, Madhahib, full offline mode,
multiple Tafsir sources, Elasticsearch.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Read the Quran in Arabic with English Translation (Priority: P1)

A visitor opens the app, browses the list of all 114 Surahs, opens any Surah,
and reads its Ayahs side-by-side in Arabic (Uthmanic script) and English
translation. They can move between Surahs and jump within a Surah without
having to sign in.

**Why this priority**: This is the core value proposition. Without it, nothing
else in the app matters. It is the smallest possible MVP that delivers spiritual
value to every persona — Daily Reader, Learner, and Listener.

**Independent Test**: Open the app as an anonymous visitor, locate Surah Al-Fatiha
in the Surah list, tap it, confirm the seven Ayahs render in Arabic with English
translation, then navigate to the next Surah (Al-Baqarah) and confirm its first
Ayah is visible. No account, audio, search, or bookmark feature is required.

**Acceptance Scenarios**:

1. **Given** a fresh visitor on the home screen, **When** they open the Surah
   list, **Then** all 114 Surahs are listed with ordinal number, Arabic name,
   transliterated name, English name, and Ayah count.
2. **Given** a visitor viewing the Surah list, **When** they select a Surah,
   **Then** that Surah's Ayahs render with Arabic text and aligned English
   translation, and the Surah header shows its name and revelation metadata.
3. **Given** a visitor reading a Surah, **When** they invoke "next Surah" or
   "previous Surah", **Then** the next/previous Surah opens at its first Ayah.
4. **Given** a visitor reading a long Surah, **When** they enter an Ayah number,
   **Then** the view scrolls to that Ayah within the same Surah.

---

### User Story 2 - Use the App in Arabic or English with RTL Support (Priority: P1)

A visitor switches the user-interface language between Arabic and English from a
control in the chrome. The page direction flips between RTL and LTR accordingly.
The Quranic Arabic text and the chosen English translation always render in
their native script regardless of the UI language.

**Why this priority**: Half the target audience reads right-to-left. An
English-only chrome excludes Arabic-speaking users on day one; an Arabic-only
chrome excludes English-speaking learners. The two languages must ship together,
and the toggle must work without a page reload.

**Independent Test**: With the app loaded in English (LTR), switch the UI
language to Arabic and confirm: chrome strings appear in Arabic, layout direction
flips to RTL, the Quran reading view continues to show Arabic text and English
translation as before. Switch back to English and confirm restoration. No login
required.

**Acceptance Scenarios**:

1. **Given** the app is loaded in English, **When** the user toggles language to
   Arabic, **Then** all UI labels appear in Arabic and the page direction is
   right-to-left, with no full-page reload.
2. **Given** the app is loaded in Arabic, **When** the user toggles language to
   English, **Then** all UI labels appear in English and the page direction is
   left-to-right, with no full-page reload.
3. **Given** a user has previously selected Arabic, **When** they revisit the
   app, **Then** the app opens in Arabic without requiring them to toggle again.
4. **Given** the UI language is English, **When** the user opens any Surah,
   **Then** the Quranic Arabic text still renders in Arabic script and the
   translation still renders in English.

---

### User Story 3 - Listen to a Surah's Recitation (Priority: P2)

A visitor opens any Surah, presses Play, and hears the recitation of that Surah
streamed from an external Quran audio source. They can pause and resume. When
the audio source provides per-Ayah timing data, the currently-playing Ayah is
visually highlighted.

**Why this priority**: Audio is a major draw for the Listener persona and
differentiates the app from a static reader, but the reading view (US1) and
language toggle (US2) must be in place first.

**Independent Test**: Open Surah An-Naba, press Play, confirm audio starts
within a few seconds, press Pause and confirm audio halts, press Play again and
confirm playback resumes from the same position. If timing data is available,
confirm the highlighted Ayah advances as audio plays.

**Acceptance Scenarios**:

1. **Given** a user reading any Surah, **When** they press Play on the audio
   control, **Then** recitation of that Surah begins streaming from an external
   Quran audio source.
2. **Given** audio is playing, **When** the user presses Pause, **Then** audio
   halts and the control switches to a Resume affordance.
3. **Given** audio is paused, **When** the user presses Resume, **Then** playback
   continues from the position at which it was paused.
4. **Given** the audio source provides per-Ayah timing markers, **When** audio
   advances past an Ayah's start time, **Then** the new current Ayah is
   visually highlighted in the reading view.
5. **Given** the audio source is unreachable or returns an error, **When** the
   user presses Play, **Then** a user-visible error message and a Retry control
   are shown instead of failing silently.

---

### User Story 4 - Find a Surah or Ayah by Searching (Priority: P2)

A user types a query (a Surah name, a fragment of an English translation, or
Arabic Ayah text) and receives a list of matching Surahs and Ayahs. Selecting
any result opens the corresponding Surah at the matched Ayah.

**Why this priority**: Essential for the Learner persona who comes looking for a
specific Ayah, but a fully usable app exists without it (US1 + US2).

**Independent Test**: Type the English word "mercy" and confirm Ayahs containing
"mercy" appear with their Surah and Ayah reference. Type "Yaseen" and confirm
Surah Ya-Sin appears. Type Arabic "الرحمن" (with or without diacritics) and
confirm relevant Ayahs/Surahs appear.

**Acceptance Scenarios**:

1. **Given** a user with a query box, **When** they enter a Surah name in
   English, Arabic, or transliteration, **Then** matching Surahs appear in the
   results, ranked by name match.
2. **Given** a user with a query box, **When** they enter Arabic Ayah text,
   **Then** matching Ayahs appear with their Surah and Ayah reference, and
   diacritics in the source text are not required to match.
3. **Given** a user with a query box, **When** they enter English translation
   text, **Then** matching Ayahs appear with their Surah and Ayah reference.
4. **Given** a search results list, **When** the user selects a result, **Then**
   the matching Surah opens scrolled to the matched Ayah.
5. **Given** a query that matches nothing, **When** the user submits, **Then**
   a clear "no results" message is shown.

---

### User Story 5 - Read Tafsir for an Ayah (Priority: P3)

A user reading any Ayah opens the Tafsir Ibn Kathir interpretation for that
specific Ayah and reads the explanation, with attribution shown.

**Why this priority**: Important depth feature for the Learner persona, but the
core read-and-listen flow is functional without it.

**Independent Test**: Open Surah Al-Fatiha, tap the Tafsir affordance on Ayah 2,
confirm the Tafsir Ibn Kathir text for that Ayah appears with author/source
attribution. Tap an Ayah for which Tafsir is unavailable and confirm a
"not available" message appears.

**Acceptance Scenarios**:

1. **Given** a user reading any Ayah, **When** they open the Tafsir view for
   that Ayah, **Then** the Tafsir Ibn Kathir interpretation tied to that Ayah
   is shown with the source name displayed alongside.
2. **Given** the Tafsir view is open, **When** the user moves to a neighboring
   Ayah, **Then** the Tafsir content updates to that Ayah's interpretation.
3. **Given** an Ayah has no Tafsir entry on file, **When** the user opens the
   Tafsir view for it, **Then** a "Tafsir not available for this Ayah" message
   is displayed instead of a blank panel.

---

### User Story 6 - Create an Account and Sign In (Priority: P3)

A visitor creates an account with email and password, signs in, and remains
signed in across browser sessions until they sign out. Reading, audio, search,
and Tafsir continue to work whether or not they are signed in.

**Why this priority**: Required gateway for cross-device personalization
(US7 — bookmarks). Not required for the read-and-listen MVP.

**Independent Test**: Open the app anonymously and confirm Quran reading still
works. Register a new account with email + password, sign in, close and reopen
the browser, confirm still signed in. Sign out and confirm signed-out state.

**Acceptance Scenarios**:

1. **Given** a visitor on the sign-in screen, **When** they choose register and
   submit a valid email and a password meeting policy, **Then** the account is
   created and they are signed in.
2. **Given** a returning user with credentials, **When** they sign in with
   correct email and password, **Then** they enter the signed-in state.
3. **Given** a signed-in user, **When** they close and reopen the browser
   within the session lifetime, **Then** they remain signed in without
   re-entering credentials.
4. **Given** a signed-in user, **When** they sign out, **Then** any
   authenticated-only views (bookmarks list) become inaccessible until next
   sign-in, and Quran reading remains available.
5. **Given** a user submitting invalid credentials, **When** they attempt to
   sign in, **Then** a clear error message is shown without revealing whether
   the email exists.

---

### User Story 7 - Bookmark Ayahs and Revisit Them (Priority: P3)

A signed-in user bookmarks an Ayah while reading, then later opens their
bookmarks list and jumps directly back to any bookmarked Ayah from any device
they sign into.

**Why this priority**: A high-value retention feature for the Daily Reader
persona, but it depends on US6 (auth) and the read-and-listen flow has shipped
value without it.

**Independent Test**: Sign in, open Surah Al-Mulk, bookmark Ayah 1, navigate
elsewhere, open the Bookmarks list, confirm Ayah Al-Mulk:1 is listed, tap it,
confirm the app opens Surah Al-Mulk scrolled to Ayah 1. Sign in on a second
device and confirm the same bookmark is present.

**Acceptance Scenarios**:

1. **Given** a signed-in user reading an Ayah, **When** they tap the Bookmark
   affordance on that Ayah, **Then** the Ayah is added to their bookmarks and
   the affordance reflects the bookmarked state.
2. **Given** a signed-in user with at least one bookmark, **When** they open
   the Bookmarks list, **Then** all their bookmarked Ayahs appear with Surah
   name and Ayah number.
3. **Given** a signed-in user viewing their bookmarks list, **When** they
   select a bookmark, **Then** the matching Surah opens scrolled to that Ayah.
4. **Given** a signed-in user, **When** they remove a bookmark, **Then** the
   Ayah is no longer in their bookmarks list.
5. **Given** a signed-in user with bookmarks on Device A, **When** they sign in
   on Device B, **Then** the same bookmarks are visible on Device B.
6. **Given** an anonymous visitor, **When** they attempt to bookmark an Ayah,
   **Then** they are prompted to sign in or register before the bookmark is
   saved.

---

### Edge Cases

- **Audio source unreachable**: External audio API down or rate-limited — the
  Play action surfaces an error with a Retry control; the rest of the app
  remains usable.
- **Audio without Ayah timing**: Source returns a single audio file without
  per-Ayah markers — playback works; no Ayah is highlighted, and the UI does
  not show a misleading static highlight.
- **Language switched mid-audio**: User toggles UI language while audio is
  playing — playback continues uninterrupted; only chrome strings and direction
  change.
- **Search query with diacritics vs without**: User searches "الرحمن" while the
  source text is "الرَّحْمَٰنِ" — both forms match.
- **Search empty result**: A query returns zero matches — a clear no-results
  message is shown, not an empty silent list.
- **Tafsir gap**: A specific Ayah has no Ibn Kathir entry — the Tafsir panel
  displays a "not available" message rather than a blank state or error.
- **Bookmark by anonymous user**: An anonymous visitor taps Bookmark — the app
  prompts them to sign in or register; no bookmark is silently lost.
- **Bookmark cap**: A signed-in user with hundreds of bookmarks opens the
  bookmarks list — list is paginated/scrollable and remains performant.
- **Concurrent sign-in on two devices**: User signed in on two devices adds a
  bookmark on Device A — Device B reflects the new bookmark on its next refresh
  of the bookmarks view.
- **Slow mobile network**: User on a slow connection opens a long Surah —
  initial Ayahs render quickly while the rest stream/lazy-load below.
- **Last-read recall**: A returning user (anonymous or authenticated) opens the
  app — the home view offers a "Continue from Surah X, Ayah Y" affordance.
- **Sign-out clears authenticated views**: User signs out — bookmarks list is
  no longer accessible; cached chrome state does not leak the previous user's
  bookmarks.

## Requirements *(mandatory)*

### Functional Requirements

**Quran Reading (US1)**

- **FR-001**: System MUST present a list of all 114 Surahs, each showing its
  ordinal number, Arabic name, transliterated name, English name, and Ayah
  count.
- **FR-002**: System MUST display every Ayah within a Surah in Arabic Uthmanic
  script alongside its English translation.
- **FR-003**: Users MUST be able to navigate to the next or previous Surah from
  within the reading view.
- **FR-004**: Users MUST be able to jump to a specific Ayah number within the
  current Surah.
- **FR-005**: System MUST display Surah header metadata (Arabic name,
  transliterated name, English name, revelation place, Ayah count) at the top
  of the reading view.

**Multi-language UI (US2)**

- **FR-006**: System MUST allow users to switch the user-interface language
  between Arabic and English at any time without a full page reload.
- **FR-007**: System MUST set page direction to right-to-left when the UI
  language is Arabic and to left-to-right when it is English.
- **FR-008**: System MUST persist the chosen UI language across sessions for
  the same device, and across devices for signed-in users.
- **FR-009**: Quranic Arabic text MUST always render in Arabic script and the
  English translation MUST always render in English regardless of UI language.

**Audio Playback (US3)**

- **FR-010**: Users MUST be able to start, pause, and resume recitation for
  any Surah from the reading view.
- **FR-011**: Audio playback MUST stream from an external Quran audio source
  (e.g., Quran.com API, Al Quran Cloud API) without requiring full download
  before playback begins.
- **FR-012**: When per-Ayah timing markers are available from the audio source,
  the system MUST highlight the currently-playing Ayah in the reading view and
  MUST update the highlight as playback advances.
- **FR-013**: When per-Ayah timing markers are unavailable, the system MUST
  still allow play/pause/resume without highlighting any Ayah and MUST NOT
  display a stale or misleading highlight.
- **FR-014**: System MUST surface a user-visible error and a Retry control if
  the audio source is unreachable, returns an error, or stops mid-stream.

**Search (US4)**

- **FR-015**: Users MUST be able to search by Surah name in Arabic, English,
  or transliteration and receive matching Surahs in the results.
- **FR-016**: Users MUST be able to search Ayah text in Arabic and English and
  receive matching Ayahs with their Surah and Ayah reference.
- **FR-017**: Search MUST normalize Arabic diacritics (tashkeel) so that
  queries without diacritics match text containing them.
- **FR-018**: Search MUST be case-insensitive for English queries.
- **FR-019**: Selecting a search result MUST open the corresponding Surah
  scrolled to the matched Ayah.
- **FR-020**: When a query produces no matches, the system MUST display a
  "no results" message rather than an empty silent list.

**Tafsir (US5)**

- **FR-021**: For each Ayah, users MUST be able to open Tafsir Ibn Kathir
  interpretation tied to that specific Ayah.
- **FR-022**: System MUST display the Tafsir source name (Ibn Kathir) and
  attribution alongside the Tafsir content.
- **FR-023**: When no Tafsir entry exists for an Ayah, the system MUST display
  a clear "Tafsir not available for this Ayah" message.

**Authentication (US6)**

- **FR-024**: System MUST allow users to register an account using email and
  password.
- **FR-025**: System MUST allow registered users to sign in with email and
  password.
- **FR-026**: System MUST keep signed-in users signed in across browser
  sessions until they explicitly sign out or the session expires.
- **FR-027**: System MUST allow users to use Quran reading, multi-language UI,
  audio playback, search, and Tafsir without an account.
- **FR-028**: System MUST hash and salt user passwords before storing them and
  MUST never store, transmit, or write plaintext passwords to logs.
- **FR-029**: System MUST return generic sign-in failure messages that do not
  disclose whether a given email is registered.

**Bookmarks (US7)**

- **FR-030**: Signed-in users MUST be able to add and remove a bookmark on any
  Ayah.
- **FR-031**: Signed-in users MUST be able to view a list of all their
  bookmarks, each showing the Surah name and Ayah number.
- **FR-032**: Selecting a bookmark MUST open the corresponding Surah scrolled
  to the bookmarked Ayah.
- **FR-033**: Bookmarks MUST be tied to the user account and MUST be visible on
  any device the user signs into.
- **FR-034**: Anonymous users attempting to bookmark MUST be prompted to sign
  in or register; the app MUST NOT silently discard the action.

**Personalization**

- **FR-035**: System MUST track the last-read position (Surah and Ayah) for
  each user — per device for anonymous users, per account for signed-in users —
  and offer a "continue reading" affordance on the home view when one exists.

**Content Integrity**

- **FR-036**: System MUST source Quran text from a single canonical edition
  and MUST display attribution for the translation edition and the Tafsir
  edition in use.

### Key Entities

- **Surah**: An ordered chapter of the Quran. Attributes: ordinal number (1–114),
  Arabic name, transliterated name, English name, revelation place, Ayah count.
- **Ayah**: A verse within a Surah. Attributes: Surah reference, Ayah number,
  Arabic text (with diacritics), normalized Arabic text (for search),
  associated translation text(s).
- **Translation**: A specific English (or other-language) rendering of an Ayah.
  Attributes: Ayah reference, language, source/edition name, attribution, text.
- **Tafsir Source**: A scholarly interpretation work. Attributes: name (e.g.,
  "Tafsir Ibn Kathir"), author, language, attribution.
- **Tafsir Entry**: An interpretation tied to one Ayah. Attributes: Ayah
  reference, Tafsir Source reference, body text.
- **Reciter**: A Quran reciter providing audio. Attributes: name, biographical
  metadata, identifier mapping to the external audio source.
- **Audio Recitation**: A recitation of a single Surah by a single Reciter.
  Attributes: Surah reference, Reciter reference, audio stream URL, optional
  per-Ayah timing markers.
- **User Account**: A registered user. Attributes: identifier, email, hashed
  password, display name (optional), preferred UI language, created date.
- **Bookmark**: A saved Ayah for a user. Attributes: User reference, Ayah
  reference, created timestamp.
- **Last-Read Position**: The most recently read Ayah for a user or device.
  Attributes: User-or-device reference, Surah reference, Ayah reference, last
  updated timestamp.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 95% of Quran reading actions (open Surah list, open Surah,
  navigate between Ayahs) complete display of content within 1 second on a
  typical mobile network.
- **SC-002**: 95% of search queries return results in under 1 second from
  query submission, including Arabic-text searches with diacritic
  normalization.
- **SC-003**: Audio playback begins producing sound within 2 seconds of the
  user pressing Play on a typical mobile network.
- **SC-004**: A first-time visitor can find any specific Surah and start
  reading it within 60 seconds of landing on the home screen, without
  external instruction.
- **SC-005**: A returning visitor can resume reading from their last position
  within 2 interactions (taps or clicks) of opening the app.
- **SC-006**: 100% of displayed Quranic Ayahs match the canonical Arabic text
  source character-for-character.
- **SC-007**: Both the Arabic (RTL) and English (LTR) experiences pass an
  accessibility audit at WCAG 2.1 AA, including correct text direction,
  contrast, and keyboard operability of all interactive controls.
- **SC-008**: A signed-in user sees a newly added or removed bookmark
  reflected on a second signed-in device within 5 seconds.
- **SC-009**: Switching the UI language between Arabic and English completes
  visually within 500 ms and does not interrupt audio playback if any.
- **SC-010**: Zero stored or logged plaintext passwords across all
  environments (verified by automated audit).

## Assumptions

- **Canonical Quran text**: A single canonical Arabic Quran edition (e.g., the
  Tanzil/Madinah Mushaf in Hafs script) will be selected and used for the
  entire app. Edition selection is finalized in the plan phase.
- **English translation**: A single mainstream English translation (e.g.,
  Saheeh International or another permissively licensed edition) is selected
  in the plan phase; multiple translations are out of scope for MVP.
- **Tafsir source**: Tafsir Ibn Kathir is the only Tafsir source included in
  MVP, sourced from a single edition with proper attribution.
- **Reciter**: A single default reciter (e.g., Mishary Rashid Alafasy) is used
  in MVP, sourced from one of the listed external audio APIs (Quran.com or
  Al Quran Cloud). A reciter-selection UI is out of scope for MVP.
- **Per-Ayah audio timing**: Used opportunistically when the chosen audio
  source provides Ayah-level timestamps. If unavailable, playback works
  without highlighting (US3 acceptance scenario 4 is conditional).
- **Authentication**: Email + password only. Social/SSO sign-in, email
  verification flows, and password-reset-by-email are noted assumptions for
  MVP — password reset MAY be addressed at the plan phase if low-cost; full
  email verification is out of scope.
- **Bookmarks**: Bookmarks require an authenticated account. Anonymous
  device-local bookmarks are out of scope to keep the bookmark sync model
  simple and trustworthy.
- **Last-read position**: Tracked automatically as the user reads (no manual
  "save place" action). Persisted per device for anonymous users and per
  account for signed-in users.
- **Search semantics**: Arabic diacritics treated as optional in queries;
  English queries case-insensitive; substring/word-boundary matching across
  Surah names and Ayah text. Ranked search (relevance scoring) beyond simple
  match is out of scope.
- **Platform**: Web application only. Native mobile apps and full offline
  support are out of scope for MVP. Modern evergreen browsers are assumed.
- **Network**: Users are assumed to have a network connection capable of
  streaming audio when the audio feature is in use; reading and search remain
  usable on slow connections.
- **Admin tooling**: No admin UI is in MVP scope. Quran, translation, Tafsir,
  and reciter content are loaded once via a seeded data import.
- **Content licensing**: All bundled content (translation edition, Tafsir
  edition, reciter audio attribution) is verified to be permissively
  licensed or properly licensed before launch.

## Out of Scope (MVP)

- Hadith collections, Azkar (supplications), Madhahib (school-of-thought)
  content.
- Multiple Tafsir sources beyond Ibn Kathir.
- Multiple reciter selection UI.
- Full offline mode (e.g., service-worker-cached Quran for use without a
  network).
- Elasticsearch or similarly advanced ranked-search engines.
- Admin Panel for managing content or users.
- Social / SSO authentication (Google, Facebook, Apple, etc.).
- Email verification and email-based password reset flows (reset MAY be
  reconsidered at plan phase if trivially cheap; otherwise deferred).
- Anonymous (device-local) bookmarks.
- Native mobile applications (iOS/Android).
