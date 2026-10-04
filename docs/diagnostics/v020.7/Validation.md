# v020.7 validation

Final .NET Debug build: zero warnings and errors. Full pure-Core regression run: **1,204,156 assertions across 18 suites**, including eight completed AI matches and **61 v020.7** fleet, harbor, refund, crash and stronghold checks. Native rendering used Godot 4.7.2 .NET, OpenGL Compatibility and the local NVIDIA RTX 4060 Laptop GPU. All save/preference checks use explicit disposable paths outside real voyage slots.

## Native behavior and pixels

| Suite | Result |
|---|---|
| ui0205 | 78 v020.5 painted setup, settings, scrolling, introduction and saved-voyage checks. |
| tutorial0206 | 57 tutorial events, native close-ups, preference, history, external captions and input checks. |
| hints0206 | 55 v020.6 native guidance, turn confirmation, setting, translation and modal checks. |
| victory | 189 staged victory/statistics/modal checks (Windows). |
| construction0205 | 40 construction projection, support art and ruin checks. |
| trade-glyph0204 | 18 v020.4 cosmetic trade traffic and shared clay/shipyard motif checks. |
| language0202 | 1229 language bridge, native menus, viewport, preference, lore and dynamic-message checks. |
| ui0204 | 142 v020.4 modal, translation, scaled input and paid town UI checks. |
| ui0207 | 57 v020.7 native parchment, ready objects, static advice, acknowledgement and modal wheel checks. |
| voyage0207 | 25 v020.7 stacked selection, welcome and personal voyage checks. |
| animation0207 | 54 v020.7 retained merchants, diagonal ports, ritual effects, idle batteries and staged solid balloon crash. |

Native checks use real mouse input for parchment choices/confirmations, plus keyboard paths; they preserve aggregate state through browsing and verify exact command boundaries. Viewport tests cover 1280×720, 618×1400 and 800×520 at interface scales 80–125% and English/Ukrainian/Dutch. Selected native captures were visually inspected for readable paper, footer reachability, shared icon design, merchant skins, shrine/smoke art and independent balloon wreck parts. A replaced advice acknowledgement cannot clear or strand the next voyage's same-topic tip. Construction/repair locks, staged sink-before-outcome order, finished-save deletion and opaque physical immersion remain checked.

## Final native frame gates

The unchanged release tool ran serially after the final Debug build, without concurrent builds or heavy tests. Each fixture uses 1,759 cells, Ocean size, four rivals and 64 route-hover probes. Limits remain pan p95≤25 ms/max≤80 ms, route hover≤16 ms, interaction frame p95≤25 ms/max≤100 ms.

| Scenario | Pan p95 / maximum ms | Hover maximum ms | Interaction frame p95 / maximum ms |
|---|---:|---:|---:|
| Pangaea overview | 20.972 / 40.255 | 0.462 | 21.475 / 56.893 |
| Oceans, live fog | 17.115 / 31.445 | 0.448 | 17.303 / 70.880 |

Both final scenarios pass. The first live-fog pan sample failed at p95 27.007 ms; its evidence is retained in `first-frame-attempt/`. Investigation found no GC, canvas/draw compilation or component work spike at its slow frames (hover under 0.5 ms; individual draw/refresh operations under 1.6 ms). A fresh serial full-tool run passed the same limits without altering code, fixtures or thresholds. Background process sampling showed no competing sampled Codex/.NET workload. The observed spike's external cause is not proven. These timings describe this host; they do not guarantee pacing on every machine.

## Rules and compatibility

New weighted/diagonal/refund/crash rules are enabled in new voyages only. Missing fields retain old saved behavior. Actual construction receipts, selected port sea cells, per-captain direct kills, all-structure and tower/beacon-only counts survive save/resume and staged commands. Free hulls cannot generate demolition currency; impact/finalization retries cannot pay or damage twice. Hidden/radar-only contacts remain anonymous.

Swift expedition limit is `6+4*rivals+3*sizeIndex`, matching 10 turns for Lake/one rival, 20 for Sea/two rivals and 31 for Ocean/four rivals. Admiral requires at least eight direct kills and twice the strongest rival. Conqueror requires six constructed towers/beacons and at least 70% towns; docks alone do not count. Long-economy evidence uses size, rivals, construction, income and final towns. Narration grants no gameplay rewards.

## Export and delivery

Final Windows export succeeded with product version 0.20.7. The actual exported executable passed four startup checks: English, Ukrainian, Dutch and map preview, with no error output. Their captured frames were visually inspected. Results are recorded in `export.log`, `export-*.log` and `exported-*.png`. Release manifests verify complete Windows/source ZIPs and mirrors. Previous archive hashes, the original beta roadmap and primary Git HEAD/index are checked before packaging/publication. Historical releases remain immutable.
