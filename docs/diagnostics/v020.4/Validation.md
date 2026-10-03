# v020.4 verification

3 October 2026, Windows, Godot 4.7.2 .NET, Compatibility renderer, NVIDIA RTX 4060 Laptop GPU.

The final Debug build completed with no warnings or errors. Core checks passed 1,203,669 assertions, including eight complete AI matches and 80 new paid-town, flagship-reward, repair-lock and save-policy checks. Historical saved rules retain automatic village growth; new games use paid levels. Test saves and interface preferences used explicit disposable paths outside the repository.

Native graphical checks passed:

| Case | Checks | Coverage |
| --- | ---: | --- |
| ui0204 | 142 | Real pointer inputs, central upgrade action, repair locks, persisted scaling, 60% modal bounds, three languages and portrait/small/landscape viewports |
| world-visual0204 | 32 | River spacing, town/peak exclusion, bank geometry, plaza and rear port road; town silhouettes remain visible between mountains |
| trade-glyph0204 | 18 | Departure interval, slow routing, three skins, bounded population, hidden processing, unchanged battle state and shared badge/menu motifs |
| ui0203 | 65 | Keyboard controls, reward claims, saved Continue state, hidden radar targets and legal movement previews |
| language0202 | 1,227 | English/Ukrainian/Dutch menus, dynamic messages, lore, viewport layouts and actual upgrade hover tooltips |
| refinement021 | 172 | Staged simultaneous salvo, actual pointer/touch choices, camera timing and encounter privacy |
| menu | 150 | Title, colors, new game, persistence and reload |
| victory | 110 | Staged victory, statistics and modal dismissal |

The tooltip regression fixture now scrolls the upgrade choice into view and uses its native canvas transform before moving the pointer. The translated-tooltip assertion remains intact. Actual PNG captures were inspected, including the previously over-tall Ukrainian upgrade parchment, the scale slider and towns at different levels.

After the final Debug build, both mandatory native frame-pacing scenarios ran serially without competing builds or tests:

| Scenario | Pan p95 / maximum | Interaction p95 / maximum | Maximum hover |
| --- | --- | --- | --- |
| Pangaea, 1,371 tiles, four rivals, wide view | 17.332 / 33.258 ms | 17.103 / 50.678 ms | 0.619 ms |
| Oceans, 1,371 tiles, four rivals, live fog and disposable autosave | 19.187 / 40.325 ms | 18.032 / 64.421 ms | 0.660 ms |

Both passed the unchanged limits: pan p95 ≤25 ms / maximum ≤80 ms; interaction p95 ≤25 ms / maximum ≤100 ms; hover ≤16 ms. These measurements establish performance on this machine and these representative cases, rather than all hardware or every possible late-game fleet.

The exported Windows executable reports product version 0.20.4 and file version 0.20.4.0. Native release startup checks exit successfully for English, Ukrainian, Dutch and map preview, with no engine errors. Their frame captures and logs are included here. Movie captures verify the release visuals, not frame pacing. Windows packaging includes the PCK and complete .NET runtime directory.

Packaging verifies ZIP integrity, SHA256 copies, source completeness against the preserved v020.3 archive, the unchanged beta roadmap and every historical output manifest. The repository release mirror contains the same playable files and both archives. Primary Git HEAD and index are preserved when publishing the separate release snapshot.
