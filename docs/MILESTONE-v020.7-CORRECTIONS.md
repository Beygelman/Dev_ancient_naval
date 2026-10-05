# v020.7 — requested correction, 5 October 2026

## Source and scope

The correction starts from frozen finalization commit
`376c8b3537ef602f386427021d53efe548cab15b`, on the isolated
`codex/v0207-corrections` branch. The gameplay baseline remains
`b03c5bdc85498f5f5d76f03f4d070adcaa13aa34`. The canonical primary working
tree contains intentional newer v020.8 work; it is not the source of a wholesale
replacement for this release. No reset, rebase or historical archive replacement
is part of this correction. The game and package version remain **v020.7**.

The latest user explicitly authorized the missing interface items identified in
`V0207_LAST_THREE_REQUESTS_AUDIT.md`, optional pirates, centered action captions,
menu translations and reliable decorative merchant travel through lighthouses.
The subsequent user request explicitly adds connected-port income and higher
ship/radar/mortar prices. Other intentional world-generation, balance and
interface placement decisions are preserved. No new ship classes, damage rules
or difficulty scaling are added.

## Implemented changes

- Rounded faction-glazed amphorae use the reference silhouette, curled handles,
  four existing damage stages and existing paired health/class feedback. The
  larger ready-action jug and Mothership floating-city emblem share the art facade.
- Info occupies the rightmost real command sector. Town command layouts no
  longer reserve an invisible upgrade sector. Town labels are black and draw
  above world feedback.
- Capture/treasury papers unfold, fold, then burn. Existing Core effects execute
  once after acknowledgement; canceled or replaced orders cannot commit to a
  new battle. Hidden cosmetic phases do not strand a command. Captions respond
  to the full paper footprint and remain centered below it.
- Information describes the current object and earned capabilities in readable
  groups, without listing unearned upgrade benefits.
- New voyages have an **Include pirates** option, enabled by default. Continue
  restores the saved choice. Historical saves missing this optional field use
  `true`; generated geometry, identity, rules and simulation RNG are retained.
- Menu choices and related hints have Ukrainian and Dutch translations.
- Cosmetic merchants traverse the connected sea-lane graph between port cities,
  including multiple intermediate lighthouses. Each Core link's length limit is
  not a whole-voyage limit. Existing 12–20-second departures and speed remain.
  Route replacement retains active motion and schedules; actual route changes
  replan remaining travel without consuming simulation RNG.
- A port has no base trade income and earns one Thor for every other live
  friendly port city connected through the sea-lane graph. Lighthouse chains
  relay the route; duplicate paths never multiply income. Four linked cities
  earn three each. Defeat, capture, relay loss and forbidden water immediately
  update the derived contribution. Town income remains separate.
- New prices: Support Brig 4→5, Brig 5→6, Galleon 7→9, Kolonel 12→16,
  Granado 16→22; purchasable radar 2→4 and Mothership mortar 10→14. Port 6,
  dock 8, tower 6, lighthouse 6 and the 20% local discount remain unchanged.
- Optional `Ports.ConnectedCityIncome` has historical default false; new
  balance explicitly sets it true and fixed `Income` zero. Old saves retain
  their rules, source amounts and construction receipts. Foreign city counsel
  does not disclose connected-port counts or unobserved trade income.
- Amphora grain endpoints are retained and grouped into two tint submissions.
  Before/after native reference galleries are pixel-identical. This reduces
  managed submissions; it did **not** reduce the renderer's recorded draw-call
  count, so no frame-speed improvement is claimed from grouping alone.

## Verification record

Executed correction checks:

- Debug build: zero errors/warnings; final editor import exits zero.
- Full Release CoreChecks: passed, including 23 historical-save/validation
  checks, eight complete AI matches, 204 optional-pirate checks and 161
  connected-port checks. Fixed a test fixture's obsolete four-Thor budget to
  use the current Support Brig price; historical JSON fixtures are untouched.
- All 24 native runtime cases passed after integration: smoke, battle, effects,
  sea, optimization, menu, ui0207, voyage0207, animation0207, ui0205, tutorial0206,
  hints0206, victory, construction0205, trade-glyph0204, language0202, ui0204,
  story, refinement020, corrections-ui, amphora-corrections, pirates-corrections,
  merchant-corrections and port-economy-ui. The new economy UI suite passes 227
  checks; merchant chains/construction/schedules/RNG pass 30,740 checks.
- Native art inspection covers the reference gallery, current paired health
  damage stages, large ready jug, multiple UI scales, captions and actual
  localized city income. The batching gallery comparison is pixel-identical.
- The canonical primary source builds with zero errors/warnings and its full
  CoreChecks exits zero: 23 save checks, 161 port checks and its retained 187
  v020.8 pirate checks pass. Primary native economy UI (221) and merchants
  (30,740) pass; its newer compact counsel card is preserved.

Frame verification is still pending. The first correction pan p95 was
25.320 ms against the unchanged 25 ms limit. Grouping grain submissions retained
the art but did not reduce recorded draw calls. A later repeat measured p95
36.858 ms and an 805.746 ms pan stall while a separate old player instance was
running. Process/GPU inspection identified that instance; no user application
was terminated. These failures are retained. No thresholds have been relaxed
and no successful corrected frame result is claimed yet.

Earlier failed harness runs are retained: three old expectations assumed the
former command fan, old jug non-overlap and unlimited direct trade links; they
were corrected to test actual approved behavior. One test clicked before its
scroll finished opening and now waits for the real reveal. The first editor
import failed; subsequent imports completed with exit zero.

Required checks: Debug build; Release CoreChecks including historical fixtures
and optional-pirate checks; native correction suites for amphorae, papyrus input,
pirates and merchants; existing native regression suites; serial native frame
gates after the final Debug build; clean Windows export and package inventory;
exact source-archive rebuild/export and player startup; repeatable ZIP hashes.

Post-packaging checks and final archive hashes belong in the delivery's separate
`evidence/FINAL-ARTIFACT-VERIFICATION.md`, because that report necessarily
postdates the immutable source ZIP it verifies.

All automated voyages and interface preferences use explicit disposable paths
outside the source project. The real user save and backup are not test inputs.

## Delivery policy

Corrected Windows/source ZIPs and SHA256 evidence are additive in
`releases/v020.7/corrected-2026-10-05/`. Historical finalized ZIPs and their
manifest remain untouched. Player packages contain the executable, PCK, complete
.NET runtime and minimal player instructions; source/tests/diagnostics/preview
commands/PDBs do not belong inside the player package.

Do not unpack a source tree under the canonical project's release folder:
Godot's C# project defaults can compile nested `.cs` files and introduce duplicate
types. Distribute the source ZIP there and keep any extracted checkout outside
the active source tree.
