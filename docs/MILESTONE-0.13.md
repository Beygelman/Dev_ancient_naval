# Ancient Naval 0.13 — Treasures and Pirates

This checkpoint implements the next gameplay and visual update on top of the organic pentagonal grid from 0.12. The grid topology is preserved.

## Changed rules

- Mothership: base movement 1, sight 2, radar 5. Galleon range 2; Brig movement 4.
- Allied sea units permit transit. Enemies block their own tile; adjacent armed enemy units impose a two-point minimum movement cost. A diagonal beside an enemy is slowed, while land still prevents corner cutting.
- Mortar ships fire before moving. Moving disables their attacks for the rest of that turn. Mortar hits on villages receive +2 raw damage before resistance.
- A defeated village remains at 0 HP. Capture requires a combat ship to hold alongside until its next turn. Moving away resets that crew's wait; another crew that remained can still capture. Capturing consumes all actions of one waiting ship.
- Both active and automatic repair use the balance setting of 5 HP, capped at maximum. Automatic repair occurs at the end of an eligible unit's own turn if it did not attack or repair. Counterattacks and movement alone do not prevent it. Manual repair cannot stack with auto repair. Towers, Balloons and defeated towns never regenerate; the existing level-three Restoration bonus of +2 at turn start remains separate.
- Village fortification extends sight from 3 to 5. Villages retain 25% resistance and damage-3 counterattacks within range 3. Their yards build only Brigs and Fishing Schooners, correcting the previous wider ship menu.
- Persistent Balloons: movement 4, sight 8, bomb after movement for 6 direct and 2 adjacent damage, including friendly fire. Recharges after three subsequent owner-turn starts. Ancient variant uses different art with the same rules.
- Treasuries: hold a combat ship on the tile until next turn, then plunder. Five equal outcomes: stationary ancient mortar tower; 5 Thors; ancient Balloon; 2 flagship resources; fatal nine-cell whirlpool. Loot is atomic and cannot be rerolled if a tower berth is temporarily unavailable. Plundering spends the crew's actions; new units are ready next turn.
- Ancient tower: damage/range/radar 5, sight 3, no movement, repair or construction. The unspecified HP value is provisionally 10.
- Neutral Pirate Schooner: provisional HP 7, damage 4, movement/sight 5, range 2. Patrol territory radius 5; hunts visible ships. A separate pirate turn follows the opponent. Destroying it awards 2 Thors and 1 Mothership resource, including kills by counterattack or bomb.
- Generated village count is divisible by the two player factions. The fleets choose different random starting sides among all five pentagon sides.

## Presentation and implementation

- Mothership twin hull now carries houses of different sizes and a flag.
- Green islands, white beaches, animated treasury chests/tower/ancient Balloon silhouettes and rotating whirlpool funnel.
- Normal gunshots fly more slowly on ballistic arcs; salvo counts and damage are unchanged. Heavier route easing, backward lean and turn banking accompany existing recoil, smoke and wakes.
- More bounded gull flocks, mostly circling fishing locations. Existing surf, subtle sea waves, rare dolphins, windmills and owner-colored village flags remain.
- English repair, capture wait, treasury and bomb cooldown controls; pirate attack previews respect the third faction.
- Rules remain in engine-independent Core. Rendering stays in separate static-map, fleet and ambience layers. Obsolete Balloon lifetime state was removed. Event state, village repair and new visual helpers have separate files.

## Verification

Core scenarios cover actual seeded meshes and eight complete AI games, with additional capture-wait, equal repair, mortar order, enemy passage, treasury outcome, pirate reward/patrol and tower rules. Godot suites exercise map picking/input, village capture and repair buttons, visible salvos, weighted movement, bounded wildlife and all five treasury results through the real loot sector. Graphical checks capture three organic seeds and each new discovery.

Source and self-contained Windows x64 checkpoints are exported separately from 0.12. Save/Continue and Android device verification remain future work; these were not added in this update. Historical milestone documents retain the rules of their respective versions. See README for current rules and run commands.
