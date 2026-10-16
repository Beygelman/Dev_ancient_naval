# Development state — 16 October 2026

## Current delivery
Version: v020.9x1 (user-facing v20.9x1), experimental.
Validated functional source: c7bbd6963b0d6d7aa8bb7f2ebed92451584aa23f.
Delivery: PR #7, branch codex/ancient-naval-v020.9x1; final handoff changes are docs only.
Original base: main 0c78297469d6d278334ea1c59d43fc4912677548 (v020.8b).
Historical archives and fixtures were preserved.

## Completed
- Root AGENTS.md reduced from 51 KB to 5 KB; Core/Presentation guides and CODEMAP.
  Original guide preserved byte-for-byte (blob 4d6f510f34e770443a845735b0e8e1c8e6deaafb).
- Permanent own-order commit/animation separation, safe visual settling on next
  order/world replacement, shorter sinking and opponent-only automatic action focus.
- Saved experiments: interleaved double salvo for ships/towns, +2 ordinary connected
  port income ceiling, city facilities at levels 2/4 and one level-5 improvement.
- Typed save/default/validation support; actual income and receipts share the cap;
  dock service consumes ship actions and city work; arsenal loss preserves hulls.
- Legal AI facility investment and Admiral forecasts for the middle reply.
- EN/UK/NL native city sheet; version/date and live Windows/iOS metadata updated.
- Gated CI and portable Windows package, preserving historical packages.

## Verified evidence
[Actions run 38893737716](https://github.com/Beygelman/Dev_ancient_naval/actions/runs/38893737716):
- Full Core suite passed, including historical fixtures and 59 new checks.
- Godot 4.7.2 .NET build passed.
- 43 graphical action/city checks passed, including actual native EN/UK/NL clicks.
- 100 existing effects checks passed; screenshots inspected.
- Windows export and actual exported executable headless startup passed.
- ZIP inventory/checksum verified: 190 members, no PDB/docs/tests/tools in outer package.

Windows: Ancient_Naval_v020.9x1_Windows.zip (87,373,825 bytes).
SHA256: 762d2f697cfcd9a6695ffa34ef04a3c9af41b60731883f6571063a6c7fecc82f
[Build artifact](https://github.com/Beygelman/Dev_ancient_naval/actions/runs/38893737716/artifacts/12042986760).
The reproducible build script is tools/Build-ExperimentalWindows.ps1.

## Compatibility and remaining playtests
New voyages enable experiments; Continue preserves embedded historical rules.
Permanent camera/animation corrections also apply to old saves.
Prototype tuning is documented in V0209X1.md. Compare combat and trade switches
separately before drawing balance conclusions; test open sea, choke points and
split fronts. Physical Android/iPhone validation and a final native performance
gate were not performed. Headless Windows startup is not a graphical playthrough.

Deferred: Galleon move-fire-move, movement-based Kolonel nerf, Restoration changes,
production queues, caravans, supplies and national mechanics.

## Session handoff
Direct clone/archive access timed out on this host. Only needed source was read
via the connector; reviewed changed paths were checkpointed remotely. No local
SDK/engine migration was made; prepared CI supplied the pinned tools. In a new
environment, inspect the current checkout and installed tools first. Do not repeat
the recovery or load the archived guide/chat history without a specific need.

