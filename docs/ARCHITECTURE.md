# Ancient Naval architecture

## Dependency direction

```text
Godot scene / input / HUD
          | commands                  ^ results / snapshots
          v                           |
      BattleState (Core, no Godot reference)
       |      |       |       |
   Navigation Combat Turns Production
       |              |
   PathSearch     Rules / Units / World / Vision

SaveStore (Presentation disk adapter)
       -> session envelope -> Core save DTOs -> validated BattleState
```

`BattleState` remains the single writer for gameplay commands. Separating its
partial files organizes rules without duplicating state or introducing ordering
ambiguities between independent controllers. `BattleNavigation`, `BattleCombat`,
`BattleProduction` and `BattleTurns` expose the existing API. Feature modules
handle villages, balloons, discoveries and progression. `Ship` contains per-unit
state and derived statistics; `BattleRules` contains configured definitions.

## Navigation lifetime

`PathSearch` implements stable Dijkstra with `(cost, insertion order)` priority.
`NavalNavigationQuery` indexes visible enemy occupancy and threat cells once per
query, then memoizes visited terrain/passability. Player estimates and actual
movement use different knowledge modes. A query must not survive a command. `MovementPreview` is instead an immutable
cost/predecessor snapshot, reused for cursor destinations only while the battle,
selected ship, position, budget and optical-vision revision still match.
During actual movement, only the moving friendly ship and optical visibility
change; the query uses real terrain and enemy occupancy, which remain constant.

This avoids rescanning every ship for every explored edge. It does not add a
global position index, which would require a new mutation/invalidating contract.
Optical/radar coverage on organic maps uses a cached bounded breadth-first
search. Its result is tested against full-board distance coverage at radii 0–64
on all four world sizes. It follows corner adjacency, including land, and never
uses the moving ship's navigation restrictions.

`NavigationReference` in tests freezes the pre-refactor implementation so costs,
route ties and fog behavior can be compared independently.

## Scene and animation

`Main` builds the scene and wires events. `MainSelection` interprets clicks and
previews; `MainCommands` executes or stages commands then animates; `MainView` refreshes
HUD/map projections; `MainSession` manages start/continue and opponent turns. `MainSaving` copies a
save DTO on the game thread and serializes/flushed-writes it on a worker while
commands remain locked. No mutable live battle or Godot object crosses threads.

`FleetView` prepares stable depth order and composes the frame. `FleetShipDrawing`
draws units; `FleetCommands` plays command results; `FleetAnimation` interpolates
movement and projectiles; `FleetEffects` renders transient water/air effects.
Projectile orders call `BattleState.Prepare`, which resolves deterministically
on an isolated aggregate with shared immutable board/rules. Saved phases reuse
one geometry snapshot. `PresentedCommand.Impact` applies each Core phase only
after the last shell in that salvo lands; counterattack has its own phase.
Surviving ship/village instances keep their identity. Visual pellet count cannot
change damage or RNG. The immediate facade remains available to simulations.
Movement/build/resource orders stay direct to avoid unnecessary cloning.

`Finish` applies the final state and releases the pending order. Core rejects
actor commands, EndTurn and snapshots while an impact order is pending; scene
input is locked as well. Exit explicitly finishes before saving. A destroyed
Mother leaves its followers temporarily drawable until its wreck has submerged,
then `CompleteFleetCollapse` removes them and their sinking begins. Hidden wrecks
do not introduce a spectator view. Healing receipts report actual HP changes,
never a second application of healing.

Static terrain has retained surface, shore and low grass sources for each cell
inside one isolated `SubViewport`. Trees and peaks live outside that raster in
the Fleet native Y-sort root, alongside villages and moving hull canvases. Each
tall object uses its own projected ground anchor; nested Y-sort groups flatten
into the same ordering. Static scenery retains draw commands. Visible town
flags/mills redraw at 30 Hz in their village canvas, and hidden explored towns
retain their last observed drawing. Unknown cells hide their scenery, including
overhanging geometry. A cell
builds drawing commands on first discovery; subsequent fog changes only set its
visibility/tint. A new projection replaces the sources and reuses the render
target. The GPU updates the bounded atlas as a whole; this is not partial GPU
texture upload. A texture sprite displays its last raster; updates are `Once`,
not continuous. A post-draw version check explicitly stops updates without losing
a request raised later in the same frame. Limits
are 4096 pixels per axis and 8,388,608 pixels (32 MiB RGBA color; conservatively
64 MiB for render/sample attachments, excluding auxiliary driver allocations).
Fog masks are compared before invalidation; hover, selection, combat health and
repair cannot trigger an unchanged terrain rebuild. Dynamic objects are culled
to a padded viewport and refreshed after camera changes. Whole-island contour
geometry is clipped per tile so an unseen shore cannot leak through the texture.

