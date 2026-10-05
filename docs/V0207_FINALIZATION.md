# Ancient Naval v020.7 — finalization, cleanup and handoff plan

**Release:** `v020.7 — The Sacred Voyage` <br>
**Date baseline:** 4 October 2026 <br>
**Engine:** Godot 4.7.2 .NET <br>
**Language/runtime:** C# / .NET 8 <br>
**Canonical working project:** `C:/__Beygelman/! -11/dev-ancient-naval` <br>
**Repository:** `Beygelman/Dev_ancient_naval`

## 1. Purpose

This document closes v020.7 as a stable historical release and prepares the project for transfer to another AI agent.

The supplied Windows ZIP is already a **compiled delivery**, not a development source tree. Do not attempt to continue feature development from the `.exe`, `.pck` or exported .NET runtime folder. Continue development from the canonical source repository/project and use the Windows ZIP only as a release artifact and behavioral reference.

The supplied development transcript shows that v020.7 reached its intended gameplay scope and passed its final verification. The remaining work for a clean v020.7 is mainly:

1. freeze the v020.7 behavior;
2. clean the Windows player package;
3. preserve diagnostics separately from the player package;
4. repair/clarify Git source-of-truth state;
5. re-run the release gates after any packaging/export change;
6. store the updated `AGENTS.md` with the source.

Do **not** silently pull later v020.8 feature work into v020.7. v020.8 ideas and lessons may inform engineering practice, but they are a separate feature boundary unless the user explicitly requests a backport.

---

## 2. Sources audited

### Supplied Windows archive

`Ancient_Naval_v020.7_Windows.zip`

Observed unpacked layout:

```text
Ancient Naval v020.7/
├─ Ancient Naval.exe
├─ Ancient Naval.pck
├─ Preview map.cmd
├─ READ_ME_RU.md
├─ data_Dev_ancient_naval_windows_x86_64/
│  ├─ Dev_ancient_naval.dll
│  ├─ DevAncientNaval.Core.dll
│  ├─ GodotSharp.dll
│  ├─ .NET runtime DLLs
│  ├─ *.deps.json
│  ├─ *.runtimeconfig.json
│  ├─ *.pdb
│  └─ runtime executables/libraries
└─ docs/
   ├─ MILESTONE-v020.7.md
   └─ diagnostics/v020.7/...
```

Observed archive facts during this review:

- ZIP size: about **81 MiB**.
- Unpacked size: about **195 MiB**.
- Total files: **241**.
- The Windows runtime folder is about **80 MiB** and must remain intact except for optional debug symbols.
- `docs/` is diagnostic/developer evidence, not a runtime dependency.
- two `.pdb` files are debug symbols, not runtime dependencies.
- `Preview map.cmd` is a developer/preview launcher, not required for normal play.

### Development transcript

The transcript records the v020.7 implementation, regression tests, release packaging, architectural decisions and later v020.8 lessons.

### GitHub state checked during this handoff

The clean v020.7 work exists as pull request **#6**:

```text
branch: codex/ancient-naval-v020.7
head:   b03c5bdc85498f5f5d76f03f4d070adcaa13aa34
base:   main
PR:     #6
state:  open, draft, currently mergeable
```

The repository's current `main` content must **not** be treated as a clean source baseline without inspection. During this handoff, `AGENTS.md`, `project.godot` and `export_presets.cfg` on `main` contained unresolved merge-conflict markers and stale version text. The v020.7 branch/commit or the canonical local working project is the safer baseline.

---

## 3. v020.7 release status: what is already complete

The recorded final v020.7 validation reports:

- final .NET Debug build: **0 warnings, 0 errors**;
- **1,204,156 Core assertions across 18 suites**;
- eight complete AI matches;
- 61 v020.7-specific regression checks;
- native UI/input checks with real mouse input;
- English, Ukrainian and Dutch coverage;
- save/Continue compatibility checks using disposable paths;
- native rendering checks in multiple viewport shapes and interface scales;
- final frame-pacing gates passed for Pangaea and live-fog scenarios;
- final Windows export succeeded with product version 0.20.7;
- exported executable passed startup checks for EN, UK, NL and map-preview mode.

Therefore, unless a new defect is reproduced, **v020.7 should be treated as feature-frozen and functionally complete**. Finishing work should not introduce speculative gameplay changes.

