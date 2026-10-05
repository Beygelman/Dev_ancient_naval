# Ancient Naval — project development guide

Version 3.0 · 2 October 2026 · Godot 4.7.2 .NET / C# / .NET 8

This guide adapts the supplied Version 1.0 document to the actual project.
The original remains in `docs/AGENT-GUIDELINES-ORIGINAL.md`. Read this file, `README.md` and the
relevant subsystem before editing. The user's latest requirements determine
scope; historical design notes are context, not new work orders.

## Preserve the playable game

- Refactors preserve rules, visuals, seeded outcomes and existing saves unless
  the user requests a behavior change. State any intentional fixes explicitly.
- Player-facing text supports English, Ukrainian and Dutch. Add complete catalog messages and preserve proper names and numerals. Keep the requested menu signature intact.
- Internal names such as `Invader`, `Togus` and `Garrison` are serialized legacy
  identifiers. Display names are Galleon, Granado and Brig; do not rename enums
  to match the UI without a migration.
- The current map is an irregular **hexagon**, with six straight sides of
  distinct side lengths scaled for 1–4 rivals; 24–30 edges is the three-rival baseline.
  Earlier pentagon specifications are historical.
- Never overwrite a shipped build or user save while testing. Use a new output
  folder and explicit `--save-file=` paths for persistence tests.
- The authoritative project is `C:/__Beygelman/! -11/dev-ancient-naval`.
  Edit its source/assets/data/README in place. Every delivered version must also
  have complete copies under this repository's `releases/<version>/`; `outputs`
  alone is insufficient. Run `tools/Sync-Release.ps1` after packaging and verify
  hashes/Git visibility. Preserve historical archives as released checkpoints.
- Keep ZIPs, screenshots and release notes Git-visible. The unpacked local
  `releases/<version>/playable` copy is Git-ignored because its executable exceeds
  GitHub's file limit. Never add caches, saves or signing credentials to Git.
  Exclude `releases/` when making a source ZIP or exporting Godot resources;
  otherwise exports can recursively contain previous builds.

## Architectural boundaries

- `src/Core` is pure .NET; it must not reference Godot, nodes, rendering or disk
  paths. It owns turns, movement, combat, world geometry, fog and economy.
- `BattleState` is the command facade and owner of mutable battle state. Its
  partial files group cohesive rules. This is not permission to add unrelated
  features to it: independently reusable algorithms belong in separate classes.
- `Core/Navigation/PathSearch` knows only positions, neighbors and costs.
  `NavalNavigationQuery` supplies ship/fog/terrain policy for one synchronous
  search. Never cache such a query across movement, a turn or a visibility change.
  An immutable MovementPreview may be reused between cursor positions only while
  battle identity, selected ship, movement budget/position and Vision.Revision match.
- `src/Presentation` translates input to commands and renders their results.
  Damage happens once in Core; projectile count and timing cannot change it.
  Projectile orders use `BattleState.Prepare`; commit each impact after its
  flight, then finish before saving or accepting another order. Simulations
  may use the immediate command facade. Movement needs no staged combat clone.
- `Main` composes the scene; selection, command orchestration, view refresh and
  session lifecycle have separate files. Avoid business rules in HUD callbacks.
- Save DTOs are an explicit compatibility contract, not live scene objects.
  Validate nested data before indexing it, and keep valid backups recoverable.

## Code and data

- Prefer one responsibility per file and readable methods, usually under
  150–500 lines per file. The range is a guide, not a reason to pad short modules
  or fragment a cohesive algorithm. Use four spaces and one statement per line
  in newly written logic; don't mix broad formatting changes into bug fixes.
- Ship definitions, economy and configurable mechanics live in `data/balance.json`
  and typed rules. Validate at input boundaries. New optional settings need
  defaults matching older saves and must be included in the saved rules snapshot.
- Update tooltips and messages when making a value configurable. Do not leave
  a literal UI number that contradicts the active rules.
- Don't add dependencies, frameworks or generic interfaces without a concrete
  caller. Remove code only after checking references and compatibility needs.
- Keep deterministic iteration and random draw order. Cosmetics must never
  consume simulation randomness. New randomness must survive save/resume.

## Version and release checks

- Version labels follow `v020`, `v020.1` through `v020.9`, then `v021`.
  The corrected v020.2 remains archived; the current delivered update is `v020.7`.
- Before **every** delivery, build and run meaningful Core/native checks, then
  run `tools/Check-ReleaseSmoothness.ps1` on the final Debug build with a native
  renderer. Preserve its reports under `docs/diagnostics/<version>/`.
  Headless runs cannot establish frame pacing. Do not run CPU-heavy checks
  concurrently with frame-timing measurements. An exceeded gate is a failure
  to investigate before export, not a reason to relax the threshold.
- Test target parchment choices using actual mouse/touch inputs. Double charges
  share a flight; staged impacts still commit exactly once. Radar-only attacks
  must never disclose hidden hulls, health, damage digits or sunk outcomes.

## Turn-based performance

- AI, income, production, healing and cooldowns advance through commands/turns,
  **not** wall-clock timers. Version 1.0's real-time timer examples do not apply.
- Profile representative cases first. Report the measured operation, fleet/map
  size, iterations, allocations and elapsed time; do not equate a microbenchmark
  with FPS or claim support for hundreds of ships without whole-game testing.
- Reuse hot-path buffers when their lifetime is clear. Caches need an explicit
  invalidation boundary. Never let caches expose hidden ships or stale terrain.
- Streaming, pooling, ECS and a global event bus are optional responses to an
  observed problem. The current map does not require introducing them in advance.
