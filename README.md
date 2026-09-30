# Ancient Naval — 0.14 — Living Seas

A turn-based naval tactics game built with **Godot 4.7.2 .NET**, C# and .NET 8. Open `project.godot`, stop the previous run with **F8**, then press **F5**. The title screen offers **New game**, **Continue** and **Exit game**. New game opens a five-color fleet selector.

Destroy Captain Anat's Mothership while exploring, collecting resources and capturing coastal villages. The entire interface is in English. Core gameplay remains independent of Godot; rendering and input live in Presentation.

## Changes in 0.14 — 30 September 2026

- Animated title scene with a living floating town, waves, gulls, color selection and recoverable Save/Continue.
- A larger irregular hexagon with smoother varied cells, opposed starts and equal villages/treasuries per territory.
- Weighted discoveries, 2-HP automatic ship repair, enemy-only mortar splash, level-4 movement, vulnerable Balloons and expanded village shipyards.
- New island details, docks, clouds, shadows, stronger hull motion and impact effects.
- Save/load and geometry logic stay in engine-independent Core; scenery and fleet anchors are cached, and effect populations are bounded.

The requested menu signature intentionally remains `GitHub: Beygelman  @Ancient_Naval_v0.13 30.09.2026`; the playable release is **0.14**. [Title asset and generation prompt](docs/TITLE-ASSET.md).

## Controls

- Select a ship and click its destination. Movement uses a translucent outer contour with a short inward glow. Hovering previews a path.
- Only targets the selected ship can attack receive a coral outline. There is no filled attack area. Radar uses a green contour; unseen contacts remain anonymous, even during attacks.
- Glowing fish/shoal icons are within your fleet's collection reach. **No ship selection is required.** Click the resource, then its purchase sector. Only the icon is highlighted.
- Ship menus provide repair, radar, mortar and production where applicable.
- Click a village to inspect it. Reduce its defenses to 0 HP, hold an eligible combat ship adjacent until its next turn, then click the flag to capture. Capture spends that ship's actions. Towns cannot be destroyed.
- Select a Balloon by its elevated model. Move it, then use **Drop bomb**. It recharges every three owner turns; Balloons no longer expire.
- Drag to pan; wheel/pinch to zoom; Escape for the menu. Creative mode removes player construction, collection and village fortification costs, preserving unlocks. Radar/mortar remain paid.

## Balance

| Unit | Shipyard level | Price | Base HP | Base damage | Movement |
|---|---:|---:|---:|---:|---:|
| Mothership | — | — | 20 | 3 | 1 |
| Brig | 1 | 2 | 5 | 3 | 4 |
| Fishing Schooner | 1 | 3 | 5 | — | 2 |
| Fishing Dock | 2 | 4 | 10 | — | 0 |
| Galleon | 3 | 4 | 10 | 5 | 4 |
| Kolonel | 4 | 8 | 15 | 4, twice per turn | 3 |
| Granado | 5 | 7 | 5 | 8 | 2 |

Galleon retains internal identifier `Invader`. The Mothership is a broad catamaran with two hulls and houses of different sizes. Ships, towns, flags and effects use procedural art. The generated transparent title image is bundled in `assets/ui`; no external downloads are needed.

Motherships produce once per turn and cannot move after production. The first ordinary Mothership production is immediately ready; later ships and village-produced ships become ready next turn. The normal fleet cap is 12 combat naval ships, excluding Fishing Schooners, structures and Balloons.

## Economy and progression

Each fleet starts with 5 Thors, Mothership, Brig and Fishing Schooner. Starting income is 4: 2 from the Mothership and 2 from the Fishing Schooner, credited at the beginning of the owner's turn. Existing base Mothership income increases by 2 per level.

Ordinary fish cost 2 Thors and grant one resource. Motherships and Fishing Schooners collect within range 2; the fisher transfers resources regardless of distance from its Mothership. From level 2, shoals can become docks for 4 Thors: two resources once, then one income per turn. Ordinary resources do not regenerate. Destroying a Fishing Dock restores its original shoal, allowing another dock to be built there.

