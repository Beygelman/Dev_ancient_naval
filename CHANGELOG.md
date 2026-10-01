# Ancient Naval change history

## 0.17 delivery follow-up — 1 October 2026

- Keep the authoritative source project and README in the Git repository;
  additionally copy every delivered artifact to `releases/0.17/` in that project.
- Add a verified repeatable copy tool, release manifest and README download links.
- Keep a complete unpacked local launch copy while tracking the Windows ZIP,
  source ZIP, notes and screenshots. Ignore the oversized raw runtime in Git.
- Prevent Godot from importing copies and exclude release mirrors from export
  and future source packaging. Previously shipped artifacts remain unchanged.

## 0.17 — 1 October 2026 — Tides and Parchment

- Stage projectile commands in an isolated Core aggregate; commit impact HP and
  veterancy after flight, counterattacks after their own flight, and save only
  completed commands. Preserve surviving entity identities when applying phases.
- Fracture/submerge the Mothership before sinking its fleet; animate other wrecks.
- Depth-sort individual forest trees, ridge peaks, villages and moving ships
  together. Move village flags/mills into their village canvas; retain last
  observed town appearance behind fog. Smooth whole-island shore contours.
- Seed forests and mountain ridges across tile boundaries; vary tree silhouettes.
- Reduce gull populations, decimate animated shore-wave samples, and drift unified
  connected-square cloud shapes above offset shadows.
- Open papyrus width and thickness from the bottom center; show icon-only sectors,
  inner-arc HP/progression and number-anchored damage/healing instead of health bars.
- Draw winding burgundy dashed routes and destination crosses; project separate
  cannon/collection/mortar contours and show selected mortar firing annuli.
- Retain one-step clear-water diagonals and two-step enemy threat costs.
- Increase Cannon Tower range to 4; persist seeded unique captains/town names.
  Keep opponent status visibility-safe and announce the human turn centrally.
- Keep movement/build/resource commands direct instead of cloning combat phases;
  profile actual hover/movement and validate released save fixtures.

See [verification and measured limits](docs/MILESTONE-0.17.md).

## 0.16 — 1 October 2026 — City and Watchtowers

- Correct ship floor/height projection at every heading, depth-sort city buildings,
  retain only visible walls, round the capital's twin keels and unified terraces.
- Bundle a generated static city-ship menu illustration; remove the procedural menu loop.
- Set Cannon Tower guns/sight/radar to 3/2/4; add purchasable radar and tower veterancy.
- Set Kolonel/Galleon/Brig ranges to 2/1/1, Balloon sight to 6 and flagship AA to 2.
- Give level-2+ fortified villages automatic weakest-target fire at range 2,
  level damage and level+1 replies. Manual repair suppresses the automatic shot;
  village yards no longer offer Cannon Towers.
- Retreat wounded AI flagships from known gun coverage before aggressive orders.
- Animate submerged resource/reef fish, add more gulls, faceted cloud shadows and V wakes.
- Keep friendly ships selectable on resource schools; keep terrain and route caches intact.
- Persist/validate the optional new rule sections with legacy defaults.

See [verification and measured limits](docs/MILESTONE-0.16.md).

## 0.15 — 1 October 2026 — Charted Seas

### Changed

- Select 1–4 independent rivals with persistent unique colors; scale the map and
  balance village/treasury/pirate counts across their starting territories.
- Start with 8 Thors and income 3; charge combat fleet upkeep, increase ship
  investment, and keep Mothership income constant across levels.
- Restrict resource collection to the same or adjacent cell. Add a level-2
  Cannon Tower; set Kolonel and Galleon cannon range to two.
- Generate shared flowing seams with reduced distortion; draw connected rounded
  shores with dense groves, larger ridges and translucent cloud shadows.
- Rebuild the floating capital as asymmetric city terraces with a golden-domed
  temple, varied houses, market, faction markings, laundry, residents and carts.
- Unfold papyrus action sectors including locked choices; offer sage-style Info
  with current numerical rules. Right-click cancels from panels as well as the map.
- Aim cannon ships broadside before salvos; swivel mortar barrels separately.
  Show hostile HP in red and turn income at each actual source.

### Fixed

- Reuse an immutable route plan across hover positions; invalidate after commands
  and visibility changes. Separate retained terrain from live selection overlays.
- Bound terrain raster memory and retain per-cell drawing commands.
  Known fog cells change tint; discoveries build new geometry; ordinary movement/hover frames reuse pixels.
- Copy immutable save snapshots on the main thread and serialize/write on a worker.
- Starting a new voyage atomically replaces the sole slot and removes the old
  voyage's backup. Subsequent recovery copies belong to the current voyage.
- Preserve released v1 save identities, geometry, roster defaults and balance.

Validation and performance evidence: `docs/MILESTONE-0.15.md` and
`docs/DEBUG-0.15.md`. Existing voyages retain their saved balance; start a new
voyage to use the new economy.

## 0.14.1 — 30 September 2026

### Changed

- Organize the battle facade by navigation, combat, production and turn lifecycle;
  organize the Godot composition root by selection, commands, view and session.
- Separate reusable Dijkstra traversal from the ship/fog navigation policy. Index
  enemy occupancy and threat zones once per query and memoize visited terrain.
- Move mortar purchase/splash/village bonus, balloon bombs/cooldown, treasury
  weights/rewards and dock/pirate resource rewards to validated balance data.
- Separate fleet drawing and command playback; reuse draw-order and ambient
  vertex buffers; respond to menu resize events instead of every frame.
- Adapt the supplied AI development guide to actual turn-based architecture,
  compatibility rules, tests and specialist ownership. Preserve its original text.

### Fixed

- Keep a good `.bak` when autosaving after recovery from a corrupt primary.
- Keep that backup when New game writes before reading an existing corrupt slot.
- Reject malformed nested save collections and geometry references before indexing.

### Validation

- Clean Godot/.NET build; pure Core remains independent of Godot.
- 1,535 basic checks; 1,195,229 gameplay/navigation/map checks and 8 AI games.
- 20 persistence checks including two pre-refactor release saves; 9 disk-recovery
  assertions exercised through the menu checks.
- Godot smoke: 474,809; graphical combat: 99; graphical effects: 41; sea events:
  108; graphical menu/continue/recovery: 27; rendering optimization: 11.
- Continue in a separate process: 13 checks; exported Windows executable opens
  the menu and renders a generated map successfully.
- Navigation differential oracle preserves all tested step costs and route ties.
- See `docs/MILESTONE-0.14.1.md` for measured allocations and timing limitations.

## 0.14 — 30 September 2026

Living Seas: animated title/color selection, recoverable Continue, irregular hexagon,
weighted treasures, enemy-only mortar splash, updated balloons, village shipyards,
terrain details and naval motion/effects. Full notes: `docs/MILESTONE-0.14.md`.

Earlier playable checkpoints are documented in `docs/MILESTONE-0.11.md`,
`docs/MILESTONE-0.12.md` and `docs/MILESTONE-0.13.md`.
