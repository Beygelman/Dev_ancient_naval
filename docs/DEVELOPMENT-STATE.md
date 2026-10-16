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
Concise root/subsystem guides and code map; original guide archived byte-for-byte
(blob 4d6f510f34e770443a845735b0e8e1c8e6deaafb; root reduced from 51 KB to 5 KB).
Permanent animation/camera changes plus saved combat/trade/city experiments, AI,
validation, EN/UK/NL controls and v020.9x1 metadata implemented.
Full Core suite passed, including 59 new checks (Actions run 38891934413).
All 43 new graphical action/city tests passed on that run; screenshots inspected.
The old sinking fixture still used own delayed impacts: updated it to explicitly
exercise staged presentation, retaining the original phase assertions.
Added a gated Windows export, real exported-player startup and SHA256 package job.
Latest integration validation and Windows package are pending; PR #7 is a draft.

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

Next: inspect final Core/native/Windows CI; fix failures, record evidence and finalize PR #7.

