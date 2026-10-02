# Ancient Naval corrected v020.2 — executed validation

3 October 2026. Final source builds with zero warnings/errors. Core reports **1,203,314 assertions**, including eight complete AI matches, updated Galleon range, save compatibility, deterministic terrain, radar privacy, progression, lighthouses and coordinated Admiral tactics.

## Native frame pacing

Godot 4.7.2 .NET, native OpenGL Compatibility on NVIDIA RTX 4060 Laptop. Default VSync, serial checks on the final Debug assembly; no headless frame claims. Seed 731, 1,371 cells and four rivals. Timing tests block unrelated UI input and call production selection, preview, movement and persistence paths. Separate pointer checks exercise actual mouse/touch events.

| Case | Pan p95 / max | Interaction p95 / max | Hover max | Engine-reported video memory peak |
|---|---:|---:|---:|---:|
| Wide Pangaea | 17.851 / 33.352 ms | 17.234 / 45.403 ms | 0.668 ms | 384.44 MiB |
| Oceans, live fog and autosave | 17.097 / 33.076 ms | 17.632 / 66.860 ms | 1.434 ms | 197.25 MiB |

**Both native gates passed.** Limits remain pan p95 ≤25 / max ≤80 ms, interaction p95 ≤25 / max ≤100 ms, hover max ≤16 ms. Reports postdate the final Debug assembly. No later gameplay/render code change was made. These measurements describe this host/workload, not all hardware or every possible fleet size.

Earlier attempts are retained as diagnostic context. D3D12 had intermittent first-move stalls (latest comparison 525.414 ms). Later OpenGL runs failed while the old game and other applications nearly filled dedicated GPU memory; disabling VSync alone did not resolve them. Final measurements occurred after the old game was closed and after retained wave geometry and selective hull redraws were implemented. Those changes and the cleaner environment both differ; this evidence does not isolate a single cause or justify claiming that all driver contention is fixed. OpenGL is the delivered default.

## Functional and visual coverage

| Native group | Passed assertions | Coverage |
|---|---:|---|
| World | 61,454 | 1–4 rivals, mesh, discovery caches, bounded atlases and retained waves |
| Rare Pangaea seed 18073096 | 25,588 | inland routes, coastline geometry, Continue and dense scenery |
| World artwork | 2,619 | inland lake hole, larger/connecting peaks, meadows, clipped town footprint, fields and flags |
| Lighthouses / towns | 3,751 | five-level town layout, square walls, placement, layer order, merged port routes |
| Salvo / input / privacy | 170 | actual mouse/touch two-choice target parchment, simultaneous launch, staged impacts and anonymous radar |
| Circular UI | 112 | long circular arcs, scaled sector hits, icon spacing and target/friendly input holes |
| Claim / treasury stories | 48 | one unfold, matching parchment family and burn-before-once-commit |
| Fleet artwork | 715 | upright 360° hulls, health amphorae, damage/healing and fog |
| Effects | 121 | projectiles, travel, wildlife and veterans |
| Localization | 1,180 | EN/UK/NL menus, active selectors, mystical names, actual hover effect descriptions |
| Other preserved checks | see logs | finished saves, menu/Continue, sea events, income, ports, legacy battle and smoke |

Latest native landscape, compact coastal town, target choice and Ukrainian hover parchment captures were inspected. Large visual summits can span neighboring inland cells, but their whole ground skirt stays on land away from sand. Connecting peaks remain attached to existing mountain gameplay cells, preserving sight obstacles. Forest sampling is a continuous seeded field; broad plain areas remain. Lakes subtract holes rather than rendering an inverted contour as land.

The final Windows export exits successfully on Ukrainian and Dutch title screens and a native map preview. Exported captures were inspected; translations, assets, compact cities, shoreline and retained waves are present. Release templates disable runtime test hooks, so standalone startup was checked separately. Numeric PE version is 0.20.2.0; displayed project version is v020.2. Movie-writing captures validate pixels/startup, not frame timing.

## Compatibility and packaging

Existing saves retain their serialized rules. Start New game for the added default Galleon attack radius of two tiles and new generation. Sources, assets and the user roadmap are retained. Original historical 0.20.2 packages remain immutable; corrected v020.2 is separate. Packaging verifies all historical checksum manifests, ZIP integrity, complete source inventory and SHA256 of copied artifacts. Playable export excludes optional project documentation/test resources; full documentation and tests remain in the Source archive. Archives exclude credentials, saves, build caches and recursive release folders.
