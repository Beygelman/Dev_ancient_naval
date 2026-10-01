# 0.15 — interaction debugging, 1 October 2026

## Reproduction and measurement

Use the Godot .NET debug project, seed 731, an organic 1,110-cell map, six
starting ships and a selected Brig. At camera zoom 1, hover 64 different sea
cells, execute one real animated move and measure another 20 frames. Warm the
scene first. Compare an isolated released 0.14.1 source checkout against 0.15.
The new version also exercises flushed autosaving to a disposable slot.
The versions deliberately differ in geography/art/rules; this is a representative
whole-game comparison, not an identical synthetic scene.

CPU scopes use Stopwatch and current-thread managed allocation counters.
Frame intervals use wall-clock timestamps between process callbacks. Earlier
trial reports used Godot delta, which can clamp long frames to 150 ms; the final
comparison corrects that instrumentation in BOTH versions. The engine's process
monitor updates slowly and is excluded from per-frame conclusions.

Windows / Godot 4.7.2 .NET / D3D12 Mobile / RTX 4060 Laptop GPU. Another 3D game
was running during final measurements; no user application was stopped. These
are measured results under shared load, not a promise of a particular FPS.

## Final comparison

| Measured operation | 0.14.1 | 0.15 |
|---|---:|---:|
| Hover handler p95, ms | 0.962 | 0.167 |
| Managed bytes for 64 hover handlers | 10,643,072 | 94,768 |
| Board CPU drawing over 66 route frames, ms | 17,041.449 | 16.287 path + 4.657 retained observations |
| Frame interval p95, ms | 714.090 | 26.730 |
| Worst measured frame, ms | 757.826 | 53.066 |
| Movement model, ms | 8.325 | 5.249 |
| Save snapshot copy on main thread, ms | not measured | 0.581 |
| Save serialize/flush/write on worker, ms | not measured | 44.529 |

The handler's temporary allocations fall by 99.1%. Terrain is not rebuilt during
route hovering. Animation deliberately lasts around 1.6 seconds: that elapsed
movement time is not 1.6 seconds of blocked CPU. Raw scope numbers, including
culture-specific decimal commas, are in [before](diagnostics/0.15/interaction-before.txt)
and [after](diagnostics/0.15/interaction-after.txt).

## Causes and changes

1. Every hovered destination repeated the same weighted route search and
   reconstructed current knowledge. Reuse one immutable cost/predecessor plan
   while battle, selected ship, position, movement budget and vision revision match.
   Actual commands still validate navigation against current state.
2. Every path redraw replayed all terrain, resource, village and range drawing.
   Retain surface/shore/scenery commands per cell inside a bounded raster, then
   retain observed objects/contours separately. Hover now draws just the route and
   selected outline. Camera and visibility changes invalidate the relevant layer.
3. Fog updates rebuilt the whole map, producing a movement spike. Known cells now
   change tint; new cells build their own geometry. Unexplored cells hide all their
   surfaces, beaches and canopies. GPU raster refresh still covers the whole bounded
   atlas, so first exploration is not free.
4. Optical/radar coverage searched full-board distances for each origin. Use exact
   bounded breadth-first coverage with immutable-mesh caching. Compare its cells
   against full-board distance coverage at radii 0–64 on all four map sizes.
5. Autosave serialized and flushed files in the command/rendering thread. Copy an
   immutable DTO there, then serialize and atomically write on a worker. Command
   locking remains until the save finishes; the rendering thread remains active.
6. Large frames made fixed-timer animation assertions unreliable. Runtime tests now
   inspect the actual aiming/projectile phases. Initialize fleet visual state before
   a command animation, so the next process frame cannot reset its initial heading.

The raster is limited to 4,096 pixels per axis and 8,388,608 pixels: 32 MiB RGBA
color, conservatively 64 MiB for render/sample attachments, plus driver metadata,
retained drawing commands and managed scene objects. It stops GPU updates after
changed source versions are drawn. No resource pools, ECS or extra framework was
introduced.

## Largest map and remaining limits

With four rivals, 1,371 cells, live fog and autosave, the final run measured:
hover p95 0.247 ms, frame p95 23.104 ms, worst frame 74.884 ms, movement model
4.759 ms. The move discovered 35 cells: surface generation took 3.796 ms, shores
22.579 ms and scenery 8.429 ms in aggregate. Existing explored geometry was reused.
Raw data: [largest-map run](diagnostics/0.15/interaction-largest-fog.txt).

A short exploration peak remains. Shared GPU load, new shore geometry and the
atlas raster all contribute; this release does not claim zero stutter or support
for hundreds of ships. Synthetic fleet-size checks validate allocation/correctness,
not FPS. They can vary or become slower under competing load.

## Re-run

Build the project with `dotnet build Dev_ancient_naval.csproj`. Run the installed
Godot .NET executable with `--path . -- --performance-test --report=<output.txt>
--save-file=<disposable.json>`. Add `--opponents=4 --scaled-map --live-fog` for the
largest live-fog case. Diagnostic hooks are debug-only; use source, not the release
executable. Never point tests at the player's real save slot.

Additional rendering assertions use `--world-test`; interaction, effects and
persistence checks use `--battle-test`, `--effects-test`, `--income-test`, and
`--menu-test --save-file=<disposable.json>`. Menu resume is tested in a second
process with `--resume-only`. See [validation results](MILESTONE-0.15.md).
