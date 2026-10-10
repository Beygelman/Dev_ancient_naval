# Ancient Naval — v020.8b

**v020.8b · 7 October 2026.** Godot 4.7.2 .NET, C# and .NET 8.
The canonical source contains the new world, interface and opponent changes.
The portable v020.8b player below contains these changes. Older ZIPs remain
historical builds.

An ancient floating city carries the last hope of its people. Explore an organic
six-sided sea, protect your Mothership, claim coastal settlements and defeat
1–4 rival fleets. Choose Sea World, Oceans, Continents or Pangaea; three AI
difficulties; six nations; optional pirates, hints and God's eye. English,
Ukrainian and Dutch are supported.

## What changed in v020.8b

### iPhone preparation — 10 October 2026

The same v020.8b source now includes landscape touch controls, iPhone safe-area
layout, stable-order background saves and generated JSON metadata for NativeAOT.
Historical save defaults, simulation RNG and gameplay remain unchanged.

Use the separate [Russian iPhone installation guide](docs/INSTALL-iPHONE-RU.md).
`bash tools/Build-iOS.sh` on a Mac with full **Xcode 26+**, Godot **4.7.2 .NET** and
**.NET SDK 8.0.425** prepares a complete unsigned Xcode project in an isolated
directory. The manual **iOS — unsigned Xcode project** GitHub Actions workflow
performs the same export/build. Sign the resulting project with your own Apple
Account and run it on the physical iPhone; the unsigned ZIP is not an installer.
The same build also packages the arm64 device app as an **unsigned IPA** for
personal signing/install through AltStore Classic and AltServer on Windows.
The separate guide explains both routes; no Apple credentials are stored in CI.
The optional simulator workflow checks startup separately; a source handoff can
be reproduced with `tools/Package-iOSSource.py` from reviewed committed Git blobs.
NativeAOT export, unsigned Xcode device build and artifact packaging passed in
[cloud run 38016489220](https://github.com/Beygelman/Dev_ancient_naval/actions/runs/38016489220).
The [iPhone IPA](releases/v020.8b/ios-2026-10-10/Ancient_Naval_v020.8b_iPhone_UNSIGNED.ipa)
is ready for personal signing through Windows AltStore Classic. The complete
Xcode ZIP is also retained locally in that release folder (182 MB, over Git's
regular-file limit); both original native hashes are in `SHA256-iOS.txt`.
The optional simulator check is being repeated on an Intel runner to match the
official .NET Godot Simulator engine archive; physical-device launch is not claimed.

**Validation limit:** Core compatibility, generated JSON, native synthetic touch,
iOS compilation, arm64 device/framework structure and PCK cleanliness passed.
Device signing, GPU performance and real-device behavior still require iPhone
validation. See
[iOS preparation report](docs/IOS-PORT-v020.8b.md) for the recorded result.

### Gameplay checkpoint

- Water falls from explored chart edges into a black void. One retained shader
  mesh animates the waterfall without rebuilding terrain each frame.
- Smaller navigation ring with light fading outward. Arrows point to ready
  actions outside the ring even when those objects are still on screen.
- A larger nation relic rises from the bottom edge: touch its upper monument
  to end the turn, the inset number to visit the next crew with useful actions.
- Dark ink in all paper control states; scrolling any window never zooms the
  sea, even at the top/bottom of a reading area. Advice is smaller and rejects
  empty topics; Mothership double shots trigger advice before Kolonel unlock.
- Primary health/movement/resource facts precede additional lookout and crew
  details. Cleaner ship silhouettes and translated nation names aid selection.
- Green, White and Purple shrine rituals are richer; Purple has a taller
  engraved silver reliquary. Owned shrines flare when an opponent turn starts.
- New voyages: Granado movement **3** and **four cautious pirate opening rounds**.
  Attacked pirates can defend. Historical voyages retain their saved policies.
- Captain/Admiral invest in paired ports, relay beacons, gun towers, research
  and reserves; escorts screen endangered flagships retreating toward safe bases.

See [v020.8b verification](docs/MILESTONE-v020.8b.md) for actual checks and limits.
Historical world/RNG/rules snapshots and serialized ship identities are preserved.

## Previous checkpoint — v020.7b

- Close coastlines and grid geometry use bounded high-resolution retained
  chunks; town names use sharper glyphs. One distant object LOD reduces draw
  submissions at full-chart zoom while preserving fog and depth ordering.
- Fog changes prepare their native graphics per scheduled chunk, avoiding the
  previous movement spike. Two consecutive serial native frame gates passed.
- World-anchored command paper wraps farther around its object as the chart
  zooms out. Icons and prices keep their size; WASD movement is faster.
- Selected-object, claim, treasury and salvo papers keep their world anchors
  beyond the screen. Offscreen directions appear on a circle whose radius is
  30% of the viewport's shorter side: blue for claims, gold for treasuries and
  red for recent damage to owned towns or the Mothership. Click to fly the
  camera there; the bottom-center compass returns to the Mothership.
- Select an object for a larger nation-styled counsel card with grouped stats
  and clear glyphs. Narrow layouts leave room for the compass. The redundant
  Info button and separate popup are removed.
- Visible command positions reserve **1–9**, left to right. Grey commands cannot
  execute. Shipyard pages assign their own numbers; Right-click clears selection.
- A glowing nation monument replaces the large ready-action jug. It remains
  visible without hints. Click repeatedly to visit useful actions in order.
  The hints-on end-turn confirmation alone shows object/action pictograms.
- New voyages: Granado movement **4**; built Cannon Tower damage **5**, with
  full damage through town walls. Ancient guns keep their existing behavior.
- Scuttling has a sinking-ship brush icon and **always asks for confirmation**,
  with the actual refund shown, whether hints are enabled or disabled.
- Screenshot advice explains flagship resources, village growth, double salvos,
  veterancy and command keys using the rules stored in the voyage.
- Merchant art, varied mountain chains, rounded clouds and purple silver
  pyramid engravings retain the game's ancient visual style.
- Owned shrines give one finite 1.1-second, 30-Hz light pulse per human round. The
  effect stops processing when hidden and changes no gameplay or random state.

Historical voyages preserve their embedded balance, world geometry, RNG and
progress. A missing new tower wall-bypass field defaults to the old rule.
No historical player/source archive is replaced.

## Play v020.8b on Windows

Download [the Windows ZIP](releases/v020.8b/Ancient_Naval_v020.8b_Windows.zip),
extract **the entire folder**, then start **Ancient Naval.exe** inside
`Ancient Naval v020.8b`. Keep the PCK and complete runtime folder beside it.
Godot and a separate .NET installation are not needed.

Canonical local launch:
`releases/v020.8b/playable/Ancient Naval v020.8b/Ancient Naval.exe`.
The [source ZIP](releases/v020.8b/Ancient_Naval_v020.8b_Source.zip),
[SHA256 manifest](releases/v020.8b/SHA256-v020.8b.txt) and
[delivery notes](docs/RELEASE-v020.8b.md) accompany it. Both unchanged native
frame-pacing gates passed; raw and ZIP-extracted players were checked in all
three languages and in map preview.

## Previous portable Windows checkpoint

Download [the portable Windows checkpoint](releases/v020.7b/Ancient_Naval_v020.7b_Windows_PLAYER_CANDIDATE.zip),
extract **the entire folder**, then run **Ancient Naval.exe** inside
`Ancient Naval v020.7b`. Keep the PCK and complete runtime folder beside it.
Godot and a separate .NET installation are not needed to play.

The canonical local launch copy is `releases/v020.7b/playable/Ancient Naval.exe`.
The [source archive](releases/v020.7b/Ancient_Naval_v020.7b_Source_CANDIDATE.zip) and
[SHA256 manifest](releases/v020.7b/SHA256-v020.7b-candidate.txt) accompany this player.
This is a playable verification candidate: current native frame-pacing approval
is pending; the thresholds were not relaxed to certify it as a final release.
See [portable delivery notes](docs/RELEASE-v020.7b.md) for validation and scope.

## Play from source

Open `project.godot` in **Godot 4.7.2 .NET**, stop a previous run with **F8**,
then press **F5**. The title screen offers New game, Continue, Settings and Exit.
Both title and in-voyage menus read the checked-in version/date signature.

- Select a ship and choose a legal tile or known target.
- **WASD / arrows:** move the chart. **Wheel:** zoom. **R:** repair.
- **1–9:** current parchment commands. **Right-click:** cancel/clear selection.
- Touch the nation relic to end the turn; its numeral cycles useful actions.
- Offscreen arrows and the Mothership compass move the camera without claiming,
  looting or attacking. Red alerts start only after committed damage finishes
  presenting; clicking dismisses them, and they expire after the following round.
- Panels consume their own scrolling; modal choices isolate the sea behind them.

## Economy and compatibility

Each port earns **1 Thor per other connected friendly port city**, including
lighthouse relays. Isolated ports earn 0; four linked cities earn 3 each.
Port price is **6**, shipyard discount **20%**, and lane movement grants at least
**+2 tiles or +20%**, whichever is greater. Support Brig costs **5**, Brig **6**,
Galleon **9**, Kolonel **16**, Granado **22**, radar **4**, and flagship mortar
**14**. Older saves retain their historical prices and port-income policy.

Core owns all damage, economy and turns. Projectile counts and animation timing
cannot change combat results. God’s eye affects human display only; radar never
reveals hidden identity, health or destruction details.

## Development and verification

Canonical working folder: `C:/__Beygelman/! -11/dev-ancient-naval`.
Read [AGENTS.md](AGENTS.md) and [architecture](docs/ARCHITECTURE.md) before changes.
Update this README and document each reviewed GitHub `main` commit.

```powershell
dotnet build Dev_ancient_naval.csproj
dotnet run --project tests/CoreChecks/CoreChecks.csproj --configuration Release
```

Native graphical checks use explicit disposable `--save-file=` and
`--ui-settings-file=` paths. Run frame gates serially after the final Debug build;
headless results do not certify smoothness. See
[the v020.7b checkpoint](docs/MILESTONE-v020.7b.md) for actual evidence and limits,
and [CHANGELOG](CHANGELOG.md) for earlier development.

The local checkpoint `9ccfa57` includes a 109,413,888-byte raw executable that
exceeds GitHub's regular-file limit. Preserve that commit under a local backup
ref and keep its files. Publish reviewed source commits based on remote `299d99c`,
excluding only the newly extracted
`releases/v020.7/corrected-2026-10-05/Ancient_Naval_v020.7_Windows_PLAYER_CANDIDATE/`
folder. Historical ZIPs and their recorded hashes remain unchanged.

## Download and local release copies

The working project, sources, assets, balance, tests and this README are edited
directly in this Git repository. Delivered files are also copied to `releases/`
inside the same project; the external `outputs` folder is an additional mirror.

- [Windows v020.8 ZIP](releases/v020.8/Ancient_Naval_v020.8_Windows.zip): extract the complete folder and launch `Ancient Naval.exe`.
- [Source v020.8 ZIP](releases/v020.8/Ancient_Naval_v020.8_Source.zip), [Russian notes](releases/v020.8/Ancient_Naval_v020.8_Notes_RU.md), [SHA256 checksums](releases/SHA256-v020.8.txt).
- Local launch copy: `releases/v020.8/playable/Ancient Naval.exe`.

- [Windows v020.7 ZIP](releases/v020.7/Ancient_Naval_v020.7_Windows.zip): extract the complete folder and launch `Ancient Naval.exe`.
- [Source v020.7 ZIP](releases/v020.7/Ancient_Naval_v020.7_Source.zip), [Russian notes](releases/v020.7/Ancient_Naval_v020.7_Notes_RU.md), [SHA256 checksums](releases/SHA256-v020.7.txt).
- Local launch copy: `releases/v020.7/playable/Ancient Naval.exe`.

- [Windows v020.6 ZIP](releases/v020.6/Ancient_Naval_v020.6_Windows.zip): extract the complete folder and launch `Ancient Naval.exe`.
- [Source v020.6 ZIP](releases/v020.6/Ancient_Naval_v020.6_Source.zip), [Russian notes](releases/v020.6/Ancient_Naval_v020.6_Notes_RU.md), [SHA256 checksums](releases/SHA256-v020.6.txt).
- Local launch copy: `releases/v020.6/playable/Ancient Naval.exe`.

- [Windows v020.5 ZIP](releases/v020.5/Ancient_Naval_v020.5_Windows.zip): extract the complete folder and launch `Ancient Naval.exe`.
- [Source v020.5 ZIP](releases/v020.5/Ancient_Naval_v020.5_Source.zip), [Russian notes](releases/v020.5/Ancient_Naval_v020.5_Notes_RU.md), [SHA256 checksums](releases/SHA256-v020.5.txt).
- Local launch copy: `releases/v020.5/playable/Ancient Naval.exe`.

- [Windows v020.4 ZIP](releases/v020.4/Ancient_Naval_v020.4_Windows.zip): extract the complete folder and launch `Ancient Naval.exe`.
- [Source v020.4 ZIP](releases/v020.4/Ancient_Naval_v020.4_Source.zip), [Russian notes](releases/v020.4/Ancient_Naval_v020.4_Notes_RU.md), [SHA256 checksums](releases/SHA256-v020.4.txt).
- Local launch copy: `releases/v020.4/playable/Ancient Naval.exe`.

- [Windows v020.3 ZIP](releases/v020.3/Ancient_Naval_v020.3_Windows.zip): extract the complete folder and launch `Ancient Naval.exe`.
- [Source v020.3 ZIP](releases/v020.3/Ancient_Naval_v020.3_Source.zip), [Russian notes](releases/v020.3/Ancient_Naval_v020.3_Notes_RU.md), [SHA256 checksums](releases/SHA256-v020.3.txt).
- Local launch copy: `releases/v020.3/playable/Ancient Naval.exe`.

- [Windows v020.2 ZIP](releases/v020.2/Ancient_Naval_v020.2_Windows.zip): extract the complete folder and launch `Ancient Naval.exe`.
- [Source v020.2 ZIP](releases/v020.2/Ancient_Naval_v020.2_Source.zip), [Russian notes](releases/v020.2/Ancient_Naval_v020.2_Notes_RU.md), [SHA256 checksums](releases/SHA256-v020.2.txt).
- Local launch copy: `releases/v020.2/playable/Ancient Naval.exe`.

- [Windows v0.20.2 ZIP](releases/0.20.2/Ancient_Naval_0.20.2_Windows.zip): extract the complete folder and launch `Ancient Naval.exe`.
- [Source v0.20.2 ZIP](releases/0.20.2/Ancient_Naval_0.20.2_Source.zip), [Russian notes](releases/0.20.2/Ancient_Naval_0.20.2_Notes_RU.md), [SHA256 checksums](releases/SHA256-0.20.2.txt).
- Local launch copy: `releases/0.20.2/playable/Ancient Naval.exe`.

- [Windows 0.21 ZIP](releases/0.21/Ancient_Naval_0.21_Windows.zip): extract the complete folder and launch `Ancient Naval.exe`.
- [Source 0.21 ZIP](releases/0.21/Ancient_Naval_0.21_Source.zip), [Russian notes](releases/0.21/Ancient_Naval_0.21_Notes_RU.md), [SHA256 checksums](releases/SHA256-0.21.txt).
- Local launch copy: `releases/0.21/playable/Ancient Naval.exe`.

- [Windows 0.20 ZIP](releases/0.20/Ancient_Naval_0.20_Windows.zip): extract the complete folder and launch `Ancient Naval.exe`.
- [Source 0.20 ZIP](releases/0.20/Ancient_Naval_0.20_Source.zip), [Russian notes](releases/0.20/Ancient_Naval_0.20_Notes_RU.md), [SHA256 checksums](releases/SHA256-0.20.txt).
- Local launch copy: `releases/0.20/playable/Ancient Naval.exe`.

- [Windows 0.19 ZIP](releases/0.19/Ancient_Naval_0.19_Windows.zip): extract the entire folder and run `Ancient Naval.exe`.
- [Source 0.19 ZIP](releases/0.19/Ancient_Naval_0.19_Source.zip), [Russian release notes](releases/0.19/Ancient_Naval_0.19_Notes_RU.md), [SHA256 checksums](releases/SHA256-0.19.txt).
- A complete local launch copy is at `releases/0.19/playable/Ancient Naval.exe`, alongside its PCK and .NET runtime files.

The raw executable exceeds GitHub's 100 MiB limit, so only that unpacked launch
folder is excluded from Git; the complete Windows ZIP, sources, screenshots and
notes remain visible to Git. [GitHub size limits](https://docs.github.com/en/repositories/working-with-files/managing-large-files/about-large-files-on-github).
Godot ignores the release directory so copied exports never re-enter game assets.
See [release-copy workflow](releases/README.md). Files reach GitHub after the
repository changes are committed and pushed; copying files alone is not a push.

## v020.6 — 4 October 2026 — The Watchful Voyage

Optional guidance shows a nation-colored amphora counting objects with useful affordable actions. Ending the turn stamps a left-unrolled parchment and, with hints enabled, opens a five-column object confirmation. Reward, capture and relic guidance sits below the scrolls. Five closeable upper-left advice cards use native game screenshots and appear once per voyage for resources, Kolonel, trade, repair and radar. Both Settings menus contain the toggle; disabling hints bypasses confirmation. English, Ukrainian and Dutch are supported.

New voyages remove combat ship upkeep, increase settlement income and dock sight, give Granado three movement and mortar range two through five. Double salvo uses two equal damage charges. Starting escorts leave a tile between themselves and the flagship; the outer ring stays empty, fish are slightly more frequent and pirate counts follow area size. Tower/lighthouse construction removes a treasury without looting it. White pencil construction ghosts include prospective lighthouse trade links.

Uniform house scale and compact inland plans prevent miniature settlements. Rivers favor coves and avoid narrow necks/beaches away from mouths; estuaries retain ocean tint. Faction shrines emit different light/foliage effects, including the white nation's red R. More bounded gulls flee nearby gunfire; waves gain subtle relief. Solid wreck parts rotate and submerge independently: decks, houses, broken sanctuaries, falling masts and rolling guns.

[Validation and compatibility](docs/MILESTONE-v020.6.md). Start a new voyage for revised rules; older saves retain their embedded balance.

## v020.5 — 3 October 2026 — The Painted Voyage

New voyages open on an aged rolling parchment: fleet emblem, independent Lake/Bay/Sea/Ocean size, 1–4 rival figures, painted difficulty signs and four world illustrations. Wheel scrolling has no visible scrollbar. Embark leaves a four-finger handprint, folds the scroll, fades the view and descends toward the flagship before the elders' welcome. Language and interface scale are in Settings on both menus; English, Ukrainian and Dutch remain available.

Fishing is now displayed as Support Brig, with workshop houses and a shared workshop icon. It collects supplies and builds docks, cannon towers and lighthouses without passive income. Repair restores 3 HP and spends the turn's remaining actions. New paid town levels cost 5/7/10/15 Thors; fishing docks yield +2. Direct port routes span at most six navigable tile transitions. Hovering a legal construction site shows a wireframe volume; cannon towers show fire range and lighthouses optical sight. Hidden/illegal sites have no blueprint.

Towns use a consistent medium footprint fitted to adjoining inland ground, with five to seventeen homes and taller buildings as levels rise. Stronger within-cell river bends have variable banks and translucent estuaries cutting the painted beach. These illustrations do not change navigation. Uncollected sea ruins shine upward; collection extinguishes their glow and removes their information/actions while their scenery remains.

The actual two-Thor flagship level reward is retained; its redundant upgrade-choice label is removed. Older saves retain their embedded balance; absent new size/port/support fields preserve legacy behavior. Start a new voyage for the complete new rules.

Validation and captured native frames: [v020.5 milestone](docs/MILESTONE-v020.5.md). The working checkout had committed merge markers; complete pre-repair copies were preserved externally and current released code was recovered before development. No user Git checkout/index or historical release archive was replaced.

## v020.4 — 3 October 2026

Vertical parchment panels are wider and bounded to 60% of the viewport with scrolling. Interface scale (80–125%) is adjustable on the title screen and in the game menu without changing the world or hit positions. Every flagship level grants 2 Thors. New voyages use paid town upgrades (5, 8, 12, 16 Thors), with the upgrade icon centered on the town's command arc. Repair ends the object's other actions for that turn.

Three small merchant skins travel the owned port routes, departing every 12–20 seconds. They are decorative and reveal no hidden enemies. River placement avoids towns and peaks; bends receive quiet depth shading and branches join rather than crossing. Early town homes are larger, the shrine gains a round irregular plaza, and beige port roads stay behind houses and mills. Clay badges and shipyard icons share one motif family; veteran amphorae have a longer neck and two burgundy bands.

[Validation and compatibility](docs/MILESTONE-v020.4.md). Select **New game** to adopt paid town progression; older saves keep their rules.

## v020.3 — 3 October 2026

Dynamic fleet capacity, worn thin parchment, explicit saved reward claims and keyboard controls. Revised sight, mortar minimum range, fishing radar contacts, veteran gun reach, repairs and scuttling. Admiral assembles next-turn attacks and searches remembered flagship positions without hidden information. Cities are spaced more than three cells apart; pirate bays do not grow until captured. Cosmetic rivers, varied snow-capped mountains, abandoned architecture and persistent sea ruins use retained rendering.

Arrows/WASD pan; Space ends the turn; R repairs the selection. Select **New game** to adopt the new rules; earlier voyages retain their saved rules.

[Validation and compatibility](docs/MILESTONE-v020.3.md).

## Corrected v020.2 — 2 October 2026

- Selecting a cannon target with a two-shot ship opens an icon-only parchment under that target. Choose one cannonball or two; a double salvo launches both charges together and receives one reply. Claim and treasury banners use the same parchment size family as object commands.
- Reduce existing unit sight by one tile. Admiral coordinates firing and same-turn advances against known targets, evaluates return fire and preserves charges on nearly sunk targets. Captain recovers when a newly discovered obstruction blocks its planned route.
- Replace repeated per-line drawing with buffered sea geometry, split terrain into bounded raster regions and update local fog regions one per frame. Bake static town layers once; pan, zoom and hover reuse their caches. Use the verified OpenGL Compatibility renderer for the 2D game to avoid the repeated first-move D3D12 stall on this host. The release workflow now requires native frame-pacing checks before packaging.
- Galleon cannon range increases from one to two tiles for new voyages. Shared smooth island surfaces remove pointed sand-tile corners and water cuts; internal lakes stay open. Larger inland summits have smaller connecting peaks, and denser continuous groves leave meadow regions.
- Object commands wrap around the selected object in one-eighth-circle sectors, extending beyond a semicircle as command count grows. Mystical upgrade names reveal precise effects in hover parchment. Compact land-fitted towns gain taller square walls and four corner towers; ship and tower mortars have detailed short bombard carriages.
- Number-only hit feedback floats above health amphorae. Victory and defeat screens can be dismissed to inspect the map. Leaving a finished voyage removes its save and backup; active voyages can still Continue.
- Move voyage setup to the right and support live English/Ukrainian/Dutch switching with an active-language marker and a persisted language preference.
- Add a buildable Lighthouse among rocks at a sea tile's upper-left corner: sight 4, optional radar, port trade links. Shared trade paths become a single dashed network. Towns use spaced, varied isometric buildings and separate rear-house, mill, wall and wheat layers.

Use **New game** to adopt the new balance and optional rules. Existing saves keep their saved rule set. Historical `0.20.2` archives remain unchanged; this correction is delivered separately as `v020.2`. Future versions follow `v020`, `v020.1` through `v020.9`, then `v021`.

[Validation and compatibility](docs/MILESTONE-v020.2.md).

## Changes in v0.20.2 — 2 October 2026

This user-requested version label builds on the completed 0.21 checkpoint and retains its salvos, camera flights, claim banners, ports and save compatibility.

- Fish resources use approximately 70% of the previous map budget, with one guaranteed first catch per fleet. Resources and coastal settlements concentrate toward the center; Pangaea retains its inland river/lake emphasis.
- Mothership improvements require **3 / 4 / 5 / 6 resources**. New maps guarantee two black, fortified level-3 pirate bays; ordinary neutral towns sometimes start at level 2 or 3.
- Each fifth personal turn grants the player **2 Thors per living owned town and Mothership**. Rivals receive **2 Thors per enemy Mothership they directly destroyed**. A top plaque and finite rays announce the reward without revealing unknown nations.
- Brig and Fishing Schooner evade radar. Mountains cast optical shadows, while radar coverage remains unaffected.
- Sparse groves alternate with plains. Inset mountain ranges follow island geometry, beaches vary in width and fill rounded land cutouts, and deeper sea is slightly darker. Town soil is irregular and clipped away from beaches; wheat sits nearer houses, walls divide it from the houses, taller towns gain taller monuments, and flags remain attached to their poles.
- Expanded Latin-letter captain and town names follow six color themes: colonial British, Egyptian, Japanese, German, woodland fantasy and Andean fantasy; pirate bays have their own names. Continue retains saved identities and rules.

[Validation and compatibility](docs/MILESTONE-0.20.2.md).

## Changes in 0.21 — 2 October 2026

- Place symmetric command papyri beneath ship/town progress, with size-dependent hull progress distance and a compact arc inset. Keep Info on the right.
- Ready claim/treasury artwork unfolds once as a straight horizontal hovering banner. Selection, temporary locks and camera visibility do not restart it. Activation burns immediately for 0.42 seconds before the one validated commit.
- New voyage balance: Mothership move 2, Fishing Schooner move 3, Brig range 2, Galleon price 7, Kolonel price 12 and cannon damage 4.
- Hold a cannon target for at least 0.4 seconds, then release to spend two shots: Kolonel fires twice for 4 base damage each; a Mothership needs Second Attack and uses its own cannon damage. The salvo receives one nonrecursive counterattack. Radar outcomes stay anonymous.
- Pay Mothership level rewards of 2 / 4 / 5 / 7 Thors at levels 2 / 3 / 4 / 5. First optical contact with each rival nation grants 5 Thors once. Radar and God's eye do not identify nations or generate encounter income.
- Lead visible attacks/counterattacks/outpost shots/bombs with a 0.25-second camera flight; focus a first encounter in 0.5 seconds. Quintic easing starts and stops at zero speed, independent of distance; manual pan cancels a flight safely.
- Compact the currency/turn plaque. A separate colored top plaque names met captains and uses “Other nations are taking their turns” until contact. Remove central opponent messages.
- Coalesce hover input to one frame, retain sea submission buffers and build local radius forecasts rather than whole-map distance tables. Synchronize world UI with the final camera transform; save directly to UTF-8 without intermediate string/JSON-document clones.

[Validation and profiling](docs/MILESTONE-0.21.md). Start **New game** for the new numeric balance and paid rewards; **Continue** keeps the saved voyage rules.

## Changes in 0.20 — 1 October 2026

- Automatically show ready claim/treasury scenes at their world targets. Dark-burgundy dry-brush artwork opens downward from a top hinge, floats, rolls inward and burns before capture/rewards commit. Remove yellow capture/loot action icons and the old capture-marker click region.
- Restore ship/town/resource action fans beside the world object. Add Thor coins and prices above purchase icons, and zero-cost labels on level rewards. Center the voyage setup on parchment with six fleet emblems, including Red.
- Share color-specific monuments between Motherships and owned towns: gold dome, silver faceted cap, pink pyramid/white diamond, black cross, dark-green tree and red crystals. Neutral houses, varied roof shades, growing/taller buildings, foreground wheat, detailed tower masonry and woven balloon baskets.
- Pangaea gains connected lake basins, winding arteries, dead-end branches and narrow bypasses. Prioritize two inland villages and one outer village per territory, with more interior resource fish.
- Enable unrestricted narrow/coastal sailing for all hulls in new voyages, and anti-air cannons for Kolonel. Saved optional rules keep older voyages' policies. Curved white trade routes meet ports straddling the town/sea edge, with roads to town centers.

[Validation and limits](docs/MILESTONE-0.20.md), [new assets and generation prompts](docs/ACTION-ART-0.20.md). Start **New game** to generate the new waterways and use the new rule settings; **Continue** preserves the saved voyage.

## Changes in 0.19 — 1 October 2026

- Add a dimmed, glowing **VICTORY** result with finite fireworks, the story of the saved people, currency earned, ships built, directly defeated enemy vessels and enemy flagships, plus **Return to menu** / **Exit game**. The result waits for the last shell and every sinking animation. Voyage totals survive Continue and staged impact snapshots; older saves start these newly recorded totals at zero.
- Show paired clay amphorae above-right of every observed ship, tower, dock, balloon and settlement: a darker fleet-color HP jug with beige digits, and a rear beige class seal. Four shapes cover intact / 75% / 50% / 25% health; damage shakes/chips both, healing rebuilds and glints twice. No class/HP is revealed for radar-only targets, including during their destruction.
- Give ready harbor claims and treasury discoveries wide illustrated Egyptian-style papyri. Click the scene or its usual action icon; the scroll rolls and burns **before** ownership/rewards change. Locked crews do not receive a ready story. [Bundled assets and generation prompts](docs/ACTION-ART-0.19.md).
- Move the upward-opening action fan beside the bottom-left object ledger, preserving Info as the rightmost icon. Panning the sea leaves this interface in place.
- Render every ship in the flagship's volumetric floor/height style: rounded timber hulls, raised decks, plaster cabins, shaded sails, rigging, gun carriages and fishing gear. Dock houses/quays gain volume. Deck height stays upright throughout a full turn. Clouds become coherent translucent solids with shaded sides above their offset shadows.
- Choose four terrain policies on the same organic six-sided mesh: **Sea World** has 1–3-cell island chains; **Oceans** preserves the existing generator; **Continents** has 15–30-cell landmasses and shallow channels; **Pangaea** has a large central land envelope crossed by rivers, lakes and bays. Rival-count scaling remains; new modes offer three coastal villages per territory and equal treasury opportunities. The world kind is saved; older voyages default to Oceans without regenerating their map.
- Bake the original trees and peaks into shared high-resolution textures once per world. Each object keeps its own ground anchor and fog tint; hovering, panning and visibility changes reuse the textures. This removes the fully revealed Pangaea's small-primitive rendering bottleneck. Transparent edges retain the original colors.

[0.19 validation, screenshots and limits](docs/MILESTONE-0.19.md). Sources, README and complete release copies are updated inside this repository; previous checkpoints remain intact.

## Changes in 0.18.1 — 1 October 2026

- Replace dense information paragraphs with a short keeper's description and separated, labelled rows: current stats, weapons, crew/economy and installed improvements.
- Show only upgrades already installed on the inspected object. Unbuilt ports/outposts and future level rewards no longer fill its description. Shipyards list only classes available now; combat and repair numbers follow the voyage's saved rules.
- Keep the papyrus card inside the viewport, with a fixed title/close button and a scrolling body for long equipment lists. Town cards use the settlement's own name. Radar-only contacts retain their anonymous short description.
- Preserve the ports, trade movement, difficulty, God’s eye, balance and rendering fixes from 0.18. [Validation and screenshots](docs/MILESTONE-0.18.1.md).

## Changes in 0.18 — 1 October 2026

- Remove repeated sea drawing calls, reuse world-space overlays during camera pans, and stop hiding/re-showing hull canvases every frame. Bobbing uses retained canvas transforms; gun turns and impacts still redraw correctly. See [measured before/after results](docs/MILESTONE-0.18.md).
- Town names are black; names, HP, progression and capture marks draw above ships, scenery and clouds. Info occupies the rightmost sector on both action and resource scrolls. Retained town HP refreshes after manual repair.
- Add **God’s eye** to the game menu, with full human visibility and ordinary AI fog. Victory automatically opens the whole chart. The setting and genuine exploration memory survive Continue.
- Choose **Boatswain**, **Captain** or **Admiral** before sailing. Captain preserves the earlier decision policy; Boatswain builds smaller fleets and uses simple targets. Admiral estimates counterfire and next-turn exposure, focuses available guns, sends a cheap scout toward radar, retreats wounded flagships and rebuilds its economy/fleet.
- Lower Mothership base HP to **15**, Brig price to **5**, Galleon **10**, Kolonel **18**, Granado **16**, Cannon Tower **6**, Fishing Schooner **4** and Fishing Dock **8**. Existing voyages retain their saved balance.
- Villages gain wheat and one mill at level 2, become larger stone-walled cities at level 3, and gain more fields and a second mill at level 4. Seeded homes vary in placement and height. Fortification remains an optional purchase: wooden below level 3, stone thereafter.
- A level-3 city can build a **Port for 6 Thors** instead of producing that turn: **+1 income**, **20% local shipyard discount**, working piers and tiny boats. Other shipyards keep their own prices; Shipwright stacks with the local discount.
- Active ports of the same fleet connect along shortest legal sea lanes, drawn as faint dashed lines on known water. Continuous lane travel accelerates to **at least +2 tiles or +20%**, whichever is greater, in clear water. Terrain/enemy penalties remain; leaving the lane ends the benefit. Navigation, previews and AI share one calculation, including saved partial movement.
- Every armed ship can fire at a coordinate supplied by allied radar within its own weapon range. Radar-only ships remain anonymous and their HP is hidden.

[0.18 validation, diagnostics and limits](docs/MILESTONE-0.18.md). Use **New game** for the new numeric balance. Old voyages default to Captain; new voyages default to Admiral.

## Changes in 0.17 — 1 October 2026

- Commit real damage, counterattack and veterancy after the last shell lands. A destroyed Mothership fractures and submerges before its remaining fleet sinks; other wrecks also sink gradually. Saving waits for the resolved command.
- Give forests and mountain chains continuous world-space placement, three tree silhouettes and independent ground anchors. Ships, trees, mountains and village animation share one native depth order. Smooth connected island shores more strongly.
- Reduce transient gulls and dock flocks. Replace overlapping cloud facets with single translucent connected-square silhouettes, drifting high above their shadows.
- Unfold both papyrus arc and thickness from its bottom center, with icons alone. Place HP/progression along the inner arc; remove ship and village health bars. Damage rises from the HP number, counterattack is yellow, and all repair feedback is green.
- Preview a continuous winding light-burgundy dashed route with an X at the destination, together with separate cannon, mortar and collection coverage. Selected mortars show their firing annulus. Diagonal/corner movement costs one tile in clear water; hostile proximity still costs two.
- Increase Cannon Tower range from 3 to 4. Add 20 unique rival captain names and 40 village names, persisted with the voyage. Opponent status announces only the acting captain; a centered **Your turn** marks the player's turn.
- Keep route/landscape caches and use direct commands for movement, avoiding the staging cost needed only for projectile impacts. Validate old saves and the actual Windows renderer.

Validation and measured performance: [0.17 milestone](docs/MILESTONE-0.17.md). Existing voyages retain their saved balance; use **New game** for range-4 Cannon Towers.

## Changes in 0.16 — 1 October 2026

- Rebuild the Mothership as one rounded terraced town over two rounded wooden keels; separate floor rotation from vertical height, sort buildings by depth and cull hidden walls. Cannon broadsides no longer fold the houses or sails.
- Replace the animated title close-up with a bundled static generated city-ship illustration. [Asset, exact prompt and tool](docs/MENU-ART-0.16.md).
- Cannon Tower: range 3, sight 2, optional radar 4 for 2 Thors; three ship kills earn veterancy. Ancient Mortar Towers also earn veterancy; their original sight/radar/range remain unchanged.
- Kolonel range 2; Galleon and Brig range 1. Balloon sight 6; Mothership anti-air range 2.
- Outposts fire automatically at the end of their owner's turn from village level 2, at the weakest visible ship within 2 tiles. Damage is 2/3/4/5 at levels 2/3/4/5; counterattack is one higher, also within 2. Manual repair suppresses that turn's automatic shot. Villages cannot construct towers; their pirate kills award the normal owner bounty.
- A wounded AI flagship retreats from observed enemy gun coverage before firing or building, and avoids routes with greater exposure. It cannot react to unseen enemies.
- Increase fish resource density and replace icons with swimming underwater schools: three fish per resource, fourteen at coral shoals/docks. Add exposed reefs inside semicircular quays, more gulls, sharp translucent cloud facets and diverging linear wakes.
- Retain static terrain/route caches; animate fish in the bounded, culled ambience layer at 30 Hz. Balance snapshots in existing voyages remain readable; start a new voyage to use the new numerical balance.

Validation and performance measurements: [0.16 milestone](docs/MILESTONE-0.16.md).

## Changes in 0.15 — 1 October 2026

- Diagnose real cursor/ship lag; cache the landscape in a bounded texture with independently retained cells, draw live actions separately, reuse one route plan per observed state and serialize saves away from the drawing thread. See [debug evidence](docs/DEBUG-0.15.md).
- Select 1–4 independent rival fleets with unique colors. Sea area scales with rival count, with equal village/treasury counts per starting territory. Rivals attack each other as well as you.
- Rebalance construction and income: starting reserve 8, starting income 3, Fishing Schooner 9, Galleon 12, Granado 18, Kolonel 20; combat fleet upkeep discourages unlimited expansion.
- Collect resources from the same or a directly adjacent tile. Build a Cannon Tower from level 2. Kolonel and Galleon cannon range is 2.
- Generate shared flowing seams with less deformation, rounded connected coastlines, dense groves, larger mountain ridges and soft drifting clouds.
- Rebuild the Mothership and title close-up as an asymmetric terraced city, with a white gold-domed temple, irregular beige homes, market, crates, laundry and moving carts.
- Unfold a papyrus command arc, read an object's sage-style numerical chart with Info, and cancel selection with right-click. Hostile HP is red; income rises from its sources at turn start.
- Turn cannon hulls broadside before firing; mortars aim their barrels independently. Visual shell count remains independent of damage.
- New game replaces the sole voyage slot and deletes the previous voyage's backup. Recovery copies subsequently belong only to the new voyage.

Validation: clean build, ten complete AI games, v1 save compatibility and actual Windows graphical/input/menu checks. [Results and limits](docs/MILESTONE-0.15.md).

## Changes in 0.14.1 — 30 September 2026

- Split navigation, combat, production and turn handling behind the existing battle command facade. Separate scene composition, input, commands and view refresh.
- Extract stable path search and command-scoped navigation policy. Enemy occupancy/threats are indexed once per query, preserving fog, friendly transit and equal-cost route order.
- Move mortar, balloon and treasury mechanics into validated typed balance settings with defaults compatible with 0.14 saves.
- Separate ship drawing from command animation; reuse rendering buffers and lay out the title menu on resize.
- Protect good save backups after a damaged primary is recovered or replaced by New game. Validate saved geometry and state before restoring it.
- Add a project-specific [development guide](AGENTS.md), [architecture contracts](docs/ARCHITECTURE.md), and [change history](CHANGELOG.md).

Verification: build succeeds with no warnings; 1,535 basic checks, 1,195,229 rules/navigation/map checks, 8 complete AI games, 20 save compatibility checks including two released v1 fixtures, and Godot gameplay/render/menu checks pass. On synthetic full-board searches, temporary managed allocations decreased by **95–98%**; idle draw preparation decreased by **27–59%**. These measurements cover specific operations, not whole-game FPS. [Measurements and limits](docs/MILESTONE-0.14.1.md).

## Changes in 0.14 — 30 September 2026

- Animated title scene with a living floating town, waves, gulls, color selection and recoverable Save/Continue.
- A larger irregular hexagon with smoother varied cells, opposed starts and equal villages/treasuries per territory.
- Weighted discoveries, 2-HP automatic ship repair, enemy-only mortar splash, level-4 movement, vulnerable Balloons and expanded village shipyards.
- New island details, docks, clouds, shadows, stronger hull motion and impact effects.
- Save/load and geometry logic stay in engine-independent Core; scenery and fleet anchors are cached, and effect populations are bounded.

The requested menu signature intentionally remains `GitHub: Beygelman  @Ancient_Naval_v0.13 30.09.2026`; the playable checkpoint is **0.19**. [Title asset and generation prompt](docs/TITLE-ASSET.md).

## Controls

- Select a ship and click its destination. Movement uses a translucent outer contour with a short inward glow. Hover previews a winding dashed treasure route, its destination cross and separate cannon/collection/mortar contours from the future position. Corner crossings cost one tile in clear water.
- Targets the selected ship can attack receive a coral outline. A selected mortar additionally shows its firing annulus; ordinary cannon coverage appears only in the movement forecast. Radar uses a green contour; unseen contacts remain anonymous, even during attacks.
- Glowing underwater fish schools are within your fleet's collection reach. **No ship selection is required.** Click the resource, then its purchase sector. Only the school is highlighted; the land/water mesh stays unchanged.
- The papyrus arc unfolds upward from its bottom center and includes available and locked command icons. Hover an icon for its action/cost; **Info** explains the object and current numbers. **Right-click** closes the arc and clears selection.
- Click a village to inspect it. Reduce its defenses to 0 HP, hold an eligible combat ship adjacent until its next turn, then click the flag to capture. Capture spends that ship's actions. Towns cannot be destroyed.
- Select a Balloon by its elevated model. Move it, then use **Drop bomb**. It recharges every three owner turns; Balloons no longer expire.
- Drag to pan; wheel/pinch to zoom; Escape for the menu. Creative mode removes player construction, collection, village fortification and port costs, preserving unlocks. Radar/mortar remain paid. God’s eye reveals the entire map to the player; victory does this automatically.

## Balance

| Unit | Shipyard level | Price | Base HP | Base damage | Movement |
|---|---:|---:|---:|---:|---:|
| Mothership | — | — | 15 | 3 | 1 |
| Brig | 1 | 5 | 5 | 3, range 1 | 4 |
| Fishing Schooner | 1 | 4 | 5 | — | 2 |
| Fishing Dock | 2 | 8 | 10 | — | 0 |
| Galleon | 3 | 10 | 10 | 5, range 1 | 4 |
| Kolonel | 4 | 18 | 15 | 4, range 2, twice per turn | 3 |
| Granado | 5 | 16 | 5 | 8 | 2 |
| Cannon Tower | 2 | 6 | 10 | 3, range 4 | 0 |

Galleon retains internal identifier `Invader`. The Mothership is a broad catamaran with two hulls and houses of different sizes. Ships, towns, flags and effects use procedural art. The generated transparent title image and static harbor illustration are bundled in `assets/ui`; no external downloads are needed.

Motherships produce once per turn and cannot move after production. The first ordinary Mothership production is immediately ready; later ships and village-produced ships become ready next turn. The normal fleet cap is 12 combat naval ships, excluding Fishing Schooners, structures and Balloons.

## Economy and progression

Each fleet starts with 8 Thors, a Mothership, Brig and Fishing Schooner. Starting income is 3: 2 from the Mothership and 1 from the Fishing Schooner. Mothership levels no longer raise income. Villages supply 1/1/2/2/3 at levels 1–5. The first two combat naval ships are free of upkeep; each additional pair (rounded up) costs 1 Thor per owner turn. Upkeep is deducted from gross income, never below zero. Fishing ships, structures and Balloons pay no fleet upkeep. At turn start, source-specific coin labels show gross gains and the upkeep deduction at the Mothership.

Ordinary fish cost 2 Thors and grant one resource. Motherships and Fishing Schooners collect on their own or a directly adjacent tile; the fisher transfers resources regardless of distance from its Mothership. From level 2, shoals can become docks for 8 Thors: two resources once, then one income per turn. Ordinary resources do not regenerate. Destroying a Fishing Dock restores its original shoal, allowing another dock to be built there.

| New level | Required resources | Base maximum HP | Choose one upgrade |
|---|---:|---:|---|
| 2 | 2 | 20 | +1 movement OR one additional Fishing Schooner |
| 3 | 3 | 25 | +2 vision and +2 radar range OR +5 maximum HP and passive repair of 2 HP each turn |
| 4 | 4 | 30 | Launch a Balloon OR +1 shot per turn |
| 5 | 5 | 35 | 25% cheaper ships OR +3 shot damage and +2 counterattack damage |

Levels preserve damage and add 5 current/maximum HP. Level 4 also grants +1 movement automatically, independently of the chosen upgrade and stacking with level-2 Mobility. Restoration adds another 5 HP and repairs at the owner's turn start, capped at maximum health. Vision also boosts radar installed later. Shipwright applies to Mothership and village production; prices round down, minimum one Thor outside creative mode. Upgrade choices are defined per level in `data/balance.json`.

Radar costs 2 Thors on Mothership/Kolonel/Cannon Tower. Cannon Towers see 2 optically and detect anonymous radar contacts within 4 after purchase. Mothership mortar requires level 5 plus radar, costs 10 and shares normal attacks. Granado and installed Mothership mortars deal 8 base damage at ranges 4–5, plus 2 against villages before fortification resistance. Mortar shells also deal 2 splash damage to adjacent enemies; allied ships and villages are safe from mortar splash. Fortified villages reduce splash by 25%. Motherships use ordinary cannons at ranges 1–3 and mortars at ranges 4–5, with separate damage values. Ships with mortars must fire before moving. Granado has built-in radar/mortar and cannot counterattack. Mothership base vision is 2 and installed radar 5; Kolonel gun range is 2; Galleon and Brig gun range is 1.

## Balloon

Permanent, with **1 HP**, movement 4 (the Galleon's base movement), vision 6. Flight ignores terrain and may share a tile with another unit. After moving, the Balloon can bomb its current cell: 6 damage directly below and 2 to adjacent cells, including friendly ships and villages. The bomb recharges at the start of three subsequent owner turns. Fortified villages retain their resistance. Ancient Balloons have the same rules and a distinct gold/bronze design. Bombs can damage Motherships. Only a Mothership can shoot a Balloon down, using cannons at up to 2 tiles; radar/installed mortars do not extend this anti-air range. Click the elevated Balloon model to target it separately from a ship below.

## Sea discoveries and pirates

Three ancient treasuries per fleet are scattered across open water, equally divided among starting territories. Hold a combat ship on one until its next turn, then use **Plunder treasury** before other actions. The ship spends its actions and the treasury disappears. The whirlpool has a **12%** chance; each of the other four outcomes has a **22%** chance:

- Ancient Mortar Tower: owned by the looter, 10 HP, damage 5, range/radar 5, sight 3. Stationary; no repair or production. Placed in the nearest free water; ready next turn.
- 5 Thors.
- Ancient Balloon, ready next turn.
- 2 resources toward Mothership progression.
- Whirlpool: sinks the looting ship and permanently blocks nine water cells (3×3 on rectangular boards; nine local cells on the organic mesh). Other units already inside may leave, but no unit may enter a forbidden cell. The rotating funnel marks the hazard.

There is one Pirate Schooner per starting fleet: 2–5 depending on rival count, one in each territory. Each patrols within five tiles of its home and attacks visible opposing ships. Initial balance: 7 HP, damage 4, movement 5, sight 5, range 2. The killer receives 2 Thors and 1 Mothership resource. Pirates act after the surviving rival fleets and before the player, and do not capture towns or plunder treasuries.

## Coastal villages

Small towns occupy coastal land, at most one per island, with equal nonempty counts in all starting territories. Neutral villages start at level 1 / 5 HP. Neutral and enemy towns can be attacked. A town persists at 0 HP and can be captured after an adjacent Mothership, Brig, Galleon, Kolonel or Granado remains alongside until its next turn. Fishing Schooners, docks and Balloons cannot capture. Capture restores full HP and consumes all actions of one waiting ship. While defeated, a town cannot grow, produce or earn income.

A captured village supplies 1/1/2/2/3 Thors at levels 1/2/3/4/5 and automatically gains a level every two completed owner turns, adding 5 HP. Maximum: level 5 / 25 HP. Growth never offers upgrade choices. Its shipyard unlocks Fishing Schooners at level 1, Brigs at level 2, Galleons at level 3, Kolonels at level 4 and Granados at level 5. Production is once per turn into adjacent free water.

**Fortification** costs 8 Thors by default (`VillageFortificationPrice` in balance settings). Incoming damage is reduced by exactly 25%; from level 2, a surviving outpost counterattacks for level + 1 damage within 2 tiles. At the end of its owner turn, it fires once at the lowest-HP visible hostile surface ship within 2; active damage equals its level (2/3/4/5). Level-one outposts cannot shoot or counter. Active repair takes precedence over automatic fire. Villages cannot build Cannon Towers. Sight increases from 3 to 5 immediately. Fractional HP are supported. A town must reach 0 HP before capture, irrespective of its owner.

## Navigation and combat

Neighbors share an edge or corner of the actual mesh; **diagonals cost the same as edge-adjacent steps**. Existing coastal/narrow-channel multipliers still apply. Ships pass through friendly units but finish on a free tile. Enemy tiles block passage; entering a tile adjacent to an enemy armed sea unit costs at least 2 movement points, including diagonal travel. Radar contacts also block their occupied tile. Balloons may stop above units. Land, boundaries and heavy-ship passage restrictions remain.

Brig moves after firing. Galleon/Mothership retain movement/attack profiles; Kolonel has two attacks but cannot move after firing. Manual repair restores up to 5 HP and replaces actions. At the end of an owner turn, an eligible ship that did not attack or repair automatically receives **2 HP**. Living villages retain **5 HP**, matching their active repair. Counterattacks do not prevent this; movement alone does not prevent it. Active village repair consumes production. Zero-HP villages stay defeated; ancient towers and Balloons never repair. Damage scales with hull condition; below 25% HP movement falls by one. Three ship kills grant veterancy, full repair and +25% maximum HP/base damage; Cannon and Ancient Mortar Towers also earn this promotion; Motherships do not gain veterancy.

Every generated map has **six straight sides of different lengths, each containing 24–30 actual boundary tiles**. Six fitted patches, nonuniform spacing, local row reconnections and constrained deformation create flowing bands of irregular quadrilaterals, with triangles and pentagons. Roughly 91% are quads; projected polygon corners stay at least 32°. Curved shared seams, picking and gameplay use the same topology. See [generator design](docs/MAP-GENERATION.md).

Ships accelerate and brake over an entire route, with heavier hulls moving more slowly, a slight backward lean underway and banking on turns. Ordinary cannonballs now have a longer flight and more visible weight. Idle rocking, recoil, gun/impact smoke and wakes are procedural. Visual salvos contain 1/3/3/2 cannonballs for Brig/Galleon/Kolonel/Mothership; mortars launch one heavy shell. Counts do not change damage. Quiet sea waves, coastal surf, more gulls (mostly around fishing spots), rare dolphins, windmills and flags animate separately from the static board. Islands are green with white beaches. Two fleets start at opposite ends; larger matches use evenly spaced maritime anchors on a random orientation. Islands have lobed coastlines, variable-width white beaches, grass details, dense tree clusters and isolated or linked mountain peaks. Rare soft clouds drift across the sea and cast shadows.

Ship and cannonball shadows, stronger recoil and impact rocking, local water mist and wooden splinters accompany combat. Floating damage is red, counterattacks yellow and health/healing green. The smaller Mothership has a rounded broad deck with houses; Fishing Docks form open semicircular harbors with houses, groynes and permanent circling gulls.

The Windows preview includes `Ancient Naval.exe` for normal play and `Preview map.cmd` to inspect a fully revealed map. New game regenerates the layout and asks for Blue, Green, Yellow, Purple or White. Color changes ship details, decorative markings and captured village flags. Rival colors are unique and exclude yours.

## Verification and remaining work

```powershell
dotnet build Dev_ancient_naval.csproj
dotnet run --project tests/CoreChecks/CoreChecks.csproj --configuration Release
& 'PATH/TO/Godot_console.exe' --headless --path . --quit-after 10000 -- --smoke-test
& 'PATH/TO/Godot_console.exe' --path . --quit-after 10000 -- --battle-test
& 'PATH/TO/Godot_console.exe' --path . -- --effects-test
& 'PATH/TO/Godot_console.exe' --path . -- --sea-test
& 'PATH/TO/Godot_console.exe' --path . -- --menu-test --save-file=C:/temp/ancient-naval-test.json
```

Every suite must print **PASS**. Tests cover seeded maps, geometry/picking, upgrade alternatives, transit/diagonals, selection-free collection, Balloon recharge/blast, treasury outcomes/pirate patrols, village capture/growth/production/fortification, hidden information and full AI matches. `--capture=C:/absolute/path/naval.png` saves screenshots during graphical checks.

Autosave records each completed player action and AI action, and saves again when returning to the title or exiting. Continue restores the exact mesh, ships, upgrades, economy, fog, village/treasury waits, random-event state and camera. A complete previous checkpoint of the **same voyage** is retained as `.bak`. On Windows the default slot is `%APPDATA%/Godot/app_userdata/Ancient Naval/last_battle.json`. Debug checks and map previews never overwrite it. Starting a new game atomically replaces the slot and removes the old voyage backup. No older voyage can be continued.

Android export/device testing remain future work. See the [beta roadmap](docs/ROADMAP-TO-BETA.md) and [current milestone](docs/MILESTONE-0.17.md). Earlier milestones describe historical rules.

## Isolated v020.7 finalization candidate — 4 October 2026

The newer v020.8 source above is preserved. A separate audited v020.7 snapshot
is on `codex/v0207-finalization`, based on `b03c5bd`; no main reset/merge occurred.
**Final native performance approval is pending. These are candidates.**

- [Clean player candidate](releases/v020.7/finalization-candidate/Ancient_Naval_v020.7_Windows_PLAYER_CANDIDATE.zip).
- [Source candidate](releases/v020.7/finalization-candidate/Ancient_Naval_v020.7_Source_CANDIDATE.zip).
- [SHA256 manifest](releases/v020.7/finalization-candidate/SHA256-v020.7-candidate.txt).
- [Audit/test report](docs/V0207_FINALIZATION_REPORT.md) and [exact artifact validation](releases/v020.7/finalization-candidate/FINAL-ARTIFACT-VALIDATION.json).

Original releases and their manifests remain byte-for-byte unchanged. Gameplay,
Core and save contracts were not altered by this audit. The guide was upgraded
to the supplied revision 4.0; prior primary documentation copies remain in
`docs/history/v0207-finalization-primary/`. No v020.8 feature was backported.

## Final verified v020.7 checkpoint — 5 October 2026

The isolated v020.7 checkpoint now passes the final Debug build, Core/historical
save checks and both serial native frame gates with unchanged thresholds.
Pangaea/wide-view pan p95 is 19.776 ms; Oceans/live-fog pan p95 is 17.247 ms.
Earlier candidate results remain historical evidence; this section supersedes
their pending status for the final delivery only. Primary v020.8 sources remain
preserved and the remote main/PR #6 have not been changed.

- [Clean Windows player](releases/v020.7/finalized/Ancient_Naval_v020.7_Windows_PLAYER_CLEAN.zip).
- [Finalized source](releases/v020.7/finalized/Ancient_Naval_v020.7_Source_FINALIZED.zip).
- [Archive SHA256](releases/v020.7/finalized/SHA256-v020.7-finalized.txt).
- [Actual artifact checks](releases/v020.7/finalized/FINAL-ARTIFACT-VALIDATION.json)
  and [technical report](docs/V0207_FINALIZATION_REPORT.md).

The source snapshot is ce6d1e50d82c57b4971d3c28da119607b148e1d1 on
codex/v0207-finalization, based on b03c5bd. Local tag v020.7-finalized records the
later artifact-delivery checkpoint. All historical archives/latest metadata and
the primary HEAD/index remain unchanged. The full Windows runtime is retained;
51 internal diagnostics/preview/PDB items from the original player are excluded.
