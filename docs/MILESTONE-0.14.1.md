# 0.14.1 — Architecture checkpoint

30 September 2026. This checkpoint applies the supplied agent guide to the
working 0.14 game. Balance defaults, procedural art and v1 save identity are
preserved. `AGENTS.md` is the adapted guide; the source document is archived as
`AGENT-GUIDELINES-ORIGINAL.md`. `REFACTOR-PLAN.md` records scope and dependencies.

## Implemented

- Cohesive partial battle/scene modules with stable public command APIs.
- Independent stable Dijkstra and short-lived indexed naval navigation policy.
- Typed validated special-weapon/treasury settings and accurate HUD descriptions.
- Explicit snapshot DTO, save validation and staged restoration.
- Safe backup rotation after recovery and first-write replacement of corrupt saves.
- Stable reused drawing list, separated fleet commands/drawing and ambience buffers.
- Consistent formatting of edited modules; formatting verified identical syntax
  tokens before a standard whitespace formatting pass.

## Measured hot paths

Release .NET synthetic full-board search on a 28×28 water board; **3 searches**
per measurement, both algorithms warmed up. Original traversal remains a test
oracle. Values are total allocated bytes and elapsed milliseconds for the batch.

| Fleet | Before bytes | After bytes | Reduction | Before ms | After ms |
|---:|---:|---:|---:|---:|---:|
| 16 | 42,098,928 | 2,117,184 | 95.0% | 23.287 | 1.822 |
| 128 | 94,398,960 | 2,040,264 | 97.8% | 112.782 | 1.560 |
| 256 | 129,229,224 | 1,967,664 | 98.5% | 178.131 | 1.544 |

Godot debug idle draw-order preparation; **300 iterations** per fleet. The
fixture uses fishing units to avoid changing the normal combat fleet cap.

| Fleet | Before bytes | After bytes | Reduction | Before ms | After ms |
|---:|---:|---:|---:|---:|---:|
| 16 | 787,200 | 324,000 | 58.8% | 1.739 | 1.442 |
| 128 | 4,051,200 | 2,743,200 | 32.3% | 16.402 | 16.110 |
| 512 | 15,151,200 | 11,037,600 | 27.1% | 64.096 | 71.490 |

The rendering change reduces allocations in all three fixtures. The 512-unit
timing is slower in this run, so no rendering speedup or FPS improvement is
claimed. Snapshot records still allocate; GPU work and entire-game performance
were not benchmarked. Navigation performance depends on board/visibility/fleet
composition; these synthetic fleets are not a large-fleet gameplay guarantee.

## Verification

- .NET/Godot build: zero warnings and errors.
- Core: 1,535 foundational assertions; 1,195,229 rules, geometry and navigation
  assertions, 100 generated map seeds and 8 complete AI games.
- Save: 20 assertions including two actual pre-refactor 0.14 session saves,
  malformed collections/references and stable v1 resave.
- Godot: 474,809 smoke checks, 99 graphical battle checks, 41 graphical effect
  checks, 108 sea-event checks, 27 graphical menu/save/recovery checks and 11
  draw preparation/layout checks.
- Continue in a separate process: 13 checks. Windows export starts and renders
  a generated map using the compatibility renderer without reported errors.
- Before/after navigation comparisons include fog, radar contacts, enemy threats,
  friendly transit, diagonals, narrow passages, organic topology and pirate patrols.
- Visual captures cover title, color selection, fleet motion/salvos, mortar splash,
  anti-aircraft attack and villages. Test files use isolated saves.

## Remaining architectural work

The battle facade still owns shared state by design. Several legacy derived
statistics and fixed values remain in units/villages; migrate those alongside
future balance work. Streaming, additional factions, campaigns and fully pooled
render snapshots are future work. No new dependencies were added.
