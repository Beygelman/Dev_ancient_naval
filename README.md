# Ancient Naval — 0.17 — Tides and Parchment

A turn-based naval tactics game built with **Godot 4.7.2 .NET**, C# and .NET 8. Open `project.godot`, stop the previous run with **F8**, then press **F5**. The title screen offers **New game**, **Continue** and **Exit game**. New game opens a five-color fleet selector and a choice of 1–4 rival fleets.

Defeat the rival Motherships while exploring, collecting resources and capturing coastal villages. The entire interface is in English. Core gameplay remains independent of Godot; rendering and input live in Presentation.

## Download and local release copies

The working project, sources, assets, balance, tests and this README are edited
directly in this Git repository. Delivered files are also copied to `releases/`
inside the same project; the external `outputs` folder is an additional mirror.

- [Windows 0.17 ZIP](releases/0.17/Ancient_Naval_0.17_Windows.zip): extract the entire folder and run `Ancient Naval.exe`.
- [Source 0.17 ZIP](releases/0.17/Ancient_Naval_0.17_Source.zip), [Russian release notes](releases/0.17/Ancient_Naval_0.17_Notes_RU.md), [SHA256 checksums](releases/SHA256-0.17.txt).
- A complete local launch copy is at `releases/0.17/playable/Ancient Naval.exe`, alongside its PCK and .NET runtime files.

The raw executable exceeds GitHub's 100 MiB limit, so only that unpacked launch
folder is excluded from Git; the complete Windows ZIP, sources, screenshots and
notes remain visible to Git. [GitHub size limits](https://docs.github.com/en/repositories/working-with-files/managing-large-files/about-large-files-on-github).
Godot ignores the release directory so copied exports never re-enter game assets.
See [release-copy workflow](releases/README.md). Files reach GitHub after the
repository changes are committed and pushed; copying files alone is not a push.

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

The requested menu signature intentionally remains `GitHub: Beygelman  @Ancient_Naval_v0.13 30.09.2026`; the playable checkpoint is **0.17**. [Title asset and generation prompt](docs/TITLE-ASSET.md).

## Controls

- Select a ship and click its destination. Movement uses a translucent outer contour with a short inward glow. Hover previews a winding dashed treasure route, its destination cross and separate cannon/collection/mortar contours from the future position. Corner crossings cost one tile in clear water.
- Targets the selected ship can attack receive a coral outline. A selected mortar additionally shows its firing annulus; ordinary cannon coverage appears only in the movement forecast. Radar uses a green contour; unseen contacts remain anonymous, even during attacks.
- Glowing underwater fish schools are within your fleet's collection reach. **No ship selection is required.** Click the resource, then its purchase sector. Only the school is highlighted; the land/water mesh stays unchanged.
- The papyrus arc unfolds upward from its bottom center and includes available and locked command icons. Hover an icon for its action/cost; **Info** explains the object and current numbers. **Right-click** closes the arc and clears selection.
- Click a village to inspect it. Reduce its defenses to 0 HP, hold an eligible combat ship adjacent until its next turn, then click the flag to capture. Capture spends that ship's actions. Towns cannot be destroyed.
- Select a Balloon by its elevated model. Move it, then use **Drop bomb**. It recharges every three owner turns; Balloons no longer expire.
- Drag to pan; wheel/pinch to zoom; Escape for the menu. Creative mode removes player construction, collection and village fortification costs, preserving unlocks. Radar/mortar remain paid.

## Balance

| Unit | Shipyard level | Price | Base HP | Base damage | Movement |
|---|---:|---:|---:|---:|---:|
| Mothership | — | — | 20 | 3 | 1 |
| Brig | 1 | 6 | 5 | 3, range 1 | 4 |
| Fishing Schooner | 1 | 9 | 5 | — | 2 |
| Fishing Dock | 2 | 10 | 10 | — | 0 |
| Galleon | 3 | 12 | 10 | 5, range 1 | 4 |
| Kolonel | 4 | 20 | 15 | 4, range 2, twice per turn | 3 |
| Granado | 5 | 18 | 5 | 8 | 2 |
| Cannon Tower | 2 | 8 | 10 | 3, range 4 | 0 |

Galleon retains internal identifier `Invader`. The Mothership is a broad catamaran with two hulls and houses of different sizes. Ships, towns, flags and effects use procedural art. The generated transparent title image and static harbor illustration are bundled in `assets/ui`; no external downloads are needed.

Motherships produce once per turn and cannot move after production. The first ordinary Mothership production is immediately ready; later ships and village-produced ships become ready next turn. The normal fleet cap is 12 combat naval ships, excluding Fishing Schooners, structures and Balloons.

## Economy and progression

Each fleet starts with 8 Thors, a Mothership, Brig and Fishing Schooner. Starting income is 3: 2 from the Mothership and 1 from the Fishing Schooner. Mothership levels no longer raise income. Villages supply 1/1/2/2/3 at levels 1–5. The first two combat naval ships are free of upkeep; each additional pair (rounded up) costs 1 Thor per owner turn. Upkeep is deducted from gross income, never below zero. Fishing ships, structures and Balloons pay no fleet upkeep. At turn start, source-specific coin labels show gross gains and the upkeep deduction at the Mothership.

Ordinary fish cost 2 Thors and grant one resource. Motherships and Fishing Schooners collect on their own or a directly adjacent tile; the fisher transfers resources regardless of distance from its Mothership. From level 2, shoals can become docks for 10 Thors: two resources once, then one income per turn. Ordinary resources do not regenerate. Destroying a Fishing Dock restores its original shoal, allowing another dock to be built there.

| New level | Required resources | Base maximum HP | Choose one upgrade |
|---|---:|---:|---|
| 2 | 2 | 25 | +1 movement OR one additional Fishing Schooner |
| 3 | 3 | 30 | +2 vision and +2 radar range OR +5 maximum HP and passive repair of 2 HP each turn |
| 4 | 4 | 35 | Launch a Balloon OR +1 shot per turn |
| 5 | 5 | 40 | 25% cheaper ships OR +3 shot damage and +2 counterattack damage |

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
