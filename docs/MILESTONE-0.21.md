# Ancient Naval 0.21 — Encounters and Salvos

2 October 2026. The authoritative source is `C:/__Beygelman/! -11/dev-ancient-naval`. The Windows application, full source archive, screenshots and Russian notes are mirrored in `releases/0.21/` and the external outputs folder. Older releases and player save slots remain intact.

## Play changes

- Command papyri form a symmetric lower arc beneath ship/town progress slots. Hull size sets the distance of progress from the ship center. Info remains rightmost; shipyard controls stay beside their vessel.
- Ready harbor-claim and treasury scenes automatically unfold once into horizontal banners above their targets. They stay open through selection changes, temporary command/menu locks and offscreen trips. Activation starts a 0.42-second burn immediately; Core commits once after it finishes and revalidates the original target.
- New voyage movement: Mothership 2, Fishing Schooner 3. Brig guns reach 2 cells. Galleon costs 7 Thors; Kolonel costs 12 and has 4 base cannon damage.
- Hold a valid cannon target at least 0.4 seconds and release: Kolonel reserves both attacks and fires twice for 4 base damage each. A Mothership requires Second Attack and retains its own cannon damage. A surviving target replies once after the combined salvo; mortar cannot use this gesture. A first-shot kill skips the unnecessary second flight while keeping both charges spent. Mouse and touch work; dragging/pinching cancels a held shot. Long presses without a salvo target retain normal selection/movement behavior.
- Flagship levels 2 / 3 / 4 / 5 award 2 / 4 / 5 / 7 Thors. Seeing the first ship of each rival nation in actual optical sight awards 5 Thors once; radar and God's eye do not identify a nation. Encounter identity and paid progression are saved, and rewards contribute to voyage statistics.
- Visible ship/town attacks, replies, outpost shots and bombs lead with a 0.25-second camera approach. First encounters use 0.5 seconds. Quintic easing gives zero endpoint speed/acceleration, with duration independent of distance. Manual pan cancels the flight without leaving a command stuck. Targets hidden in fog do not become camera clues.
- Currency and turn use a compact top plaque; a separate colored plaque below it names known captains. Before contact it reads “Other nations are taking their turns”. Opponent action messages no longer appear at the center.

## Compatibility and evidence

New optional saved rule fields default to disabled/zero when absent: `DoubleSalvo`, `EncounterCurrencyReward`, `LevelCurrencyRewards`. Missing encounter history is accepted; malformed, duplicate, off-board or out-of-roster encounters are rejected. Continue retains the saved numeric balance. Start New game for 0.21 prices, movement and paid rewards. The existing organic hexagon, six fleet colors, Pangaea waterways, navigation, treasury RNG, clay badges and victory system are retained.

| Executed check | Result |
|---|---|
| C# source build | 0 errors, 0 warnings |
| Pure Core, progression, worlds, saves and statistics | 1,203,204 checks; eight complete AI matches |
| New salvo/encounter/reward/validation Core scenarios | 38 checks included above |
| Real mouse/touch holds, staged shots, lower fans, camera timing and nation privacy | 141 checks |
| Six fleets, villages, ports and prices | 48 checks |
| Claim/treasury gates, real pointer activation and no repeated reveal | 46 checks |
| Full-circle fleet art, clay transitions and radar privacy | 666 checks |
| Menu / separate process Continue | 142 / 120 checks |
| Battle input / mouse-touch-camera smoke / retained renderer | 83 / 248,659 / 11 checks |
| Effects / victory with encounter income | 121 / 107 checks |
| City and port controls | 12 checks |
| Exported Windows title / map startup | Exit 0 / exit 0; no error logs |

The shipping Release template deliberately disables automated test hooks; held-salvo checks run in the native debug runtime against the same source. Release startup is probed separately.

Graphical checks run using the installed Godot 4.7.2 .NET renderer and actual viewport input events. Assertions distinguish the camera, aiming, shell flight, impact and reply stages: Core health is unchanged until the corresponding staged impact. Town salvos respect fortification resistance and return the camera to the attacked vessel before its reply. Logs and selected screenshots are in `diagnostics/0.21/`.

## Performance measurements and remaining limit

Fixed the initial drag-threshold camera jump and intermediate zoom/UI transforms. Hover input is coalesced to one dispatch per frame. Route forecasts use local mesh-radius queries instead of full-map origin distance tables. Sea submissions retain their native buffers. Saves serialize the captured DTO directly to UTF-8 on the existing worker, eliminating intermediate JSON string/document clones.

A matched fully revealed 1,371-cell Pangaea / four-rival workload reduced Save.Write managed allocation from 6,341,224 to 1,491,408 bytes, and its measured elapsed time from 185.1 to 48.5 ms. Overlay allocation per draw fell from about 84.7 KB to 9.5 KB; fish allocation per draw from about 38.8 KB to 7.5 KB. These are operation measurements, not a whole-game FPS claim.

The last ordinary-fog Pangaea run on the local RTX 4060 Laptop GPU measured camera-pan p50 16.70 ms / p95 17.02 ms / maximum 17.86 ms (119 intervals), hover-handler p95 0.094 ms (64 calls), and interaction frame p95 17.16 ms. A 104.45 ms outlier remained during movement/discovery. Other runs on this active desktop showed larger outliers, including on the fully revealed chart; the diagnostics record phase, GC and renderer compilation counters. The last outlier coincided with neither a managed collection nor new canvas/draw pipeline compilation. This release fixes the observed input/transform jumps and reduces repeated work, but does not claim every discovery/rendering stall is eliminated. The alternate OpenGL measurement did not justify changing the default D3D12 Mobile renderer.

Hold-to-fire instructions and New game / Continue compatibility are included beside the application in `READ_ME_RU.md`. This remains a playable local prototype rather than a final competitive balance or a universal frame-rate guarantee.
