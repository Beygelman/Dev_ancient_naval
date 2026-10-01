# Ancient Naval 0.17 — Tides and Parchment

1 October 2026 · Godot 4.7.2 .NET · .NET 8 · Windows x64

## Delivered behavior

Projectile combat now has a real impact boundary. Core resolves an isolated
deterministic command, then applies its saved phases when the corresponding
salvo finishes flying. HP, kills and veterancy in the live aggregate stay
unchanged during aiming/flight. Counterattack commits after its own flight.
The scene locks input and cannot save an unresolved projectile. Exit finishes
the order before saving. Immediate Core commands still serve AI simulations.

Wrecks descend, tilt and fade under a water veil. A Mother splits into two
separating deck halves; its escorts remain until that wreck submerges, then
sink together. Splash, bombs, village replies and whirlpools also produce wrecks.
Offscreen/hidden combat cannot create a spectator view or reveal action messages.

Tree groves and mountain ridges use continuous seeded world-space fields rather
than independent per-tile decoration. Their cells own fog visibility only.
Three tree silhouettes and multiple peaks can share a cell. Native Y sorting
places each tree, peak, village and hull using its individual ground anchor.
Mills/flags belong to the village canvas, not a flat background layer. Hidden
explored towns keep their last observed appearance. Rounded connected island
contours receive four smoothing passes; narrow beaches use guarded explicit
triangles to avoid occasional engine triangulation failures.

The cloud is one connected-square polygon, so overlapping rectangles do not
multiply its opacity. Its high air layer is separate from its surface shadow.
Gulls likewise have separate air/shadow layers. Ambient transient gulls are
capped at 18 (formerly 40), with groups of 1 or 3 every 4–7 seconds. Each dock
has three circling gulls (formerly seven).

The papyrus unfolds symmetrically from the bottom with growing arc and thickness.
Only command icons remain on the paper; prices/descriptions remain in tooltips
and Info. HP/progression follow its inner arc. Health bars are removed. Damage
starts at the HP number, counterattack is yellow and all healing is green.
Town names sit under villages. Twenty captain names are shuffled without repeats
within a voyage, and forty town names use a separate cosmetic seeded stream.
Both persist; old saves receive names without advancing combat/treasury RNG.
Opponent status announces only the acting captain; the human gets **Your turn**.

Destination forecasts show separate cannon, mortar and collection contours from
the prospective position. Selected mortars show their minimum/maximum firing
annulus. The route is a continuous interpolated light-burgundy dashed line with
a destination X. Coverage caches are bounded and never include unknown terrain.
Corner movement remains one tile in clear water, two beside hostile warships;
enemy occupancy and land-corner restrictions remain effective.
Cannon Tower range is 4. Other combat balance is unchanged from 0.16.

## Verification

- Build: zero warnings/errors.
- Core: 1,535 basic checks, 1,198,194 rules/navigation/combat checks, 23 save
  compatibility/validation checks, eight legacy-policy and two multi-fleet AI games.
- New Core cases verify unchanged live HP/veterancy before impact, idempotent
  impact, blocked commands/saves, stable surviving entity identity, escort
  retention, final-state equivalence to immediate resolution, persistent names
  and actual diagonal/threat movement costs.
- Actual Windows D3D12 renderer: 116 effects checks including a low-HP Mother
  impact followed by staged escort sinking; all salvo counts, full-circle city
  rotations, outpost/reef and veterancy visuals remain checked.
- Menu/new voyage: 63 checks; a separate resume run: 46. Battle: 82; sea events:
  108; income: 6; smoke/gameplay: 248,659; draw-preparation benchmark: 11.
- All four map sizes: 61,413 shoreline/cache checks. The final three-rival
  close-up passes 16,609 checks including native shared Y sorting and one anchor
  per tall object; route/landscape screenshots are inspected. The 1,110-cell sample has
  1,235 trees and 113 peaks across 16 smooth island contours.
- Hover reuses known world draw commands and cannot update the terrain raster.
  Texture axis and total-pixel limits stay at 4096 and 8,388,608.

Logs and raw profiling reports are in `docs/diagnostics/0.17`.

## Performance and practical limits

The real-map profile uses 64 cursor destinations plus a legal animated Brig
move. Reported handler times exclude render submission and the deliberate hull
acceleration/deceleration. The first full-board 1,110-cell run measured hover
p95 0.064 ms and synchronous movement 3.76 ms; the final repeat after depth/air
integration measured hover p95 0.094 ms and movement 4.89 ms. The largest
1,371-cell, four-rival map with live fog measured hover p95 0.078 ms and movement
3.73 ms. Raw reports include allocations, frame times and engine monitor readings.

Across these profiles, observed frame p95 was approximately 18–33 ms with a
107 ms peak in the final fully revealed run. Movement animation lasts roughly
1.6 seconds by design. These are
short runs on the current shared RTX 4060 Laptop GPU, not a guarantee of fixed
FPS, large-fleet scalability or low-end hardware performance. Ambient shoreline
waves/fish remain significant render-submission work on fully revealed close-ups.

An early draft staged every command and made movement cost about 115 ms and
29 MB of temporary allocations. Movement/build/collection now execute directly;
only commands needing impact timing stage snapshots. The retained landscape,
bounded destination cache and decimated animated shoreline samples keep hover
independent of that combat staging work.

## Compatibility and release

Save/session versions remain 1. Continue preserves saved geometry, rules, fleet
palette, progression, discoveries and RNG. Names are optional new fields with
deterministic defaults. Start **New game** for the range-4 Cannon Tower and newly
generated scenery. There is still one voyage slot; its backup belongs to the
same voyage. Diagnostic save paths are separate from the real user slot.

Windows executable, complete runtime ZIP, source ZIP and SHA256 manifest form a
separate 0.17 checkpoint. Released 0.14/0.14.1/0.15/0.16 artifacts and the user's
beta roadmap are checked against their previous bytes. The static generated
0.16 menu illustration and requested v0.13 footer are intentionally retained.
The exported executable is launched separately in title and map-preview modes,
with disposable save paths, and exits with code 0 without engine errors.
The editor's .NET restore/build is repaired after export so F5 remains usable.
