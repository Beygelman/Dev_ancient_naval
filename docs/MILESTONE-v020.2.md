# Ancient Naval v020.2 — corrected edition

3 October 2026. The requested corrected identifier is delivered separately from
the preserved historical `0.20.2` archives. Future labels are `v020.3` through
`v020.9`, then `v021`.

## Playing and changes

Extract the complete Windows ZIP and run **Ancient Naval.exe**. Keep its PCK and
.NET runtime beside it. For development, open **project.godot** in Godot 4.7.2
.NET, stop an old run with F8 and start with F5.

- Galleon's normal attack radius is two tiles, one more than the preceding
  balance. Existing saves retain their saved combat rules; start a new voyage
  for the new default range.
- One continuous rounded island surface is clipped across tile seams. Water
  cannot overpaint its inland bend, and sand no longer fills the whole angular
  coastline tile. Internal lake contours subtract water holes correctly.
- Major inland summits use a doubled nominal size, fitted to actual land and
  away from sand. Smaller summits connect neighboring ridge cells without
  introducing new sight obstacles. Denser continuous forest fields cross tile
  seams while retaining broad meadow regions.
- Object parchment unfurls symmetrically around the selected ship or town.
  Each command occupies a one-eighth circle; more commands extend the parchment
  beyond a semicircle, up to a full ring with a small seam. Hull size affects
  its inner clearance. Legal target centers take input priority over the ring,
  including anonymous radar contacts.
- Claim and treasury banners share the command parchment's dimensions, open
  once, hover until activation and burn before one validated capture/collection.
- Selecting a cannon target with Kolonel or an upgraded two-shot Mothership
  opens a two-icon parchment below the target: one cannonball or two. There are
  no prices. Double charges launch simultaneously, consume two shots and receive
  one reply. Mortar attacks remain single. Hidden radar targets never expose
  hulls, health, damage digits or sunk outcomes.
- Upgrade choices display mystical names. Hover parchment explains their actual
  effects using the voyage's saved rules. Producer information describes only
  the producer and lists available construction names, including Lighthouse.
- Cities use a smaller, spaced, deformed isometric lattice with varied roofs.
  A closed square wall has higher masonry, crenellations and four corner towers.
  The shared future-town footprint fits inside land with clearance from sand:
  houses, walls, animated mills and flags retain their common placement through
  upgrades. Wheat and irregular soil are clipped independently. Roof silhouettes
  remain intact. Port piers intentionally extend from land into sea.
- Mortars on ships and ruins have a short hollow bronze bombard, reinforced oak
  carriage, iron bands, trunnions and a detailed rim. Cosmetic geometry does not
  change combat damage or range.
- Admiral concentrates multiple ships' fire and advances against observed
  targets, evaluates return fire and preserves charges on near-fatal targets.
  Captain recovers from newly discovered route obstructions. Existing units'
  optical sight is reduced by one tile.
- Number-only damage rises above health amphorae. Victory/defeat can be dismissed
  to inspect the map. Leaving a finished voyage removes the save and backup and
  hides Continue; unfinished voyages retain their slot.
- New-voyage setup is on the right. English, Ukrainian and Dutch can be switched
  on the title screen or game menu; the active language is marked and saved
  separately from voyage state. Names and numerical rules remain unchanged.
- Lighthouse costs 6 Thors, has 10 HP, sight 4 and a radar upgrade costing 2 Thors.
  A Mothership or level-three town builds it on adjacent free sea. It occupies
  rocks at the tile's upper-left corner and joins port routes. Shared sea-lane
  edges are drawn once as a dashed network.

## Rendering and release checks

Sea geometry, shore segments and wake lines use retained triangle batches.
Animated sea waves use a retained mesh and a visibility-scoped shader; movement
and fading no longer rebuild and upload its triangles each animation frame.
Hull selection invalidates only the old and new selected hull.
Terrain uses bounded raster regions and a queue that updates one local region
per frame during discoveries. Static town layers are bounded one-shot textures;
pan and hover reuse them. CPU scopes are opt-in and disabled in ordinary play.

Repeated D3D12 Mobile runs on this host still produced first-move stalls of
386–499 ms, despite improved steady frame intervals. The delivered 2D project
uses Godot's OpenGL Compatibility renderer, which is verified separately with
native graphical checks. This is an intentional renderer change; there is no
silent relaxation of the frame gates.

Before each delivery, **tools/Check-ReleaseSmoothness.ps1** must run on the final
Debug assembly. Its native cases cover a 1,371-cell wide Pangaea with four rivals
and Oceans with four rivals, live fog and a disposable autosave. Limits are pan
p95 ≤25 ms / max ≤80 ms, interaction p95 ≤25 ms / max ≤100 ms and synchronous
hover max ≤16 ms. The harness blocks unrelated desktop input while invoking the
production selection, preview and movement paths. Separate UI checks use actual
mouse and touch events.

Final measurements and executed checks are recorded in
[the validation record](diagnostics/v020.2/Validation.md). Native debug timings
describe this host and workload; they are not a guarantee of constant frame rate
on other computers. Exported Windows startup and translated title screens are
checked separately because release templates disable test hooks.

## Compatibility

Choose **New game** for the new balance and optional mechanics. **Continue**
retains saved rules, progress, resources and Latin nation/city names. Missing
optional rules preserve historical behavior. Simultaneous visual projectiles
still commit staged Core damage exactly once before saving. Language choice
cannot mutate a voyage. Original source/assets, user roadmap and historical
release checksums remain preserved.