Known resources, contacts, radar and movement/target contours have a
separate retained observation layer. Refresh it after game state/selection or
camera changes. Cursor previews redraw only the route, selected outline and
cached destination coverage. The coverage cache is bounded to 96 destinations
and invalidated by projection, selection, vision or mortar changes. Never
reuse the observation layer across a visibility change without invalidation.

World ambience updates its stationary-point caches on `RefreshVisibility`.
Reusable vertex buffers are consumed by draw calls before the next use. Menu
layout responds to resize events. Procedural scenery remains bounded and separate
from simulation randomness.

## Balance and persistence contracts

`data/balance.json` supplies ship definitions and typed mortar, balloon and
treasury settings. Validation rejects invalid/null groups and invalid probability
totals. Defaults preserve 0.14 rules when new groups are absent from a v1 save.
Existing saves carry their own rule snapshot; changing the bundled JSON affects
new games, not the balance of an already saved game.

The session envelope and battle snapshot remain version 1. Keep serialized enum
identities and obsolete-but-persisted fields until there is an explicit migration.
The save layer validates references and values before restoring indexed state.
Camera state belongs to the session envelope. Fleet palette choices and roster
are persisted in Core because they must remain consistent across all factions.
Captain identities and town names are optional v1 fields. A separate seeded
cosmetic stream supplies them for old saves without advancing simulation RNG.

`SaveStore` writes a flushed temporary file, atomically replaces the primary and
retains a previous valid backup. After recovery from a corrupt primary it keeps
the known-good backup rather than backing up the corrupt file over it. New game
atomically installs a new voyage and removes the prior voyage backup; subsequent
backups are earlier checkpoints of that same voyage.

## Verification and limitations

The core suite includes 100 generated map seeds and complete AI matches.
Navigation refactor checks compare all traversable edge costs in rectangular and
organic worlds, hidden/visible enemies, radar, narrow passages and pirate patrols.
Synthetic fleets of 16/128/256 units measure full-board navigation allocation and
elapsed time. `--optimization-test` compares idle draw preparation for
16/128/512 units, stable ties, changing fog and menu resize behavior. These are
microbenchmarks, not a whole-frame FPS or large-fleet gameplay guarantee.

Godot runtime modes: `--smoke-test`, `--battle-test`, `--effects-test`, `--sea-test`,
`--menu-test`, `--optimization-test`, `--world-test`, `--income-test`,
`--performance-test`. Performance checks accept `--opponents=1..4`, `--scaled-map`,
`--live-fog`, `--report=...`; autosave measurements require an explicit test slot. Menu tests require an explicit disposable
`--save-file=`. Save compatibility and backup tests use temporary directories.
Visual smoke checks should use the compatibility renderer and capture a PNG with
the test's `--capture=` option where supported.

To include actual released save fixtures in the core suite, set
`ANCIENT_NAVAL_LEGACY_FIXTURES` to a directory containing at least two untouched
session saves named `baseline-menu-*.json`. The regular suite also creates its
own v1 compatibility cases when external fixtures are unavailable.

The project still has intentionally shared state in the battle facade, legacy
compact methods and some fixed rule constants. Future extractions should follow
real feature changes. World streaming and multi-faction generalization have not
been implemented by this refactor.

## Multiple fleets and scarcity

Core owns a persisted 2–5 faction roster plus neutral pirates. Every different
owner is hostile. Eliminating a Mother withdraws its fleet and neutralizes its
towns. Alive factions determine turn order and the eventual model winner. The
human scene stops at PlayerDefeated instead of forcing a full spectator campaign.
EconomyRules keeps backward-compatible defaults for old rule snapshots; current
JSON supplies slower income growth and naval upkeep. IncomeReceipt events describe
source gains/deductions; presentation only shows the human fleet’s private receipts.


## Deck and ambience projection (0.16)

DeckProjection rotates a horizontal x/y floor before applying the sea's vertical
foreshortening. Height z is an independent screen-vertical offset. CityShipArt
reuses its buffers, orders building footprints by projected depth and draws only
visible wall faces; nearly edge-on faces are guarded triangles. Convex floor
surfaces retain nonzero area at every yaw. Shell aiming converts screen direction
back to the deck plane; movement uses the same convention.

FishSchools caches visible school geometry on the normal visibility refresh and
renders three/fourteen swimming fish in WorldAmbience, never in TerrainRasterCache.
Wildlife remains capped (18 transient gulls, one dolphin); three dock gulls are
procedural. Schools, clouds and docks are viewport culled. The title harbor is a
static bundled TextureRect and has no processing loop.

VillageCombatRules and BalloonRules.AntiAirRange are optional persisted balance
settings with defaults for released saves. FireOutposts resolves once at owner
turn end before automatic repair, marks active town attacks, and records
visibility-safe OutpostShot snapshots. Presentation only animates those results.
The policy skips manually repairing towns and never damages air units.
FlagshipSafety uses observed enemies only and compares known weapon exposure
across legal routes; it is deliberately local, not an omniscient future planner.
