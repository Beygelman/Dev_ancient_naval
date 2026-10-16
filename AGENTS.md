# Ancient Naval — working agreement
Read this at the start of each session. The newest user request sets scope.
This guide supersedes operational instructions in the archived long guide.

## Start
1. Read docs/DEVELOPMENT-STATE.md and the relevant row of docs/CODEMAP.md.
2. Inspect Git status, branch, remotes and recent commits. Preserve user edits,
   real saves, historical archives and newer source; never reset or force-push.
3. Read the nearest subsystem AGENTS.md. Search symbols with rg, then read bounded
   sections. Do not load the whole codebase, chat export or historical guide.
4. Check relevant files for conflict markers and verify the baseline.
5. Use one local checkout. Wait for a pending clone; never simultaneously fetch
   the same repository file-by-file. If cloning is blocked, record the limitation,
   retrieve only needed source through the connector and use prepared CI.

## Economical implementation
- One primary agent by default. Delegate only when explicitly authorized and useful
  for independent work. Give short briefs, paths and acceptance criteria rather
  than full history; assign one owner to each shared file.
- Complete one bounded change at a time: implement, focused checks, diff review,
  coherent commit, remote checkpoint, short state update. Continue authorized
  stages autonomously without routine confirmation between them.
- Prefer targeted patches. Split a file only at a clear responsibility boundary.
  No mechanical fragmentation, unrelated refactors or speculative frameworks.
- Batch independent reads/searches. Keep output concise, inspect full logs only on
  failure. Reuse unchanged context and completed checks until changes invalidate them.
- Check installed engine/SDK first; reuse a prepared environment. Do not migrate
  versions or repeatedly install tooling merely to work around this host.
- Use current model for ordinary implementation. Increase effort/delegation only
  for a concrete unresolved problem. Do not invent token-saving percentages.

## Permanent contracts
- Godot 4.7.2 .NET, C#, .NET 8. Preserve Windows, Android/touch constraints and the
  existing iOS NativeAOT path. No engine/runtime migration without user request.
- Core is pure .NET; BattleState owns gameplay commands. UI is not a second model.
- Preserve deterministic simulation RNG, fog/radar secrecy, serialized identities,
  legal AI knowledge, actual purchase-price refunds and historical save behavior.
- Optional new rules default to old behavior when absent. New voyages may enable
  experiments; Continue retains embedded saved rules. Include validation, AOT JSON,
  AI/forecast behavior and localized UI where relevant.
- Preserve ancient papyrus/clay/ink art, native input ownership, EN/UK/NL and touch.

## Permanent animation/camera policy — 16 October 2026
Player actions never auto-move the camera. Automatic action focus is allowed only
while showing opponent actions during end-turn replay; manual navigation remains.
Own attack/sinking effects must permit the next legal order without waiting for
the full animation. Damage, counterattacks, crashes, rewards and fleet collapse
apply exactly once. Skipping visuals, saving and world replacement must never
duplicate results or mutate a replacement battle. Opponent replay remains ordered.
Carry this into future versions and old saves; it is not an economy experiment.

## Checks and checkpoints
- Meaningful regressions for changed behavior/invariants; no tests for trivial docs
  or tests that merely mirror implementation. Focused checks during development,
  broad integration once at the end. Baseline commands:
  dotnet run --project tests/CoreChecks/CoreChecks.csproj -c Release
  dotnet build Dev_ancient_naval.csproj
- Native Godot checks are necessary for camera/input/render behavior. Headless/build
  success does not prove responsiveness, pixels or pacing. Report unrun checks.
  Use disposable save/capture paths. Final performance gates run serially.
- Use a development branch; commit/push coherent stages regularly. Incomplete work
  is a checkpoint, never a validated release. Review changed inventory and secrets.
  Never overwrite main, old tags/archives/hashes as a checkpoint shortcut.
- At integration update README/changelog and live version/date metadata, including
  Windows resources. Keep player packages separate from source/evidence; exclude
  caches, personal saves, nested clones and oversized raw executables.
- Final report: actual changes, completed checks, limitations and branch/commit/PR.
  Never claim saved/published work without successful remote verification.

## Read on demand
- src/Core/AGENTS.md — simulation and saves.
- src/Presentation/AGENTS.md — commands, camera, animation, input.
- docs/CODEMAP.md — focused file lookup.
- docs/DEVELOPMENT-STATE.md — current verified state and next step.
- docs/ARCHITECTURE.md — relevant architecture sections only.
- docs/history/AGENTS-before-v0209x1.md — full prior guide, preserved verbatim.
  Its old scope/status/staging text is historical, subordinate to this guide.
