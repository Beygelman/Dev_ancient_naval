# Development state — 16 October 2026
## Verified source
Base: main at 0c78297469d6d278334ea1c59d43fc4912677548 (v020.8b).
Target: v020.9x1 (user-facing v20.9x1), experimental.
Branch: codex/ancient-naval-v020.9x1.
No previous v020.9x1 source was found remotely; reimplement from verified base.
Direct GitHub clone/archive access times out. Read bounded source through the
connector and publish only reviewed changed paths. Local .NET/Godot unavailable;
use prepared CI for build checks.

## Completed
Documentation checkpoint: concise root guide, Core/Presentation guides, code map.
Full previous guide preserved verbatim in docs/history/AGENTS-before-v0209x1.md.
Stage 1 source implemented: immediate own command commit; nonblocking own effects;
explicit opponent-only focus; shorter sinking; visual settle on next order/world swap.
Added native Action0209Checks and a CI workflow for Core/build/graphical checks.
Stage 1 CI passed: full Core suite, Godot build, 19 graphical action0209 checks.
Evidence: Actions run 38888534402. Combat/trade source added next; validation pending.

## Authorized stages
1. Permanent camera/animation correction: own actions do not auto-focus; next legal
   order need not wait for effects; exact-once Core; ordered opponent replay.
2. Saved combat experiment: first shot, surviving target replies, second shot if
   attacker lives. One reply; retain existing second-shot damage. No simultaneous
   movement/HP/price/Galleon rebalance.
3. Saved trade experiment: one coin per other connected friendly port, capped +2.
4. Cities: facility slots at levels 2/4; level 5 improves one chosen facility.
   Trade yard, repair dock, arsenal; unique types; paid replacement; dock repair
   consumes serviced ship action; lost capacity blocks new builds, never kills hulls.
   Preserve normal hull unlocks and historical saved economy.
5. Integration checks, version metadata, changelog. Report native checks and player
   packaging separately from source/build success.

Deferred: Galleon move-fire-move, Kolonel movement nerf, flagship Restoration changes,
production queues, caravans, supplies and national mechanics.

Next: inspect CI for stage 1; fix failures before integrating combat/economy.