- Static terrain has a bounded raster and separately retained per-cell sources.
  Discoveries build only new cells; known cells change tint on fog updates.
  A new world invalidates every cell source. Hover cannot update the raster. Retain world-space range/resource/trade commands across camera movement; native clipping handles their viewport. A new manually culled overlay needs explicit camera invalidation. Keep fog in the cache key.
- Animation populations remain bounded. Stop processing hidden menu scenery;
  respond to resize/command events instead of repeating unchanged work per frame.
- Tall scenery, villages and hulls share one native Y-sort root with individual
  ground anchors. Retain static tree/peak draw commands; only visible village
  animation redraws. Hidden explored towns must retain their last observed art.
- Since 0.16 the title background is static artwork. Keep map ship floor x/y
  and vertical height z separate; test full-circle heading changes before releasing
  renderer edits. Cull invisible building walls and order footprints by depth.
- Underwater schools live in WorldAmbience, never in the terrain raster. Rebuild
  their geometry on observation changes; animate at 30 Hz with viewport culling.
- Automatic outpost shots are Core turn results. Respect active repair, level-2
  unlock, lowest-HP targeting and fog; animation cannot apply damage a second time.
- Flagship retreat decisions use only observed enemies and legal movement routes.

- Trade routes are cached by active owned ports/hazards. Clear topology after aggregate restore; lane speed depends on run history, so route search states include the run. Preview paths retain their original origin. Save the run across partial movement and reset it at turn start.
- God's eye overrides only human visibility; it must not pollute explored memory or AI observations. Radar targeting uses shared contacts without exposing health.
- Difficulty changes decisions rather than statistics or income. Forecasts use observed enemies; a healthy hostile flagship is engaged only with adequate combined firepower.
- Town art/UI must refresh on repair/production even when sight did not change. City interfaces live above world scenery; visual seed draws never consume simulation RNG.

- WorldKind selects terrain only; it must never silently replace the organic cell mesh. Oceans is the optional save-field default. New map policies preserve fair coastal settlement/treasury counts and connected ocean fleet routes.
- VoyageStatistics are immutable player-attributed totals in every staged snapshot. Income counts gross positive receipts/rewards; shipyards count built hulls; direct enemy kills exclude structures and nation-collapse scuttling. Missing old-save totals start at zero.
- Ready pictorial actions open once as horizontal banners and commit only after their burn; block orders during the ceremony and revalidate its original battle/target. Never award a treasury before that boundary.
- Clay badges use observed HP only, retain an unrotated foreground canvas, and animate independently of hull geometry. Town health animation must not refresh hidden observed state. Victory must wait for Busy and PendingPresentation to clear, then own input and stop hidden/finished fireworks.

- TerrainFeatures is the deterministic, board-owned source for mountain cells, forest density and coast depth. Optical rays use actual mesh polygons; radar ignores mountains. Rendering cannot invent extra blocking peaks.
- Heavenly aid counters persist in staged snapshots and Continue. Pay once at each fifth personal turn start; direct flagship kills exclude collapse scuttling. Never reveal an unknown nation through the aid plaque.
- Village ground and wheat are clipped to land minus beach, including fully enclosed holes. Native polygon subtraction can return hole rings; these require proper decomposition before drawing.

## Safety invariants to test

- Fog previews treat unknown terrain as unknown; radar reveals coordinates only.
- Friendly transit, enemy blocking, threat costs and diagonal corner rules apply
  consistently to player previews, actual movement and AI routes.
- Counterattacks never recurse and do not count as an active attack for repair.
- Destroyed objects release income, waiting crews and occupied cells together.
- Villages at zero HP persist; delayed capture and spent capturer actions survive
  save/resume. RNG state and treasury rewards must not reroll after loading.
- Corrupt primary saves may fall back to a good backup; saving afterward must
  not replace that good backup with the corrupt primary. Starting a new voyage
  replaces the slot and deletes the previous voyage backup after successful writing.
- Factions use persistent enum identities and unique colors. Skip eliminated turns;
  model victory is the last surviving Mother, but human presentation stops at defeat.

## Workflow and specialist coordination

1. Inspect the working tree and preserve unrelated/user edits. Identify the
   smallest useful scope and affected public/save contracts before editing.
2. For complex work, specialists may own combat/data (Ares), navigation/ships
   (Poseidon), world (Terra), persistence/economy (Atlas) and rendering (Sol).
   Assign non-overlapping files; the main agent integrates and reviews.
   If specialists are unavailable, continue serially rather than blocking work.
3. Coordinate shared builds and formatting. Do not have two agents overwrite
   the same outputs or edit a shared entrypoint at the same time.
4. Add regression tests for changed invariants, not tests that restate each line.
   Run the existing relevant checks and inspect actual exit codes and logs.
5. Update `README.md` and `CHANGELOG.md` on each delivered update, with concrete
   behavior, validation and limits. Keep architectural contracts in
   `docs/ARCHITECTURE.md`; do not turn this guide into an implementation diary.

## Verification commands

From the repository root:

```powershell
dotnet build Dev_ancient_naval.csproj
dotnet run --project tests/CoreChecks/CoreChecks.csproj --configuration Release
```

With the installed Godot .NET executable, run `--headless --path . --` followed
by `--smoke-test`, `--battle-test`, `--effects-test`, `--sea-test` or
`--optimization-test`. Run menu/persistence checks only with explicit disposable
save paths. See `docs/ARCHITECTURE.md` for compatibility and benchmark details.
Run graphical checks for rendering edits: a headless pass cannot validate pixels.

Long-term goals include larger worlds, more factions, larger fleets and campaigns.
These are design constraints, not authorization to invent mechanics or assurances
that the current release already supports them.
