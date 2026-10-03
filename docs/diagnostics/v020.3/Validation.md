# Ancient Naval v020.3 — executed validation

3 October 2026. Debug and Windows Release compile successfully. The exported PE reports version **0.20.3.0**, product **Ancient Naval v020.3**. All listed functional checks passed; their logs are in this directory.

## Rules, compatibility and opponents

`core-checks.log` reports **1,203,589 assertions**, including eight complete AI matches. This is the sum of assertions, not a count of independent scenarios. Historical balance scenarios use the immutable v020.2 fixture; the live balance is exercised by the 75 v020.3 checks, 443 Admiral checks and 40 swarm/memory checks. Those three groups also passed after the final description update.

Coverage includes fleet capacity and lost capacity, build/gift limits, scuttling, veteran range and full healing, active/passive repairs, lighthouse builders, mortar dead zones, radar privacy, frozen pirate/neutral levels, saved flagship observations, deferred claims, malicious claim/save rejection, persistent treasury ruins, settlement separation and inland Pangaea placement. Legacy saves retain their original rules.

## Native functional and visual checks

| Group | Passed assertions | Scope |
|---|---:|---|
| v020.3 UI | 65 | Physical keyboard, focus/modal guards, capacity, terrain counsel removal, reward claim and Continue, movement preview after shooting, silhouette mask, lethal flag, radar privacy |
| Target choices | 172 | Actual mouse/touch selection, simultaneous double shots, one-time staged impacts, camera timing and hidden radar results |
| Action stories | 48 | Single unfolding, capture/treasury burn before commit, clay transitions |
| Language bridge | 1,224 | English/Ukrainian/Dutch menus, viewport, persisted selection, lore and dynamic messages |
| Title/setup/Continue | 150 | Native title, colors, new game, lore, save and reload |
| Victory/defeat | 110 | Staged completion, modal statistics, dismissible map inspection, finished save deletion and hidden Continue |
| v020.3 world art | 27 | Cosmetic rivers and land clipping, mountain/snow variation, ruin motifs, persistent sea art and loot glow fade |

World, target, reward parchment and translated interface captures were inspected. The target outline renders above terrain. Rivers are narrow cosmetic geometry, independent of gameplay terrain and simulation random state. Sea ruins remain after collection. Native checks exercise disposable save paths outside the repository and leave the player's slot untouched.

The defeat test advances the turn before setting lethal fixture HP: the requested +4 idle healing otherwise makes that old fixture survive. The test still resolves an actual enemy attack and verifies completed-slot cleanup.

## Final native frame pacing

Godot 4.7.2 .NET, OpenGL Compatibility, NVIDIA RTX 4060 Laptop, default VSync. `Check-ReleaseSmoothness.ps1` ran both workloads serially after the final Debug build, with no concurrent Core tests. Seed 731, 1,371 cells, four opponents. Frame reports postdate that assembly; no gameplay/render changes followed the gates.

| Case | Pan p95 / maximum | Interaction p95 / maximum | Hover maximum | Engine video-memory peak |
|---|---:|---:|---:|---:|
| Wide Pangaea | 17.604 / 41.978 ms | 17.200 / 47.476 ms | 0.627 ms | 414.52 MiB |
| Oceans, live fog and autosave | 20.863 / 33.716 ms | 19.361 / 62.531 ms | 0.757 ms | 192.48 MiB |

**Both gates passed without changing their limits:** pan p95 <=25 / max <=80 ms; interaction p95 <=25 / max <=100 ms; synchronous hover max <=16 ms. These are measurements on this host and workload, not guarantees for all hardware or procedural maps.

## Standalone export and packaging

`export.log` ends in `DONE savepack`; final export stderr is empty. Standalone native startup passed in EN, UK and NL, and in map preview; corresponding `export-*.log` / `.err` and `exported-*.png` are preserved. Release-template test hooks are disabled. Movie captures validate startup and pixels, not frame pacing.

The packager verifies historical SHA256 manifests, retained source/assets/tests, the unchanged user roadmap, source inventory, ZIP integrity and copied artifact hashes. Source archives exclude recursive releases, credentials, saves and build caches. Windows includes the full runtime; screenshots remain beside the archives to keep each ZIP below GitHub's 100 MiB file limit. Select New game to use v020.3 rules.
