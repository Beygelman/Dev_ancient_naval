# Ancient Naval 0.20 — River Kingdoms

Delivered 1 October 2026. The authoritative source project remains `C:/__Beygelman/! -11/dev-ancient-naval`. Complete local release copies are under `releases/0.20/`; the external outputs folder is an additional mirror. Earlier releases and user saves are preserved.

## Player-facing changes

- Ready claim and treasury actions automatically unfold beside their world targets, even without selecting them. Dark-burgundy dry-brush scenes bend with the downward semicircular papyrus, float gently, close symmetrically inward and burn. The old flag/loot action buttons and capture-marker click region are removed.
- Ship, town and resource action fans follow their selected world object again, unfold upward and stay inside the viewport. Coin-and-price badges appear above purchase icons; free level rewards show zero cost. A Thor coin also accompanies the main currency number.
- New voyage setup has a centered parchment panel, six fleet emblem previews, rival/difficulty choices and a visible map description. Red joins the existing colors.
- Motherships and owned settlements share the chosen monument: gold dome, silver faceted cap, pink pyramid/white diamond, black cross, dark-green tree or red crystals. Neutral towns have houses without a faction temple. Higher-level towns and flagships add taller/more houses; roofs vary within their fleet hue. Fields stand in front of the walls. Towers gain masonry, slit windows, battlements and flags; balloons gain sewn panels, rigging and woven baskets.
- New Pangaea maps contain linked lakes, meandering arteries, dead-end tributaries and narrow bypasses. Each territory prioritizes two interior river/lake villages and one exterior village. Interior fish sites are prioritized, while starting-fleet collection opportunities remain.
- New voyages permit every hull through one-cell waterways without coast/narrow terrain penalties. Land, whirlpools, occupied destinations, closed land corners and enemy threat costs still apply. Kolonel cannons can destroy balloons within their standard gun and anti-air range.
- White sea lanes curve like the movement preview and terminate beneath the port. Port piers straddle the town/sea-berth boundary, with a road to the village center. This changes appearance, not berth occupation or trade discounts.

## Compatibility and validation

`FreeCoastalNavigation` and `Balloon.KolonelAntiAir` are optional saved rule fields. New balance enables them; missing fields retain previous rules. Red is appended to the persistent color enum. Continue restores the actual saved mesh, land, resources and rules; it never regenerates old Pangaea maps.

Validation includes the pure-.NET regression suite and 79 new checks for all hulls in narrow rivers, actual movement vs previews, Kolonel AA, old-rule defaults, red saves and twelve Pangaea seeds. Native graphical checks exercise six fleet models, all town levels, ports, world-anchored fans, price badges, all four world policies, real pointer clicks on the claim/treasury scrolls, atomic animation/commit boundaries, fog privacy and save/reload. Screenshot evidence is in `diagnostics/0.20/`.

## Scope

This is a playable local prototype with procedural vector models, not a claim of final art or competitive balance. New terrain and rule changes are seen by starting a new voyage; Continue keeps the existing voyage's saved rules. The organic six-sided cell mesh, fleet progression, economy and existing victory system remain in place.

[Image assets and edit prompts](ACTION-ART-0.20.md).
