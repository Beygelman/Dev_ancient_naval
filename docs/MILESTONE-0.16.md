# Ancient Naval 0.16 — City and Watchtowers

Playable Windows checkpoint, 1 October 2026. Replaces ship projection and menu
artwork, introduces tower/outpost changes and local flagship retreat, and adds
submerged fish schools, sharper clouds and linear wakes. Full changes are in
[README](../README.md) and [CHANGELOG](../CHANGELOG.md).

## Rendering fix

The old two-coordinate helper treated some negative deck coordinates as vertical
height. That changed the footprint discontinuously. The city also retained a fixed
house order and painted both walls at every heading. Floor/height are now explicit:
rotate deck x/y, foreshorten the floor, then subtract independent vertical z.
Buildings are sorted by projected footprint depth; hidden wall faces are culled.
Nearly edge-on walls use guarded triangles. Convex floor surfaces remain nondegenerate.
The capital has two rounded wooden keels, continuous terraces, varied homes and a
gold-domed temple. The previous procedural title close-up was removed in favor of
the [generated static illustration](MENU-ART-0.16.md).

## Verification

- Build: zero warnings and errors.
- Core: 1,535 base checks; 1,198,175 rules/navigation/map checks, including
  ten complete AI games (eight legacy, two current multi-fleet games).
  Current matches finished at rounds 65 and 53 with two/four opponents.
- Persistence: 23 compatibility/validation checks, including two released v1
  fixtures. New rule fields have legacy defaults and are included in rule snapshots.
- Actual Windows Godot / D3D12: world 37,677; effects 104; income 6; menu 63;
  process-restart Continue 46; battle 82; input/projection smoke 248,659;
  sea events 108; draw preparation 11. Logs contain no rendering exceptions.
- The effects suite renders the capital through 36 headings, captures six views,
  verifies invariant building height and plays an automatic outpost projectile
  beside a dock reef. Visual salvos still leave committed damage unchanged.
- Regression cases cover tower radar and kill promotion, level 1–5 outposts,
  weakest-target selection, one-higher counter damage, active-repair precedence,
  blocked village tower construction, exact AA reach, fog-safe retreat, and
  resource density and pirate bounties awarded to outpost owners. Existing single-voyage save/recovery checks also pass.

## Interaction measurements

Same Windows laptop/Godot renderer as 0.15, with other user applications left
running. Reports use actual elapsed frame intervals, 64 hovered destinations,
real movement animation and an explicit disposable autosave slot.

| Scenario | Tiles | Hover p95 | Movement calculation | Frame p95 | Worst frame |
|---|---:|---:|---:|---:|---:|
| Baseline board, one rival, revealed | 1,110 | 0.151 ms | 3.664 ms | 20.991 ms | 48.251 ms |
| Largest board, four rivals, live fog | 1,371 | 0.125 ms | 4.152 ms | 18.432 ms | 65.851 ms |

Hover handler allocations: 94,768 / 94,192 bytes across all 64 calls. Movement
animation lasts about 1.63 / 1.62 seconds by design. Retained terrain and route
caches remain intact; fish animate in the culled ambience layer at 30 Hz.
The fully revealed scenario's ambience draw peaked at 21.598 ms; largest live-fog
scenario at 7.236 ms. Rare exploration/drawing spikes remain possible. These are
two measured scenarios on shared hardware, not an FPS guarantee or a large-fleet
benchmark. [Raw reports](diagnostics/0.16/interaction-default.txt) and
[largest live-fog report](diagnostics/0.16/interaction-largest.txt).

## Scope and compatibility

Start a new voyage for the new balance; existing voyages retain saved numerical
settings. Cannon Towers receive the new 3/2/4 weapon/sight/radar values. Ancient
Mortar Towers retain their original 5/3/5 profile, gain veterancy and still cannot
repair or produce. Outposts shoot at owner turn end, except when actively repaired.
The retreat policy considers known current weapon coverage; it is a local policy,
not a prediction of every enemy's future move. The static menu illustration keeps
blue accents while chosen colors apply to the playable fleet and settlements.

The self-contained 0.16 export was launched in both title and map-preview modes; both exited normally with no engine errors. Earlier release folders/archives and real player
save files are not overwritten. Android/device validation remains future work.