| New level | Required resources | Base maximum HP | Choose one upgrade |
|---|---:|---:|---|
| 2 | 2 | 25 | +1 movement OR one additional Fishing Schooner |
| 3 | 3 | 30 | +2 vision and +2 radar range OR +5 maximum HP and passive repair of 2 HP each turn |
| 4 | 4 | 35 | Launch a Balloon OR +1 shot per turn |
| 5 | 5 | 40 | 25% cheaper ships OR +3 shot damage and +2 counterattack damage |

Levels preserve damage and add 5 current/maximum HP. Level 4 also grants +1 movement automatically, independently of the chosen upgrade and stacking with level-2 Mobility. Restoration adds another 5 HP and repairs at the owner's turn start, capped at maximum health. Vision also boosts radar installed later. Shipwright applies to Mothership and village production; prices round down, minimum one Thor outside creative mode. Upgrade choices are defined per level in `data/balance.json`.

Radar costs 2 Thors on Mothership/Kolonel. Mothership mortar requires level 5 plus radar, costs 10 and shares normal attacks. Granado and installed Mothership mortars deal 8 base damage at ranges 4–5, plus 2 against villages before fortification resistance. Mortar shells also deal 2 splash damage to adjacent enemies; allied ships and villages are safe from mortar splash. Fortified villages reduce splash by 25%. Motherships use ordinary cannons at ranges 1–3 and mortars at ranges 4–5, with separate damage values. Ships with mortars must fire before moving. Granado has built-in radar/mortar and cannot counterattack. Mothership base vision is 2 and installed radar 5; Galleon gun range is 2.

## Balloon

