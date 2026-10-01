# Architecture refactor after 0.14

The playable 0.14 release is the behavior baseline. This work changes internal
organization and measured hot paths, not the game rules or the visual direction.

## Dependencies and ownership

- `Core` is an engine-independent simulation. `Presentation` consumes its state
  and command results; effects never apply damage or advance turns.
- Keep `BattleState` as the public command facade and owner of mutable battle
  state. Group navigation, combat, production and turn handling by responsibility.
- Extract reusable path search from battle rules. A navigation query may snapshot
  occupancy and threats for one synchronous search, never across commands or
  visibility updates. Preserve route tie order and unknown-terrain behavior.
- Move selected mechanic values into validated typed balance settings, retaining
  0.14 defaults when reading old saves. Do not change serialized enum identities.
- Isolate save DTOs, validation and disk replacement. Verify v1 fixtures, random
  continuation and recovery when the primary file is damaged.
- Separate ship drawing from command animation. Reuse frame-local buffers and
  update layout on resize. Measure allocations before claiming improvements.
- Split input handling from the scene composition root; keep existing public
  methods used by the UI and runtime checks.

## Verification gates

1. Deterministic navigation comparisons against the original traversal, including
   fog, radar contacts, friendly transit, enemy threats and irregular topology.
2. Existing core suite (100 map seeds and complete AI games), plus focused tests
   for configuration and persistence changes.
3. Godot build and runtime smoke/combat/effects/menu/sea checks, using isolated
   test saves. Compare representative screenshots after presentation changes.
4. Record actual results and remaining limits in `CHANGELOG.md` and `README.md`.

## Deliberate limits

This is a turn-based game: economy and AI run on turn/command boundaries, not
wall-clock timers. Streaming, ECS, a global event bus and generic object pools
require evidence of a bottleneck and are not prerequisites for this refactor.
Supporting hundreds of units is a future capacity target, not a release claim.
