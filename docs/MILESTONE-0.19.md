# 0.19 — Worlds and Clay

Completed 1 October 2026 from the local unfinished 0.19 handoff, preserving the
stable 0.18.1 packages and all earlier local work. The authoritative project is
`C:/__Beygelman/! -11/dev-ancient-naval`; `outputs` is an additional delivery mirror.

## Playable changes

- Four new-game world choices: Sea World, Oceans, Continents and Pangaea, using
  the same organic hexagonal mesh. Continents and Pangaea include shallow rivers;
  settlement/treasury opportunities and starting sea routes remain fair.
- Volumetric ships, dock buildings and clouds in the flagship's projected
  floor/height style. Ship decks remain upright at every heading.
- Paired clay HP/class amphorae with four damage shapes, impact shake/chips and
  a rebuilding, two-glint repair animation. Radar contacts retain anonymity.
- Illustrated harbor/treasury papyri that roll and burn before the validated
  capture/reward command. The action fan sits beside the bottom-left ledger.
- A dimmed victory result with glowing text, finite fireworks, story, earned
  currency, ships built and directly attributed enemy vessel/flagship kills.
  It waits for projectile impacts and sinking to finish before taking input.

World kind and voyage statistics survive Continue and staged combat snapshots.
Older saves default to Oceans and zero newly recorded lifetime totals, without
regenerating terrain or replacing their saved balance. Existing controls, ports,
trade lanes, AI difficulties and creative/God's-eye settings are preserved.

## Rendering completion

The handoff's remaining bottleneck was fully revealed Pangaea: thousands of
individual tree/peak primitives submitted every frame. SceneryAtlas now bakes
the original shapes at 2× resolution into shared 2048×2048 textures. Separate
sprites retain every original ground anchor, depth order and per-cell fog tint.
Height-aware shelves pack trees and peaks without tying a whole row to the
height of one mountain. Bake viewports stop after the first completed frame,
their source painters are freed, and callbacks disconnect exactly once.
Hovering, camera motion and fog changes never rebake scenery. Premultiplied
blending preserves translucent edges without adding dark fringes.

A dense regression seed, 18073096, also exposed subpixel beach quads that passed
the orientation test but failed native triangulation. Such sand slivers are
omitted while retaining their shoreline stroke. This changes cosmetic geometry,
not the mesh, terrain topology, pathfinding or gameplay.

## Validation

Clean .NET build: zero warnings/errors. Actual Windows D3D12 Mobile renderer:

| Check | Passed |
|---|---:|
| Core baseline | 1,535 |
| Economy/progression/combat | 1,201,652; eight completed AI matches |
| Save compatibility | 18 |
| Four-world generation | 368 |
| Voyage statistics | 23 |
| World geometry/cache/fog, 1–4 rivals | 61,448 |
| Dense Pangaea seed, including save/Continue | 23,856 |
| World selector/new game/Continue/native maps | 93 |
| Fleet art, full heading rotation, clay health and radar | 667 |
| Staged victory/statistics/modal input | 108 |
| Capture/treasury ceremony gates | 43 |
| Title/color/save/reload; separate-process Continue | 141; 120 |
| Ports; projectile/movement effects; income feedback | 12; 116; 6 |
| Naval runtime; input/projection smoke; sea events | 82; 248,659; 108 |
| Rendering cache regression | 11 |

Dense Pangaea retains 4,203 trees and 399 peaks in 13 texture pages. The tests
inspect actual atlas pixels, every anchor's fog visibility/tint, idle viewports
and texture reuse. They check that Continue replaces the atlas once and retains
the exact saved battle. A multi-world diagnostic timed out once during development;
subsequent multi-world runs and an explicit replay/Continue of that seed passed.
The timeout's exact cause was not conclusively isolated.

PNGs were inspected for world shape, rivers, tree/peak placement, transparent
edges, ship volume, clay stages/repair, action placement, illustrated papyri,
victory layout and the 1280×720 selector. Source and Windows ZIP integrity,
release checksums and preservation of prior packages are verified during packaging.
The Windows export is separately launch-probed in menu and map-preview modes.
Tests use disposable saves; the user's normal save is untouched.

## Measured performance and limits

Seed 731, 1,371 tiles, four rivals, RTX 4060 Laptop GPU, Windows D3D12 Mobile.
The before/after test uses the same fully revealed Pangaea and fitted overview,
120 camera frames, 64 cursor previews and an actual Brig movement:

| Metric | Before | Final 0.19 |
|---|---:|---:|
| Draw calls, p95 | 23,164 | 3,497 |
| Camera frame p50 | 40.731 ms | 26.758 ms |
| Camera frame p95 | 58.220 ms | 35.039 ms |
| Hover/movement frame p95 | 74.718 ms | 31.841 ms |
| Hover handler p95 | 0.262 ms | 0.101 ms |
| Hover/movement maximum frame | 355.077 ms | 103.610 ms |

Draw calls fall about 85%, camera p95 about 40%. Earlier post-fix runs measured
camera p95 around 22 ms; the table uses the final run, rather than the best run.
On Oceans with ordinary fog, camera p95 is 17.720 ms and hover/movement p95
17.700 ms, with a 70.984-ms maximum. These are scenario measurements, not a
promise of fixed FPS or smooth generation on every device/seed. Atlas memory
and initial bake cost grow with dense land scenery. Android is not validated.
The pure draw-order microbenchmark still favors its former implementation at
some large fleet sizes; it is not evidence of a universal speed improvement.

[Before Pangaea](diagnostics/0.19/baseline-performance-pangaea.txt),
[final Pangaea](diagnostics/0.19/performance-pangaea.txt),
[ordinary fog](diagnostics/0.19/performance4.txt),
[dense seed](diagnostics/0.19/release-world-pangaea-seed.log),
[Core](diagnostics/0.19/core-checks.log).

## Release copies

Complete launch folder: `releases/0.19/playable/Ancient Naval.exe`.
Extract the entire Windows ZIP when sharing; keep EXE, PCK and bundled runtime
together. Source ZIP excludes generated output, credentials and all release
mirrors, avoiding recursive archives. ZIPs, notes, screenshots and a precise
completion change list remain Git-visible; the oversized raw runtime is local-only.
Copying a release does not commit or push to GitHub.

[Windows ZIP](../releases/0.19/Ancient_Naval_0.19_Windows.zip),
[source ZIP](../releases/0.19/Ancient_Naval_0.19_Source.zip),
[Russian notes](../releases/0.19/Ancient_Naval_0.19_Notes_RU.md),
[checksums](../releases/SHA256-0.19.txt).

[Pangaea](../releases/0.19/Ancient_Naval_0.19_pangaea.png),
[fleet](../releases/0.19/Ancient_Naval_0.19_fleet.png),
[clay stages](../releases/0.19/Ancient_Naval_0.19_clay-stages.png),
[claim papyrus](../releases/0.19/Ancient_Naval_0.19_claim.png),
[victory](../releases/0.19/Ancient_Naval_0.19_victory.png).
