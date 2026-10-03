<<<<<<< Updated upstream
# Organic hexagonal maps — 0.15

The references have coherent curved rows flowing through occasional three-way
and five-way junctions. The useful feature is shared topology and direction,
not random outlines for individual cells. The generator constructs one connected
mesh first; every cell then inherits its sides from that mesh.

1. Pick six different boundary subdivision counts. Map area scales approximately
   with fleet count: linear scale is `sqrt((rivals + 1) / 4)`.
2. Solve the circumradius for six chord angles summing to a full turn. Chord
   lengths follow the subdivision counts. All six boundary sides remain straight.
3. Fit six bilinear patches between the center, corners and boundary divisions.
   Shared vertices have common IDs. Smooth parameter spacing varies cell sizes.
4. Redirect paired interior rows to produce local junctions and changes of flow.
   Keep boundary vertices fixed. Apply broad seeded displacement and swirl,
   without independent per-vertex random jitter.
5. Accept relaxation and topology changes only while neighboring cells retain
   safe area, edge lengths and convex corners. Minimum angles are 38 degrees
   in map space and 32 degrees after isometric projection.
6. Split or collapse selected interior connections to form triangles and
   pentagons. Roughly nine out of ten cells remain quadrilaterals. Gameplay
   addresses are IDs; their values do not imply square adjacency.
7. Pair opposite half-edge directions at junctions to derive coherent tangents.
   Compute each interior cubic seam once, sample it six times, and share that
   exact curve in reverse between neighboring cells. Validate intersections and
   picking, falling back to a straight shared seam if a curve is unsafe.
8. Index edge and corner adjacency for movement and range. An ordinary corner
   crossing costs one step; terrain and hostile threat costs still apply.

| Rivals | Fleets | Edges per side | Cells at seed 731 |
|---:|---:|---:|---:|
| 1 | 2 | 17–23 | 611 |
| 2 | 3 | 21–27 | 879 |
| 3 | 4 | 24–30 | 1,110 |
| 4 | 5 | 27–34 | 1,371 |

Sources: `Core/World/OrganicMesh.cs` and `Presentation/Map/IsometricProjection.cs`.
Rectangular boards remain for focused rule fixtures. Saves preserve exact
vertices/faces, so Continue does not regenerate the map.

## Islands and deployment

Islands use rotated multi-frequency radial outlines with spatial perturbation.
Start berths remain at sea, and narrow passages are widened. Larger island
placement cycles through starting territories. Deployment uses a random axis:
two fleets are opposed; additional fleets occupy separated perimeter anchors.
Each starting territory receives the same village count, three treasuries and
one pirate patrol. This balances access counts rather than promising identical
travel distance or terrain in every procedurally generated position.

Rendering follows the boundary of each entire connected island. It decimates
short segments and applies four shared Chaikin rounding passes. That outline
is clipped against individual cells for fog privacy. Beaches have continuously
varying widths; inset points are relaxed together to avoid crossed strips.
A translucent offshore band follows the coastline rather than coloring whole
water cells. Dense forests and winding mountain chains follow continuous seeded
world-space fields, crossing tile boundaries; a cell owns only their fog mask.
Three tree silhouettes and several peaks may occupy one cell. Trees, peaks,
villages and hulls share native Y sorting at individual ground anchors.
Clouds are coherent connected-square translucent silhouettes; they drift in an
elevated air layer above separately rendered surface shadows.

## Verification

Core checks exercise boundary counts, disk topology, symmetric adjacency,
determinism, naval passage, balanced territories and independent factions.
Godot checks additionally triangulate coast/beach geometry, check quadrilateral
majority, groves, peaks and raster limits. Hover must leave terrain unchanged,
while discovering a cell builds only its geometry. Changing an already known
cell's visibility must reuse its retained geometry and change only its tint.
=======
# Organic hexagonal maps — 0.14

The references show coherent rows that curve and split around occasional three-way/five-way junctions. Independent random polygon outlines would look like shattered glass. Our generator instead deforms a connected quadrilateral patch mesh and changes its topology locally.

1. Choose six radial subdivision counts. Neighboring counts determine the six boundary side counts; accept only six distinct counts in 24–30.
2. Solve a circle radius whose six chord angles sum to one turn. The chord lengths are proportional to the chosen counts, producing a convex hexagon with six unequal straight sides.
3. Fit six bilinear patches between the center, corners and side division points. Shared patch vertices use common IDs. Smooth nonuniform parameter spacing changes cell sizes without gaps at patch seams.
4. Reconnect local rows in pairs, creating three/five-way junctions and new directions of flow. Keep boundary vertices fixed.
5. Apply broad, seeded swirl and displacement fields. Accept each local relaxation only while incident cells remain convex, with safe areas and edge lengths: at least 38° in map space and 32° after isometric projection.
6. Split/collapse selected interior connections to add triangles and pentagons. Around four fifths of cells remain quadrilaterals. No square-coordinate movement assumption is made.
7. Index actual edges and shared corners for movement/range calculations. Corner crossings cost one normal step, subject to coastal and enemy-zone penalties.
8. Render each shared seam once as a sampled curve reused by both cells. Validate curved boundaries against intersections and picking; fall back to a straight edge when needed. Outer map edges stay straight.

The source is `src/Core/World/OrganicMesh.cs`; `IsometricProjection` adds curved seams and spatial picking. Rectangular boards remain only for focused gameplay fixtures. Save files retain exact vertices and faces, so Continue never regenerates a different layout.

## Geography and deployment

Seeded islands use rotated, multi-frequency radial shapes and a spatial perturbation field. Separate land components, a fleet-to-fleet sea corridor and broad start berths preserve navigability. White beaches vary continuously in width. Grass, sparse tree clusters, isolated peaks and winding mountain ridges are a cached visual layer, without changing movement terrain.

Both fleet anchors lie on opposite ends of a common random axis through the hexagon. Territory is assigned by proximity to these anchors. Village counts are balanced across the two territories; three treasuries and one pirate patrol are placed in each. Current play supports two player factions.

## Verification

One hundred seeds check six straight sides, exact boundary tile counts, manifold edges, disk topology without holes, symmetric corner adjacency, reproducible land, safe passage and equal village counts. Actual Godot checks validate curved polygon triangulation and edge picking. Representative seeds 42, 101 and 723 contain 1,116 / 1,058 / 1,072 cells respectively.
>>>>>>> Stashed changes