### Performance caveat to preserve in evidence

One first live-fog timing sample exceeded the p95 gate (27.007 ms versus a 25 ms gate). A fresh serial run with unchanged code/fixture/threshold passed. The cause of the first spike was not proven. Keep this diagnostic note in developer evidence; do not misrepresent the measured host result as a guarantee for all machines.

---

## 4. Player package cleanup

### Keep in the Windows player package

Keep:

```text
Ancient Naval.exe
Ancient Naval.pck
READ_ME_RU.md

data_Dev_ancient_naval_windows_x86_64/
    all required runtime .dll files
    Dev_ancient_naval.dll
    DevAncientNaval.Core.dll
    GodotSharp.dll
    *.deps.json
    *.runtimeconfig.json
    hostfxr.dll
    hostpolicy.dll
    coreclr.dll
    clrjit.dll
    native runtime libraries
    createdump.exe   # keep unless a separately verified trimmed runtime proves it unnecessary
```

The safe rule is: **do not manually prune runtime DLLs by intuition**. A self-contained .NET/Godot export includes many assemblies that may be loaded conditionally. Saving a few megabytes is not worth creating a machine-specific startup failure.

### Remove from the Windows player package

Remove from the player-facing ZIP:

```text
Preview map.cmd

docs/
    MILESTONE-v020.7.md
    diagnostics/v020.7/**

data_Dev_ancient_naval_windows_x86_64/*.pdb
```

Reasoning:

- `Preview map.cmd` exposes a developer preview mode and is unnecessary for a normal player.
- `docs/diagnostics/**` contains internal logs, `.err`, `.log`, timing evidence and test material. Preserve it in the source/release evidence, not the player folder.
- `.pdb` files are debugging symbols. They are useful for developer crash analysis, but not required to run the released game.

### Keep developer evidence elsewhere

Preserve, but outside the clean player ZIP:

```text
releases/v020.7/
    Ancient_Naval_v020.7_Windows.zip          # clean player package
    Ancient_Naval_v020.7_Source.zip           # source snapshot
    Ancient_Naval_v020.7_Notes_RU.md
    SHA256-v020.7.txt
    diagnostics/ or source/docs/diagnostics/v020.7/
    screenshots/previews
```

The player ZIP and source/evidence ZIP have different purposes. Do not mix them.

---

## 5. Fix the export/package pipeline, not only the existing ZIP

A one-time manual deletion produces a cleaner ZIP, but the source pipeline should also be corrected so future exports do not reintroduce debug/dev files.

### Windows export preset

For the final player release preset:

```ini
dotnet/include_debug_symbols=false
```

Keep the export exclusions at minimum equivalent to:

```text
releases/*
docs/*
tests/*
work/*
```

Also exclude any known local-only/cache/diagnostic directories if they are present in the project.

### Packaging script

The v020.7 packaging script should produce at least two distinct products:

1. **Windows Player ZIP** — executable, PCK, required .NET runtime, user readme only.
2. **Source/Evidence ZIP** — source, docs, tests, diagnostics, release notes; no generated caches, personal saves or credentials.

The packaging step must not copy `docs/diagnostics` back into the Windows player folder after Godot correctly excludes it.

---

## 6. Git/source-of-truth repair before further development

### Critical rule

Do **not** start a new AI session with:

```powershell
git pull
```

before inspecting the repository. The checked `main` contains conflict markers/stale state and can overwrite or confuse the working v020.7 source.

### Preferred recovery order

From the canonical local project:

```powershell
cd 'C:/__Beygelman/! -11/dev-ancient-naval'

git status --short
git branch --show-current
git log --oneline --decorate -10
git remote -v
```

Then verify whether the local source matches v020.7 PR head:

```text
b03c5bdc85498f5f5d76f03f4d070adcaa13aa34
```

If the local tree contains newer intentional work, do not reset it blindly. Preserve it first in a branch or copy.

### Recommended Git outcome

For a clean historical baseline:

1. preserve any uncommitted user work;
2. retain `codex/ancient-naval-v020.7` as the v020.7 source checkpoint;
3. verify/build that checkpoint;
4. resolve `main` deliberately rather than accepting conflict markers;
5. merge or otherwise promote the verified v020.7 state;
6. tag the final source checkpoint, e.g. `v020.7`, only after verification;
7. keep older release archives immutable.

