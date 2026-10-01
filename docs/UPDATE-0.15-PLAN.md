# 0.15 work plan — user-requested gameplay and presentation update

Delivered as the 1 October 2026 checkpoint. All required outcomes below are
implemented and validated; concrete results and limits are in MILESTONE-0.15.md
and DEBUG-0.15.md. The remaining first-exploration peak is documented explicitly.

## Required outcomes

- Reproduce hover/movement lag on a real organic map and compare handler, draw,
  movement-model, save and frame timings before/after fixes. Retain diagnostic mode.
- One latest voyage slot: New game replaces primary and removes prior-voyage backup.
- Coherent shared flowing grid lines, less distortion, continuous rounded islands,
  grouped trees, larger mountains and moving translucent clouds/shadows.
- Economic scarcity: higher construction investment and controlled income growth;
  explain new values and verify several turns of progression.
- 1–4 independently hostile enemy factions, unique fleet colors, size-scaled map,
  fair starts/resources and persistent roster/turn order.
- Adjacent collection, buildable cannon defense tower, Galleon/Kolonel range two.
- Source-specific income popups and red hostile HP.
- Asymmetric terraced floating town Mother/menu model, varied beige homes,
  golden-domed temple, markings, chimneys, props, bunting/laundry and carts.
- Turn a cannon ship's broadside toward its target before firing; mortars swivel
  their barrel independently. Keep visual salvos independent of damage.
- Minimal papyrus HUD, unfolding radial actions including locked items, dry-brush
  icons and concise sage-style information for inspected objects; right-click cancel.
- Updated guide/readme/changelog, regression tests, graphical review and new build.

## Ownership and compatibility

Root: real interaction profiler, hover/movement lifecycle, disk/session saving,
ships/menu-town visuals and integration. Atlas: factions/economy/tower/Core save
migration. Terra: map/shoreline generation and terrain rendering. Sol: papyrus UI.
Preserve all prior shipped builds and user saves while testing; use isolated slots.
Existing enum identities remain valid; old balance snapshots continue with their
original rules. Latest user requirements supersede old side counts/default balance.