Permanent, with **1 HP**, movement 4 (the Galleon's base movement), vision 8. Flight ignores terrain and may share a tile with another unit. After moving, the Balloon can bomb its current cell: 6 damage directly below and 2 to adjacent cells, including friendly ships and villages. The bomb recharges at the start of three subsequent owner turns. Fortified villages retain their resistance. Ancient Balloons have the same rules and a distinct gold/bronze design. Bombs can damage Motherships. Only a Mothership can shoot a Balloon down, using cannons at up to 3 tiles; radar/installed mortars do not extend this anti-air range. Click the elevated Balloon model to target it separately from a ship below.

## Sea discoveries and pirates

Six ancient treasuries are scattered across open water, three in each starting territory. Hold a combat ship on one until its next turn, then use **Plunder treasury** before other actions. The ship spends its actions and the treasury disappears. The whirlpool has a **12%** chance; each of the other four outcomes has a **22%** chance:

- Ancient Mortar Tower: owned by the looter, 10 HP, damage 5, range/radar 5, sight 3. Stationary; no repair or production. Placed in the nearest free water; ready next turn.
- 5 Thors.
- Ancient Balloon, ready next turn.
- 2 resources toward Mothership progression.
- Whirlpool: sinks the looting ship and permanently blocks nine water cells (3×3 on rectangular boards; nine local cells on the organic mesh). Other units already inside may leave, but no unit may enter a forbidden cell. The rotating funnel marks the hazard.

There is one Pirate Schooner per player faction: two in the current 1v1 game, one in each territory. Each patrols within five tiles of its home and attacks visible opposing ships. Initial balance: 7 HP, damage 4, movement 5, sight 5, range 2. The killer receives 2 Thors and 1 Mothership resource. Pirates act after Captain Anat and before the player, and do not capture towns or plunder treasuries.

## Coastal villages

Small towns occupy coastal land, at most one per island, with equal counts in the two starting territories. Neutral villages start at level 1 / 5 HP. Neutral and enemy towns can be attacked. A town persists at 0 HP and can be captured after an adjacent Mothership, Brig, Galleon, Kolonel or Granado remains alongside until its next turn. Fishing Schooners, docks and Balloons cannot capture. Capture restores full HP and consumes all actions of one waiting ship. While defeated, a town cannot grow, produce or earn income.

A captured village supplies one Thor per level and automatically gains a level every two completed owner turns, adding 5 HP. Maximum: level 5 / 25 HP. Growth never offers upgrade choices. Its shipyard unlocks Fishing Schooners at level 1, Brigs at level 2, Galleons at level 3, Kolonels at level 4 and Granados at level 5. Production is once per turn into adjacent free water.

**Fortification** costs 5 Thors by default (`VillageFortificationPrice` in balance settings). Incoming damage is reduced by exactly 25%; a surviving village counterattacks for 3 within range 3. Sight increases from 3 to 5 immediately. Fractional HP are supported. A town must reach 0 HP before capture, irrespective of its owner.

## Navigation and combat

Neighbors share an edge or corner of the actual mesh; **diagonals cost the same as edge-adjacent steps**. Existing coastal/narrow-channel multipliers still apply. Ships pass through friendly units but finish on a free tile. Enemy tiles block passage; entering a tile adjacent to an enemy armed sea unit costs at least 2 movement points, including diagonal travel. Radar contacts also block their occupied tile. Balloons may stop above units. Land, boundaries and heavy-ship passage restrictions remain.

Brig moves after firing. Galleon/Mothership retain movement/attack profiles; Kolonel has two attacks but cannot move after firing. Manual repair restores up to 5 HP and replaces actions. At the end of an owner turn, an eligible ship that did not attack or repair automatically receives **2 HP**. Living villages retain **5 HP**, matching their active repair. Counterattacks do not prevent this; movement alone does not prevent it. Active village repair consumes production. Zero-HP villages stay defeated; ancient towers and Balloons never repair. Damage scales with hull condition; below 25% HP movement falls by one. Three ship kills grant veterancy, full repair and +25% maximum HP/base damage; Motherships do not gain veterancy.

Every generated map has **six straight sides of different lengths, each containing 24–30 actual boundary tiles**. Six fitted patches, nonuniform spacing, local row reconnections and constrained deformation create flowing bands of irregular quadrilaterals, with triangles and pentagons. Roughly 80% are quads; projected polygon corners stay at least 32°. Curved shared seams, picking and gameplay use the same topology. See [generator design](docs/MAP-GENERATION.md).

Ships accelerate and brake over an entire route, with heavier hulls moving more slowly, a slight backward lean underway and banking on turns. Ordinary cannonballs now have a longer flight and more visible weight. Idle rocking, recoil, gun/impact smoke and wakes are procedural. Visual salvos contain 1/3/3/2 cannonballs for Brig/Galleon/Kolonel/Mothership; mortars launch one heavy shell. Counts do not change damage. Quiet sea waves, coastal surf, more gulls (mostly around fishing spots), rare dolphins, windmills and flags animate separately from the static board. Islands are green with white beaches. Player fleets start at opposing ends along a randomly oriented common axis. Islands have lobed coastlines, variable-width white beaches, grass details, sparse tree clusters and isolated or linked mountain peaks. Rare soft clouds drift across the sea and cast shadows.

Ship and cannonball shadows, stronger recoil and impact rocking, local water mist and wooden splinters accompany combat. Floating damage is red, counterattacks yellow and health/healing green. The smaller Mothership has a rounded broad deck with houses; Fishing Docks form open semicircular harbors with houses, groynes and permanent circling gulls.

The Windows preview includes `Ancient Naval.exe` for normal play and `Preview map.cmd` to inspect a fully revealed map. New game regenerates the layout and asks for Blue, Green, Yellow, Purple or White. Color changes ship details, roofs and captured village flags.

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

Autosave records each completed player action and AI action, and saves again when returning to the title or exiting. Continue restores the exact mesh, ships, upgrades, economy, fog, village/treasury waits, random-event state and camera. A complete previous save is retained as `.bak`. On Windows the default slot is `%APPDATA%/Godot/app_userdata/Ancient Naval/last_battle.json`. Debug checks and map previews never overwrite it. Starting a new game replaces the active slot.

Android export/device testing remain future work. See the [beta roadmap](docs/ROADMAP-TO-BETA.md) and [current milestone](docs/MILESTONE-0.14.md). Earlier milestones describe historical rules.