Do not rewrite historical release artifacts merely to make Git look cleaner.

---

## 7. Frozen v020.7 gameplay contract

The following belongs to v020.7 and should not change while cleaning the release.

### Fleet capacity

Weighted slot cost:

| Object | Capacity cost |
|---|---:|
| Mothership | 0 |
| Brig | 1 |
| Support Brig | 1 |
| Galleon | 2 |
| Granado | 3 |
| Kolonel | 4 |
| Balloon | 0 |
| Fixed structures | 0 |

### Ship dismantling

- refund = **40% of the actual paid construction price**;
- round to a whole Thor;
- starting, gifted and free/creative hulls refund zero;
- historical saves without a receipt use the documented compatibility fallback;
- command payout must be applied exactly once.

### Town shipyards and ports

- legal diagonal shore cells may be used for ship production;
- a new port selects the least-cost legal navigable approach toward the nearest owned port/beacon;
- selected `PortCell` is persisted;
- later network changes must not relocate an existing saved port.

### Shared-cell selection

Repeated clicks may cycle only among **currently observed** occupants of the same tile. It must:

- spend no action;
- not reveal hidden objects;
- not override attack/build target priority;
- not mutate Core state.

### Balloon crash

A destroyed balloon:

- reaches a distinct staged sea-impact boundary;
- applies 2 damage to its own underlying cell and the eight nearest neighboring cells;
- can damage friendlies;
- applies damage once;
- respects fog/presentation secrecy;
- can destroy other objects, but presentation must not double-apply Core damage.

### Voyage judgement

Personal ending evidence includes direct kills, structures/strongholds, owned towns, duration, world size, income and difficulty.

Known thresholds:

```text
swift limit = 6 + 4*rivals + 3*sizeIndex
```

- Admiral: at least 8 direct kills and at least twice the strongest rival's direct kills.
- Conqueror: at least 6 constructed towers/beacons and at least 70% of towns.
- Fishing docks may support economic evidence but do not count as military strongholds.

### Languages

Player-facing UI supports:

```text
English
Ukrainian
Dutch
```

All new player-visible strings require complete catalogs unless intentionally proper names.

---

## 8. v020.7 UI/presentation contract

Keep the established visual language:

- worn fibrous papyrus;
- painted/brush-like controls;
- clay/amphora motifs;
- wax/faction ornament language;
- physical roll/fold/burn ceremonies;
- dark ink on paper rather than modern flat UI;
- ancient maritime/faction identity over generic fantasy UI.

Specific v020.7 behavior:

- action amphora opens a side list of ready/useful objects;
- hover text is derived from actual available commands;
- advice content remains static while selecting other objects;
- advice is acknowledged at its bottom with a hand/stamp ceremony;
- welcome/outcome footer actions remain reachable while narrative content scrolls;
- modal mouse-wheel input must not zoom the world behind it;
- end-turn paper folds after the actual commit boundary and returns at the player's next turn;
- opening camera descent is intentionally slower (recorded as 3.2 seconds in v020.7 work);
- lighthouse cones and idle battery headings are cosmetic only;
- merchant traffic must survive ordinary state refresh/staged commands;
- cosmetic effects must not consume simulation RNG.

---

## 9. Engineering rules learned through later versions

These are **engineering lessons**, not automatic v020.7 feature backports.

### Papyrus animation

When implementing later rolling papers:

- keep content at a fixed logical scale;
- reveal it through a clipped host between moving rolls;
- **never squash text/images by animating `Control.Scale` on the content tree**;
- roll thickness/radius may change physically while folding;
- interrupted open/fold awaits must resolve safely;
- hidden scrolls stop processing/animation;
- ceremonial rods/beads/ornaments belong to high-importance papers (welcome, encounter, blessing, outcome), not every utility menu.

### Action menus

- radial/sector menus should contain real actions only;
- do not leave invisible blank sectors merely to force symmetry;
- symmetry should be produced from layout of actual commands;
- decorative brush strokes sit **behind the text**, not as a generic button rectangle.

### Information card direction

For later UI work, the selected-object card direction established after v020.7 is:

- compact lower-left card;
- faction-colored wax seal with cord;
- faction vertical ornament;
- large object name + small level;
- short description first;
- detailed scrollable two-column information below;
- health does not need to be duplicated there if already shown by world HUD;
- observed enemy uses observed faction styling;
- radar-only contact remains neutral/anonymous and must not leak identity/HP.

