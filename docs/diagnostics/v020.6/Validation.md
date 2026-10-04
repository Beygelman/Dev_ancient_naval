# v020.6 validation — 4 October 2026

Windows native OpenGL Compatibility, Godot 4.7.2 .NET, NVIDIA GeForce RTX 4060 Laptop GPU. Debug build: zero errors and warnings. All tests use disposable save/settings paths outside the repository.

## Rules and compatibility

Full Core checks passed: **1,204,413 assertions across 17 suites**, including 107 v020.6 checks and 8 complete AI matches. New rule fields have historical defaults; embedded old-save rules are retained. Tests include ready-object affordability/capacity/movement/safe attacks, equal double charges, Granado distances 2–5, no upkeep, revised income, separated starts, empty outer rim, gift spawns, size-dependent pirate quotas, settlement eligibility and serialization.

## Native regression checks

| Suite | Passed checks | Coverage |
|---|---:|---|
| Animation0206 | 30 | Solid keel/deck/house/sanctuary/mast/gun fragments, physical sea clipping, cleanup, gull alarm, pencil lanes, treasury demolition |
| Hints0206 | 55 | Actual clicks/Space/R/Escape/right click, stamp/modal guards, five-column objects, preference, locales, scaling and cached guidance |
| Tutorial0206 | 57 | Five public event triggers, close controls, once-per-voyage history, hints-off bypass, nonmodal input, captions below parchment |
| NativeWorld0206 | 836 | Four map sizes × four world types × four rivals; uniform house size and inland footprints, shifted town picking/action anchors, six faction shrines and retained geometry |
| Ui0205 | 78 | Painted setup, settings, scrolling, introduction and saved voyages |
| Construction0205 | 40 | Construction projections, support vessel artwork and ruins |
| Ui0204 | 142 | Scaled native input, modal bounds, translations and paid city upgrade |
| Language0202 | 1,223 | Native locale bridge, dynamic messages, lore, viewport and preference persistence |
| TradeGlyph0204 | 18 | Cosmetic traffic and shared clay/shipyard symbols |
| Menu | 150 | Title, colors, save/reload and unfinished-voyage lifecycle |
| Ui0203 | 65 | Keyboard shortcuts, object counsel and reward gates |
| Refinement021 | 172 | Staged simultaneous salvos, camera timing, circular actions and anonymous nation status |
| Victory | 124 | Staged outcome/statistics and modal lifecycle |
| Effects | 121 | Salvos, delayed damage, movement, bounded wildlife and veterans |
| Story | 48 | Capture/treasury activation, burn boundary and clay transitions |

Total: **3,159 native regression checks**. A separate native capture fixture produced five real game close-ups for tutorials; the Kolonel image contains its simultaneous six-cannonball broadside. Images were inspected. Historical Effects fixtures were adjusted to claim encounter rewards before choreography and use paid city upgrades, replacing obsolete timed growth assumptions.

## Final frame pacing

Final Debug build, **1,759 tiles/four rivals**, hints enabled, no concurrent heavy test/build workload. Both native gates passed without changing their limits.

| Scenario | Pan p95 / max, ms | Interaction p95 / max, ms | Maximum hover, ms |
|---|---|---|---|
| Pangaea overview | 21.763 / 37.408 | 19.934 / 60.194 | 0.732 |
| Oceans with live fog | 18.516 / 33.433 | 20.452 / 88.962 | 0.644 |

Limits: pan p95 ≤25/max ≤80; interaction p95 ≤25/max ≤100; hover max ≤16. Measurements apply to this host and these scenarios, not every driver or machine. Full operation traces and slow-frame entries remain in the adjacent reports; the longest interaction interval occurred during move/encounter/save, with no GC or canvas/shader compilation in that frame. Headless checks were not used to establish pacing.

## Delivery checks

Windows Release export is checked with the actual executable in English, Ukrainian, Dutch and map-preview startup. Full runtime folders, source/data/assets, notes and evidence are packaged with SHA256 manifests. Previous output manifests and archive integrity are verified; release copies are mirrored into the repository and hash-checked. No test saves/preferences or tutorial history sidecars enter the archives. The user's primary Git HEAD/index and roadmap remain unchanged.
