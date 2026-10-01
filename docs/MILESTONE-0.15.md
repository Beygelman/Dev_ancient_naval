# Ancient Naval 0.15 — Charted Seas

Playable checkpoint, 1 October 2026. This implements the requested multi-fleet,
economy, coherent coast/grid, parchment interface and movement/debugging update.
Existing release folders and saves were preserved during verification.

## Behavior

- One to four rivals, independent hostility/turns, unique colors excluding the
  player, scaled sea area, balanced territory access counts and pirate populations.
- Starting reserve 8, income 3, fixed Mother income 2; village income 1/1/2/2/3.
  The first two combat naval ships are upkeep-free; each further pair costs one.
  Brig 6, Fishing Schooner 9, Dock 10, Galleon 12, Kolonel 20, Granado 18.
- Directly adjacent collection, level-2 Cannon Tower, range-2 Kolonel/Galleon.
- Connected rounded islands, coherent shared seams, dense groves, larger ridges,
  translucent drifting clouds and shadows. Map algorithm is in MAP-GENERATION.md.
- Asymmetric city Mother, gold-domed temple, varied beige homes, faction motifs,
  markets, laundry, residents/carts, waves and birds in the title close-up.
- Papyrus action arc with locked choices, current-stat sage-style Info, right-click
  cancellation, red hostile HP and source-specific income labels.
- Broadside alignment before cannon launch; independent mortar barrel aim.
- New voyage atomically replaces Continue and removes the prior voyage backup.
  Later recovery copies belong to the current voyage. Old v1 saves retain their
  original rule snapshots; start a new voyage for the new balance.

## Verification

- Godot/.NET build: zero errors, zero warnings; Core has no Godot dependency.
- 1,535 basic Core checks and 1,194,349 rule/navigation/map/progression checks.
- Ten complete AI games: eight legacy games, plus two-rival and four-rival matches.
  Multi-rival winners were Enemy at round 31 / 422 commands and Enemy4 at round
  35 / 712 commands. This exercises elimination and independently hostile turns.
- 23 persistence checks, including two released v1 map fixtures and stable resaves.
- Actual Windows/D3D12 graphical suites: world 37,677; effects 51; income 6;
  menu/save/reload 61; separate-process resume 44; battle 82; input/camera/HUD
  248,659; sea events 108; rendering preparation 11. All returned exit code zero
  with empty error logs. Runtime summary is in diagnostics/0.15/runtime-checks.txt.
- Reviewed world overview, close-up command papyrus, title, income and fleet/effects
  screenshots. Tests validate hidden terrain sources, one-cell discovery, tint-only
  known fog updates, shared contour triangulation and bounded raster dimensions.
- Core coverage optimization is independently compared with full-distance coverage;
  old navigation reference checks still verify fog, blockers, threat costs and ties.

Performance evidence and the remaining exploration peak are documented in
[DEBUG-0.15.md](DEBUG-0.15.md). Timings are operation/scene measurements, not broad
FPS guarantees. Windows release export succeeded. Its title and fully revealed
map started in actual D3D12 windows and exited with code zero and empty error logs.
The deliverables include source, Windows build and SHA-256 manifests; archive
integrity and the unchanged 0.14/0.14.1 manifests are verified during packaging.