### Optional world settings

A later optional-pirate setting established a reusable compatibility rule:

- new-voyage generation options are written into saved settings;
- Continue uses the saved composition and must not reroll it;
- absent fields default to historical behavior;
- difficulty may influence initial world composition where explicitly requested, but should not silently alter combat statistics/income.

---

## 10. Verification before declaring the cleaned v020.7 final

Run from the canonical source root.

### Build

```powershell
dotnet build Dev_ancient_naval.csproj --configuration Debug --no-restore
```

Expected: 0 errors. Treat new warnings as review items.

### Core regression

```powershell
dotnet run --project tests/CoreChecks/CoreChecks.csproj --configuration Release
```

At minimum, the v020.7-specific suite and all standard suites must pass.

### Native Godot checks

Use the installed Godot 4.7.2 .NET console executable and the project's existing test arguments. Do not use headless-only results to claim visual correctness.

Important native groups recorded in v020.7 include:

```text
language0202
ui0204
ui0205
tutorial0206
hints0206
victory
construction0205
trade-glyph0204
ui0207
voyage0207
animation0207
```

Run save/preferences tests only with explicit disposable `--save-file=` / settings paths.

### Frame pacing

Run `tools/Check-ReleaseSmoothness.ps1`:

- on the final Debug assembly;
- with a native renderer;
- serially;
- without concurrent CPU-heavy builds/tests.

Do not relax thresholds to force a pass. Investigate a failure and retain failed evidence.

Recorded v020.7 gates:

```text
pan p95 <= 25 ms
pan max <= 80 ms
route hover <= 16 ms
interaction frame p95 <= 25 ms
interaction max <= 100 ms
```

### Export startup

After exporting to a new clean folder, verify the actual exported executable in:

```text
English
Ukrainian
Dutch
map-preview command mode
```

No console/error output should appear for the startup checks.

### Manual smoke test

At minimum:

- New game;
- Continue with a v020.7 save;
- Continue with at least one historical save fixture;
- select/cycle stacked objects;
- build from diagonal town berth;
- build a port and reload its saved berth;
- dismantle a paid ship and a free/start ship;
- destroy a balloon near multiple targets;
- open/close advice, welcome, game menu and result paper;
- verify wheel isolation over modals;
- end turn and watch paper fold/restore;
- verify ready-object amphora list;
- finish victory and defeat flows.

---

## 11. Final release acceptance criteria

v020.7 is ready to freeze when all are true:

- [ ] canonical source checkpoint is known and protected;
- [ ] no unresolved conflict markers in the source being released;
- [ ] `project.godot` reports v020.7;
- [ ] Windows export metadata reports 0.20.7 / v020.7;
- [ ] build passes;
- [ ] Core suites pass;
- [ ] relevant native suites pass;
- [ ] serial frame gates pass or a consciously accepted exception is documented;
- [ ] EN/UK/NL exported startups pass;
- [ ] old-save compatibility is rechecked;
- [ ] player ZIP contains no `docs/diagnostics`, tests, work folders, PDBs or preview script;
- [ ] player ZIP retains the complete required .NET runtime folder;
- [ ] source/evidence archive retains diagnostics and milestone notes;
- [ ] SHA256 manifest is regenerated for the final archives;
- [ ] `AGENTS.md`, `README.md`, `CHANGELOG.md` and milestone docs agree on version and behavior;
- [ ] historical releases remain untouched;
- [ ] final v020.7 tag/checkpoint is created only after the preceding checks.

---

## 12. What the next AI must do first

When this project is handed to another AI, its first actions should be:

1. Read `AGENTS.md` fully.
2. Inspect Git state before changing anything.
3. Confirm it is working from `C:/__Beygelman/! -11/dev-ancient-naval` or a verified clone of the v020.7 source checkpoint.
4. Do not trust current `main` blindly if conflict markers/stale content still exist.
5. Identify whether the requested task targets frozen v020.7 or a new version.
6. Preserve save compatibility and deterministic behavior before refactoring.
7. Use tests and native visual checks as part of the change, not as a packaging afterthought.

If the user says “finish v020.7”, prefer cleanup, bug fixes and release hygiene. New mechanics/UI concepts belong to v020.8+ unless the user explicitly asks to amend v020.7.
