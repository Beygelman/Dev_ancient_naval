# v020.7 finalization audit — 5 October 2026

**Functional and final native performance verification: PASS.** Both unchanged
frame gates passed after the final Debug build on a quiet host. Final archive
validation is recorded in the adjacent `FINAL-ARTIFACT-VALIDATION.json`. The earlier
loaded-host failure and candidate artifacts remain preserved. The existing
v020.8 primary work remains intact; no remote merge/promotion is performed.

## Source of truth

| Evidence | Finding |
|---|---|
| Canonical working directory | `C:/__Beygelman/! -11/dev-ancient-naval` |
| Initial canonical branch / HEAD | `main` / `299d99c434475b0b83ef17c30fd1450e978a6788` (v020.6 metadata) |
| Canonical working source | Marker-free, with intentional uncommitted v020.7 and newer v020.8 work |
| Confirmed remote `main` | `faf3ae14a4988b190459c0f1d02c1d790d82c404`: 1,482 merge-marker lines across 99 files |
| Clean gameplay baseline | `b03c5bdc85498f5f5d76f03f4d070adcaa13aa34`, `codex/ancient-naval-v020.7` |
| Existing PR | [#6](https://github.com/Beygelman/Dev_ancient_naval/pull/6), open draft; head is the clean baseline; not updated/merged by this task |
| Isolated reconciliation branch | `codex/v0207-finalization`, separate native Git worktree |

The supplied AGENTS revision 4.0 and finalization plan were read completely.
All supplied handoff hashes matched. The compact handoff ZIP contains those
documents, not source. No cleaned player ZIP was present among the attachments.
Old chat establishes project history, not an exact version cutoff or authority
over later saved rules.

Comparing actual primary file contents to the clean baseline found 1,428 exact
matches, 403 line-ending-only differences, 52 changed files and 104 local-only
files; no baseline source file was missing. Newer pirate setup, scroll/amphora,
counsel UI and v020.8 documentation are intentional work, not conflicts to
discard. No tracked editor cache/bin/obj was found. Full per-file classification
is in the external reconciliation JSON. Before changes, 1,987 primary source
files and 164 historical release/artifact files were hashed; a separate source
backup ZIP was created outside the repository.

Neither the primary HEAD/index nor its gameplay files were reset/rebased or
replaced. A new worktree from b03c5bd is the frozen v020.7 development source.
This avoids importing known-conflicted main or deleting newer local work.
The Windows player export is never used as source authority.

## Changes and preserved contracts

- Adopted the supplied AGENTS guide, with reusable release/source/fixture traps.
- Rewrote current README values from unchanged balance.json; preserved the whole
  previous README under docs/history. Added procedure/audit/milestone notes.
- Corrected the title footer's stale v0.13 literal to read project metadata;
  removed its stale fixed date. Font, art and gameplay stay unchanged.
- Corrected the missing Ukrainian/Dutch View-the-map translation found in a
  settled native outcome capture; added actual translated-button regression.
- Reconciled runtime tests with current rolling-paper timing, acknowledged
  deferred rewards, observed shared-cell cycling and each fixture's saved rules.
  Added a bounded wait for fully opaque victory captures and a disposable runner.
- Disabled active Windows debug symbols, corrected the scratch export path and
  excluded development directories from the PCK. Older presets remain history.
- Added exclusive-output export/package/verifier scripts, inner-PCK checks,
  full runtime hash comparison and stable ZIP ordering/timestamps. Negative
  probes reject PCK flags, aliases, duplicate/unsafe paths and invalid bounds.
- Added two immutable pre-refactor diagnostic save fixtures with provenance and
  hashes. No personal save or valid backup was used for testing.

There is **no Core, balance, scene, world-generation, save DTO or serialized
identifier change** relative to b03c5bd. The one shipped C# difference is the
footer metadata line; two localization dictionaries gain one UI message each. Combat prepare/flight/exact-once impact/counterattack,
RNG/geometry/rule snapshots, fog/radar anonymity and all v020.7 features remain
the baseline implementation. No v020.8 gameplay was backported; no mass style
rewrite, framework, dependency, historical release removal or main merge occurred.

## Executed verification

Host: Windows, .NET SDK 8.0.425, installed Godot 4.7.2 .NET, OpenGL Compatibility,
NVIDIA GeForce RTX 4060 Laptop GPU. Native tests use actual windows/renderer and
explicit disposable --save-file and --ui-settings-file paths.

Debug build after the footer/localization/capture correction: **PASS**, zero warnings/errors.
CoreChecks Release with the two historical fixtures: **PASS**, 1,204,161
assertions in 18 suites, including eight complete AI matches. Production Core
was unchanged after that run. The extracted-source Debug build also passed with zero warnings/errors, and its
Core run repeated all 1,204,161 assertions. Core and native Continue retain historical saved
rules instead of silently substituting the current balance.

The earlier native run passed smoke, battle, effects, sea, optimization,
menu/save/recovery, v020.7 UI/voyage/animation, v020.5 UI/construction, v020.6
tutorial/hints, victory, trade glyph, v020.2 localization and v020.4 layout suites.
The final native regression results are recorded below (localization/layout/outcome
were repeated after the missing translation fix):

| Native suite | Result | Actual log result |
|---|---|---|
| smoke | PASS | 248660 runtime checks (projection, selection, mouse, touch, camera, HUD). |
| battle | PASS | 111 naval runtime checks (legacy-save controls, scroll input, upgrades, stealth animation, hexagon). |
| effects | PASS | 121 effects checks (salvo counts, unchanged damage, smooth movement, wildlife, veterans). |
| sea | PASS | 181 sea-event runtime checks. |
| optimization | PASS | 11 rendering optimization checks; checksum=255671900. |
| menu | PASS | 151 title/color/save/reload checks. |
| ui0207 | PASS | 57 v020.7 native parchment, ready objects, static advice, acknowledgement and modal wheel checks. |
| voyage0207 | PASS | 25 v020.7 stacked selection, welcome and personal voyage checks. |
| animation0207 | PASS | 54 v020.7 retained merchants, diagonal ports, ritual effects, idle batteries and staged solid balloon crash. |
| ui0205 | PASS | 78 v020.5 painted setup, settings, scrolling, introduction and saved-voyage checks. |
| tutorial0206 | PASS | 57 tutorial events, native close-ups, preference, history, external captions and input checks. |
| hints0206 | PASS | 55 v020.6 native guidance, turn confirmation, setting, translation and modal checks. |
| victory | PASS | 191 staged victory/statistics/modal checks (Windows). |
| construction0205 | PASS | 40 construction projection, support art and ruin checks. |
| trade-glyph0204 | PASS | 18 v020.4 cosmetic trade traffic and shared clay/shipyard motif checks. |
| language0202 | PASS | 1265 language bridge, native menus, viewport, preference, lore and dynamic-message checks. |
| ui0204 | PASS | 142 v020.4 modal, translation, scaled input and paid town UI checks. |

An independent native process loaded the pre-refactor Continue fixture and
completed 139 checks. The language-preference process restart completed five
checks. Final-current-voyage process restart completed 139 checks. Extracted-source build is documented
in the artifact verification evidence.

Failures are retained separately. The first native runner rejected an empty
stderr because PowerShell returned null; casting it to string fixed the wrapper,
and no game failure was reported in that smoke log. Initial battle/sea/layout/localization/legacy
tests had obsolete instant-close, deferred-modal, occupant-priority, command-line
language or fixed saved-range assumptions; those harness issues were corrected
and rerun. Victory captures initially sampled the .65-second opacity transition;
the settled outcome is readable and the updated test waits for completion.
No gameplay was changed to make these assertions pass.

## Performance: retained failure and final passed gates

Earlier reconciliation Debug Pangaea gate (before the final footer/localization corrections), four rivals, 1,759 cells, wide view:

| Metric | Measured | Unchanged limit |
|---|---:|---:|
| Pan frame p95 | 59.123 ms | 25 ms |
| Pan maximum | 99.113 ms | 80 ms |
| Hover maximum | 0.673 ms | 16 ms |
| Interaction frame p95 | 29.224 ms | 25 ms |
| Interaction maximum | 64.436 ms | 100 ms |

**FAIL.** The serial runner stopped before its live-fog case. Slow frames showed
no GC or shader compilation spikes; the profile did not establish a specific
production-code regression. Outside tests, WorldOfTanks and an older Ancient
Naval remained running with roughly 65–74% GPU utilization. This is a material
measurement confound, not proof that performance is acceptable. The user was
asked to close those games; no unrelated process was terminated. After the
user confirmed they were closed on 5 October, the final Debug build passed
with zero warnings/errors and both real native cases ran serially without
competing builds/tests. No threshold or performance-harness code was changed.

| Final gate metric | Pangaea / wide view | Oceans / live fog | Limit |
|---|---:|---:|---:|
| Cells / rivals | 1,759 / 4 | 1,759 / 4 | Representative fixture |
| Pan samples | 119 | 119 | Existing harness |
| Pan p95 | 19.776 ms | 17.247 ms | 25 ms |
| Pan max | 22.763 ms | 20.543 ms | 80 ms |
| Hover calls / max | 64 / 1.704 ms | 64 / 0.475 ms | 16 ms |
| Interaction p95 | 21.508 ms | 17.583 ms | 25 ms |
| Interaction max | 61.142 ms | 84.758 ms | 100 ms |
| Result | PASS | PASS | Unchanged |

GPU competition plausibly explains the earlier degradation; this repeat does
not prove universal performance or isolate every host factor. Godot 4.7.2
TimeProcess telemetry is the previous one-second maximum including rendering,
not per-frame C# CPU time ([engine implementation](https://github.com/godotengine/godot/blob/4.7.2-stable/main/main.cpp)); its earlier
82.259 ms cannot identify a gameplay CPU bottleneck. The release gate uses
Stopwatch frame intervals instead. Headless tests, movie captures and older
supplied green measurements cannot substitute for this final native run.

## Export and packaging boundary

Real headless Godot Windows export ran repeatedly from the checkout and an
extracted source snapshot: exporter exit 0, required
self-contained runtime present. The first wrapper then correctly exposed a
verifier assumption about res:// entry names; actual Godot stores relative
resource names. Normalization was repaired without relaxing path checks. The
corrected export wrapper passed: PCK format 4, engine 4.7.2, 168 resources,
required game/config/localization present, no development resource paths.
Native actual clean-package startup EN/UK/NL and map preview each exited 0
with no stderr errors and eight rendered captures. Representative captures,
including the corrected v020.7 footer and fully opaque portrait outcome, were
inspected. EXE FileVersion is 0.20.7.0, ProductVersion 0.20.7.

Exporting the first extracted-source snapshot exposed a recursive evidence leak:
188 resources instead of 168, including source inventories and diagnostic
captures. The source archive now marks release-evidence/ with .gdignore; the
active preset excludes it and the PCK verifier rejects it. A rerun with the
corrected preset/verifier produced exactly the same 168 resource paths as the
clean checkout export. This is a release-only repair, not a simulation change.
The original shipped player archive has 51 development items: Preview map.cmd,
two game/Core PDBs and 48 internal documentation/diagnostic files. They are
absent from the clean package; complete runtime assemblies remain.

Player policy retains EXE/PCK, complete data_Dev_ancient_naval_windows_x86_64
runtime and a minimal launch readme. Development docs/diagnostics, Preview map.cmd,
console wrappers and PDB files are excluded. .NET/Godot/Core runtime DLLs are not
deleted. Source archives include code/assets/config/tests/docs/tooling and
per-file hashes, excluding caches, private settings and recursive release ZIPs.
Historical fixture saves are explicitly reviewed test source, not personal slots.
The first source-policy probe copied all historical diagnostic images and grew
to 217 MB. Corrected snapshots retain engineering text and all required assets,
with selected current captures; old diagnostic screenshots remain unchanged in
Git/historical archives and an exclusion list documents the source ZIP policy.

Earlier candidates keep Windows_PLAYER_CANDIDATE / Source_CANDIDATE names and
their candidate manifest. Final delivery uses an additive `finalized/` directory,
Windows_PLAYER_CLEAN / Source_FINALIZED names and a separate SHA256 manifest.
Hash-bound evidence has scoped Git attributes; stored blob SHA256 was checked
against raw files, including CRLF logs, rather than assuming working-tree hashes
prove a future clone retains those bytes.
Stable ZIP claims apply only to identical inputs and the same compressor;
byte-identical independent Godot/.NET binaries are not promised.

## Remaining work and next scope

Both required native frame gates are complete. The final artifact pair is
revalidated by repeated packaging and independent exact-source extraction,
build/Core/export/native startup; measured post-package results accompany the
canonical delivery in `releases/v020.7/finalized/FINAL-ARTIFACT-VALIDATION.json`.
That sidecar distinguishes the source snapshot from later artifact commits and
records actual archive paths/hashes and primary/history preservation. Original
archives, earlier candidates and the latest pointer remain unchanged. The
isolated branch is local; PR #6 and remote main are not merged or pushed here.

After v020.7 is actually clean and verified, reconcile the preserved v020.8
work against it, review its optional save policies and clipping/interruption
regressions, and plan a safe marker-free main promotion. No next-version code is
implemented by this finalization task.

## Post-commit delivery receipt — 5 October 2026

Final source snapshot: ce6d1e50d82c57b4971d3c28da119607b148e1d1.
Artifact delivery commit: 376c8b3537ef602f386427021d53efe548cab15b on
codex/v0207-finalization, with local annotated tag v020.7-finalized resolving to
that commit. The worktree is clean. All 31 delivered Git blobs/canonical copies
and 30 manifest entries match their raw SHA256; all 164 historical artifacts and
the primary HEAD/index remain unchanged. Only three original primary docs were
updated, with byte-exact previous copies preserved.

PR #6 remains open/draft at b03c5bd; its base main is still faf3ae1. No push/merge
was attempted. The native Git HTTP check timed out; read-only GitHub API metadata
was rechecked successfully. See [the post-commit receipt](../releases/v020.7/POST_FINALIZATION_GIT_RECEIPT.json).
This receipt is outside the frozen archives, avoiding self-referential commit
hashes or changes to already verified source/player ZIPs.
