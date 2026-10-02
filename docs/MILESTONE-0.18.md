# 0.18 — Ports and Captains

Delivered 1 October 2026. Sources are edited directly in `C:/__Beygelman/! -11/dev-ancient-naval`; the complete release is mirrored in `releases/0.18` and the external outputs directory. Previous checkpoints and the user roadmap are preserved. New voyages use the new balance; Continue preserves the voyage's saved numeric rules.

## Implemented behavior

Black city names and front-layer town stats/capture marks; rightmost Info on action/resource papyrus; God’s eye and automatic full-map victory; persistent Boatswain/Captain/Admiral selection; observed-only Admiral focus fire, counterfire/reach forecast, wounded flagship retreat, cheap radar scout and economy/fleet recovery. No difficulty alters prices, HP, income or fog access.

Mothership base HP 15 (+5/level). Prices: Fishing Schooner 4, Fishing Dock 8, Brig 5, Galleon 10, Kolonel 18, Granado 16, Cannon Tower 6. Town level 2 adds wheat/mill; level 3 becomes a taller city and permits a port; level 4 expands fields/homes and adds the second mill. Fortified walls are wooden before level 3 and stone thereafter. Layout is seeded independently of gameplay randomness.

Ports cost 6 and consume that turn's shipyard production, add exactly 1 income and discount local ship production 20%, stacking with Shipwright. Shortest diagonal-safe sea links join active same-owner ports. A continuous lane run accelerates to a minimum of +2 tiles or +20% in clear water; leaving a lane resets it. Coast/threat costs still add to travel, enemies block their cells and friendly transit stays legal. The run survives partial movement and saves, resets each owner turn, and preview/actual/AI use the same stateful search. Zero-HP/captured ports immediately change the network. Maritime link art displays only known segments.

Every armed unit can target a coordinate supplied by allied radar within its own range. Radar contacts supply no HP/name; the visual hit remains anonymous until actual observation. God’s eye affects only the human view and preserves genuine explored memory when turned off.

## Debug findings and changes

The seeded 1110-cell scene showed cheap cursor handling but expensive drawing: pan p50 32.746 ms / p95 55.565 ms, about 3135 draw calls at p95. Individual wave/shore and fish/coral commands dominated measured scene CPU. Camera changes also reissued resource/range observations. Hull canvases were explicitly hidden and shown every frame, forcing their artwork to redraw even if its state was unchanged.

Sea strokes and fish triangles now use bounded reusable buffers and bulk native submissions. Camera-independent observations retain their drawing commands and use native clipping; cursor selection changes only its overlay. Bobbing updates native hull position/rotation. Static hull art is retained until snapshot/heading/barrel/selection/sinking changes; city life refreshes at 10 Hz. Town body art, animated life and foreground interface are independent; gameplay invalidation refreshes repaired HP even without a sight revision.

## Measured graphical interaction

Windows Godot 4.7.2 .NET, D3D12 Mobile, RTX 4060 Laptop, 1280×720. Same seed 731, close camera at zoom 1: 120 pan frames, 64 destination previews, one animated Brig movement and 20 settling frames. Values include actual frame timing, distinct from synchronous handler cost.

| Operation | 0.17 baseline | 0.18 |
|---|---:|---:|
| Pan frame p50 | 32.746 ms | 16.677 ms |
| Pan frame p95 | 55.565 ms | 20.589 ms |
| Pan max | 83.547 ms | 33.819 ms |
| Hover handler p95 | 0.083 ms | 0.080 ms |
| Hover/movement frame p95 | 28.905 ms | 18.606 ms |
| Hover/movement frame max | 66.392 ms | 56.520 ms |
| Synchronous move calculation | 4.321 ms | about 4–5 ms |
| Draw calls p95 | 3135 | 2076 |

The movement animation still deliberately lasts about 1.55 s; that is sailing time, not frozen calculation. Wave CPU submission fell from about 6.27 ms/draw to 0.42 ms/draw, fish from 2.93 to 0.21. An intermediate retained-hull run still had 3150 hull redraws; fixing the visibility toggling reduced them to 183 in a similar interval. Observation redraws during the pan disappeared.

Largest 1371-cell / four-rival map with normal fog: isolated rerun pan p95 19.637 ms / max 23.203 ms; hover p95 0.068 ms; hover/movement frame p95 19.628 ms / max 76.774 ms. The first run also recorded one 4.559 s pan stall without a managed scope explaining that duration; it did not recur in the isolated rerun. Its cause is not established. These measurements demonstrate reduced sustained interaction cost, not a guarantee of constant FPS or the absence of every platform/background stall.

Raw reports: [baseline](diagnostics/0.18/baseline.txt), [1110-cell update](diagnostics/0.18/final-performance.txt), [first largest-map run](diagnostics/0.18/performance4-first.txt), [largest-map rerun](diagnostics/0.18/performance4.txt).

## Verification

- Clean .NET build; Core stays independent of Godot. More than 1.2 million rules/navigation/geometry checks, including saved partial lane speed, every movement speed 1–8 receiving the minimum bonus, preview/actual equality, captured/defeated ports, local price debit, God’s eye memory/privacy, shared-radar fire and observed flagship safety.
- Eight released-policy AI games plus two multi-faction games finish. Each difficulty runs 300 legal-order checks and one complete current-balance voyage: Boatswain round 18 / 251 orders, Captain round 34 / 709, Admiral round 23 / 502. A completion test proves absence of that fixture's stalemate, not that one difficulty wins every game.
- Released v1 saves, optional new settings and save corruption/recovery remain under compatibility checks. Runtime menu checks use disposable explicit save paths; the user's voyage is untouched.
- Actual Windows renderer: 61,423 world checks across all map sizes; 116 effects (full heading turns, salvos, delayed impacts, sinking, veterancy); 69 menu/new game checks and 52 resume; 82 legacy battle/control checks; 248,659 projection/input checks; 108 sea-event checks; 6 income checks; 11 draw-preparation checks; city/port/rightmost Info and retained-health repair checks, with inspected PNGs.
- Exported Windows build and source archive are verified separately and copied with SHA256 checks into the authoritative repository. Godot/source packaging exclude releases and caches to prevent recursive bundles.

The initial randomized runtime movement fixture occasionally clicked a valid resource/papyrus sector rather than an empty destination. It now uses a fixed seed and chooses an unobscured non-resource water cell, retaining real touch input testing.
