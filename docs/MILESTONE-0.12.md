# Ancient Naval 0.12 — Organic Grid Preview

This is a playable checkpoint for reviewing the new map before further development.

## Reference analysis

The supplied patterns show coherent bands of quadrilaterals bending around sparse exceptional vertices. A three- or five-way junction changes the direction of several rows at once. These junctions are distinct from triangular/pentagonal **cells**. Randomly jittering a rectangular grid cannot reproduce this topology. The colored regions in the second reference are not used as gameplay regions.

The implementation is an original construction inspired by those visible properties; it does not claim to reproduce the references' original generator.

## Generator

1. Choose five different integer side counts, each 18–24. Construct an unequal cyclic pentagon whose side lengths are proportional to these counts.
2. Fit five quadrilateral patches to its actual boundary. Shared patch vertices have exact topological IDs, avoiding floating-point cracks. The outer boundary is straight, without clipping a staircase of square cells.
3. Reconnect selected pairs of quadrilaterals across the other diagonal of their combined six-vertex boundary. This introduces paired three/five-way junctions. Relax their surrounding rows while pinning the outer boundary.
4. Apply a smooth, seeded displacement field, then small constrained local adjustments. Reject changes that fold cells, shrink edges excessively or create unusable angles.
5. Split selected interior junctions into pentagons and collapse a few interior edges into triangles. More than 80% of cells remain quadrilaterals. Generated maps use only three-, four- and five-sided cells.
6. Sample gently curved shared interior edges once. Keep all external seams straight. Validate curved tangents and triangulation; picking, highlights and contours reuse exactly the same edges.

Minimum angles: 38° in the map plane and 32° after the 2:1 isometric projection. A seed reproduces the geometry, islands and initial resources. A fresh game chooses a new seed. No claim of mathematical uniqueness across every possible seed is made.

## Gameplay integration

`PentagonalMesh` is engine-independent and belongs to Core. `GridPosition` is a stable address, not a geometric coordinate on generated maps. Neighbors come from shared edges; diagonals come from shared vertices. Each movement step costs one base tile, including diagonals; existing coast and narrow-channel multipliers remain. Sight, radar, collection, attacks, production, AI routes and Balloon flight use this topology. Distances are cached after a breadth-first traversal from each needed origin.

The Balloon expiry area remains nine local cells: the origin and its eight nearest surrounding cells. At an exceptional junction it can include a second-ring cell to retain exactly nine. Rectangular rule fixtures preserve their exact 3×3 footprint.

## Included earlier changes

- Towns persist at 0 HP. Only then can an adjacent eligible ship capture them. Capture restores HP; a defeated town pauses income, production and growth.
- English unit names: Brig, Galleon, Kolonel, Granado, Fishing Schooner.
- Mothership: broad twin hull, two bows and three large sails. Hull sizes distinguish the other classes; Granado has a broader deck. Veterans gain a raised stern deck and silver figurehead.
- Visual salvos: Brig 1, Galleon/Kolonel 3, Mothership 2; mortar 1 slower heavy shell. Projectile count does not multiply damage. Faster cannon arcs land within the target hull.
- Whole-route acceleration/braking, idle rocking, recoil, impact movement, directional smoke and size-scaled wakes.
- Faint sea/coast waves, occasional gulls with shadows, rare dolphins, rotating village mills and animated allegiance flags.

## Preview build

Windows x64, self-contained .NET export. `Ancient Naval.exe` starts the normal game. `Preview map.cmd` starts the same build with `--map-preview`, revealing the chart and both fleets for inspection. This is an explicit preview mode; the normal executable keeps fog of war. Drag to pan, wheel to zoom, Menu → New game to regenerate. Save/Continue remains absent in this prototype.

## Verification

Core checks cover 100 seeded maps, exact tile counts on all five straight sides, connected topology, manifold edges, symmetric neighbors, nine-cell blasts, island separation and reachable harbors. Runtime checks exercise real Godot triangulation, points near every edge, picking, controls, combat and animation. Eight AI matches must finish using legal commands. Exported Windows executable receives a separate launch check.

No external raster assets or dependencies were introduced. The static map does not redraw for ambient motion. Animated effects use separate layers and bounded particle lists.
