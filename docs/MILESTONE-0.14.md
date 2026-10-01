# 0.14 — Living Seas

Date: 30 September 2026. This is a separate playable checkpoint; 0.12 and 0.13 exports remain intact.

## Delivered

- Title menu: New game, Continue and Exit; Blue, Green, Yellow, Purple and White fleet selection. A close-up floating town with moving inhabitants, gulls, flags and a layered wave shader animates behind the generated transparent title.
- One recoverable autosave slot stores exact mesh geometry, balance settings, ships and all action/upgrade flags, villages, capture/loot waits, resources, treasuries, hazards, pirate homes, event random state, fog memory, economy, turn state and camera. Continue also resumes an interrupted opponent turn. File replacement happens only after a complete new save is flushed; a previous complete slot is kept as `.bak`.
- Hexagonal maps with six distinct straight side counts in 24–30. Nonuniform patch spacing, broader deformation and more topology variations retain a quadrilateral majority. Common seam geometry handles drawing and picking. Opposing fleets use opposite ends of a common random axis. Territories have equal village counts, three treasuries each and one pirate each.
- More irregular islands, variable beach widths, grass details, sparse tree clusters and mountain ridges/outcrops. Rare drifting clouds and shadows remain within observed water. Static scenery and bounds are cached.
- Treasure probabilities: four rewards at 22% each, whirlpool at 12%. A save cannot reroll an already determined outcome.
- Automatic ship repair is 2 HP. Manual ship repair and automatic/manual village repair remain 5. The Restoration upgrade retains its additional 2 HP at the start of the owner's turn.
- Mortars deal 2 splash damage to adjacent enemies; allied ships and villages are excluded. Fortified villages retain resistance. Direct mortar damage and the +2 village bonus remain distinct from splash.
- Level 4 automatically adds one Mothership movement tile. Its installed mortar fires at range 4–5; normal cannons still fire at 1–3 with their own damage.
- Balloons have 1 HP, persist indefinitely and retain their 6-direct/2-area bomb on a three-turn recharge. They can bomb Motherships. Only Mothership cannon fire can shoot them down, at up to three tiles. Elevated selection distinguishes a Balloon from a ship under it.
- Village production: Fishing Schooner at level 1, Brig at 2, Galleon at 3, Kolonel at 4, Granado at 5. Fishing Schooners do not count against the combat fleet cap. Sunk Fishing Docks restore a buildable shoal.
- Smaller rounded Mothership deck and town houses; semicircular Fishing Dock harbors with houses, groynes and permanent gulls. Stronger bobbing, recoil and hit rocking; projectile shadows, local water mist and splinters. Impact points remain on the target hull; visual pellet counts never multiply damage. Damage is red, counterattack yellow and health/healing green.

The title footer deliberately retains the user's exact `@Ancient_Naval_v0.13 30.09.2026` text. The project/export version is 0.14. The [title asset provenance and exact generation prompt](TITLE-ASSET.md) are included.

## Verification

- C# build: zero warnings and zero errors.
- 1,535 basic Core checks and 986,083 gameplay, map and AI assertions. One hundred seeded maps and eight complete AI matches.
- 474,809 Godot checks for geometry, polygon triangulation/picking, camera, mouse/touch and HUD.
- 99 graphical gameplay checks, including real input for elevated anti-air selection, village shipyards, upgrades and enemy-only mortar splash with exactly one shell.
- 41 graphical effects checks, including salvo counts, hull travel and wildlife. 108 sea-event runtime checks cover all five discoveries and their controls.
- 18 graphical menu/save checks and four separate-process Continue checks, including a corrupt primary save recovered from its backup.
- The exported executable launches successfully in headless mode, native D3D12 and OpenGL map preview. Export logs contain no errors.

Windows x64 is verified on this machine. Android/device testing and multiplayer remain future work; no Android or network-play claims are made. Existing historical milestone documents are retained as history; README describes current rules.
