# Alpha 0.11 — progression, coastal villages and usable curved tiles

## Gameplay

- Base hulls are Mothership 20 (+5 per level), Kolonel 15, Frigate 10, Garrison/Mortar/Fishing Ship 5 and Fishing Dock 10.
- All four Mothership level transitions offer the requested choices. Upgrade tiers are defined in balance data; effect application remains in Core. Vision applies to later-installed radar, restoration heals at turn start, and ship discounts also apply to village yards.
- Balloons are invulnerable, last three owner turns including launch turn, and drop one 8-damage bomb after moving. Unused bombs expire in a nine-tile 3×3 blast dealing 3 to ships/docks of either side. Both Motherships destroyed by the same expiry resolve as a draw.
- Transit ignores ship occupancy, while destinations remain exclusive. Orthogonal and diagonal base costs are identical; terrain multipliers remain.
- Resource collection/dock construction can resolve a nearby eligible collector without selection. The purchase confirmation sector remains; only resource icons glow.
- Coastal villages spawn on islands, wait one owner turn for capture, grow every two owned turns to level 5, earn their level in Thors and have 5 HP per level. Ship unlocks mirror the Mothership. Fortification provides 25% resistance and a 3-damage counterattack at range 3.
- AI uses the new upgrades, villages and Balloon commands, preserving observed-information restrictions.

## Presentation and geometry

- All interface, balance names and gameplay messages are English.
- A wide twin-hull catamaran replaces the old Mothership silhouette. Towns, flags, defenses, Balloon lifetime and bomb indicators use procedural art.
- Only valid attack targets are outlined. Movement uses shared outer seams with a short inward gradient.
- Irregular mesh retains predominantly quadrilateral tiles with triangles, pentagons and hexagons. Logical corners require minimum world/projected angles, edge length and area; curved corner tangents also require at least 24° in projection.
- Hit testing uses the rendered polygon and double-precision ray crossing, fixing a native float polygon test false positive on a distant corner.
- Bomb drops, expiry blasts and village attacks have visible effects while hidden actors/targets remain hidden.

## Optimizations and structure

- Fishing vision iterates its bounded square instead of every map tile.
- Mesh vertex incidence is built in one pass; centers, closed outlines and oriented seams are cached.
- Movement boundaries are cached until the reachable set or projection changes.
- Hover pathfinding runs when the hovered tile changes; screen action placement runs when the camera/viewport changes. HUD layout assignments skip unchanged dimensions.
- Balloon and village lifecycle code is separated into focused BattleState partials; Village is an explicit Core model. Village HUD is a small Presentation partial. C# whitespace is normalized in touched source files.

## Explicit decisions

- Explosion footprint is exactly nine tiles, confirmed by the user.
- Fortification price is configurable and defaults to 5 Thors; no price was specified in the request.
- Villages initially build Fishing Ship/Garrison and unlock Frigate at 3, Kolonel at 4 and Togus at 5, following the requested Mothership progression.
- The free level-two Fishing Ship is a reward and can exceed the normal fleet cap; it spawns in the nearest unoccupied connected water tile and is ready next turn.
- Existing damaged-hull scaling, veterancy, economy, visibility rules and terrain movement multipliers remain except for the requested changes.
- Save/Continue was absent before this work and remains future scope. There are no existing saves to migrate. Android export/device validation was not part of this request.

## Checks

Core checks cover hulls, all upgrade alternatives, collection without selection, transit, diagonals, Balloon bombing/expiry and village lifecycle/production/fortification. Runtime checks exercise actual Godot projection, picking, mouse/touch, contextual actions, upgrade dialogs, hidden combat and visual capture scenarios. Full AI games must finish. Use the commands in README; successful runs print PASS.
