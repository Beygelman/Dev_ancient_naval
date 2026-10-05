# v020.7b — working source checkpoint, 6 October 2026

This is the user's requested source update, not a new Windows player release.
The canonical source remains `C:/__Beygelman/! -11/dev-ancient-naval`.

## Source selection and preservation

The primary contained intentional later development, rather than just the frozen
v020.7 release. Its reviewed local snapshot is `9ccfa57` (`v020.7b - Round for UI`),
parent `299d99c`; the public main was still `299d99c`. Later source was preserved
and amended for the latest explicit v020.7b request. No reset, forced checkout,
version-wide replacement, enum renaming or save migration was used.

Local `9ccfa57` also contains a duplicated extracted corrected player candidate.
Its raw executable is 109,413,888 bytes, over GitHub's regular-file limit. A clean
publication snapshot must omit that extracted directory from Git ancestry while
retaining it on disk, all historical ZIPs and the original local commit. This is
a source publication repair; runtime DLLs inside historical packages are intact.
New documentation-only import sidecars, caches, transient saves and build outputs
are excluded. Previously tracked historical sidecars, normal game asset imports
and source UIDs are retained.

## Delivered scope

- Checked-in v020.7b/date signatures on the title and session menus; faster WASD.
- Bounded close-view terrain detail, sharp black town labels, safe queued fog
  changes and one far object LOD with depth/observation invariants retained.
- World-anchored wrapping command papers, readable prices, current-page 1–9,
  larger grouped nation counsel, no redundant Info button/popup.
- Nation sanctuary ready counter, hints-off visibility, click-to-cycle useful
  objects, and action mini-glyphs in hints-on end-turn confirmation only.
- New voyages: Granado movement 4; built Cannon Tower damage 5 through town walls.
  Shared damage calculation includes actual combat, forecasts and lethal markers.
- Always-confirm scuttling, dry-brush sinking hull glyph, exact actual-price
  refund, modal isolation and safely canceled stale dialogs.
- Saved-rule-aware flagship, town, veteran and keyboard advice, fixed footer,
  native screenshots and a revised double-salvo image before firing.
- Exact world-anchored claim/treasury/salvo papers; separate blue/gold/red
  offscreen directions on a circle with radius 30% of the shorter viewport side.
  Directions move the camera only. Red warnings observe committed own health,
  never prepared future impacts or hidden enemy objects. They clear on review,
  expire after the following round and reset on world replacement.
- Bottom-center Mothership compass with independent hit space even in compact
  windows, and finite nation-styled owned shrine flares once at a stable human
  turn start. Continue does not replay that round's ceremony. Hidden/offscreen
  effects stop processing.
- Merchant art, rounded varied clouds, mountain chains and silver pyramid detail
  preserve cosmetic RNG independence and existing traffic timing/navigation.

Internal serialized class identities, embedded historical catalogs, geometry,
RNG, statistics, factions, selected port cells and tutorial v1 history remain.
The optional tower wall policy defaults false when absent. Existing connected
port income, relayed merchant routes and staged exact-once combat remain intact.

## Verification record

Executed on the native Godot 4.7.2 .NET Compatibility renderer, with disposable
external save/settings paths. Builds so far report zero warnings/errors.

- Full Core run: 1,535 baseline checks; 25 new tower/Granado/staging/historical
  policy checks; 1,199,448 Thor/progression/combat assertions; eight complete AI
  matches; 23 historical save/validation checks and all remaining world, economy,
  navigation, pirate, fleet and progression suites. Exit 0.
- Native renderer: 21 checks, including close sampling, retained camera behavior,
  queued fog suppression/generation and unchanged Core state.
- Native ready guidance: 47; command menus: 184; object counsel: 45. Numeric and
  pointer commands remain disabled until the physical reveal finishes.
- Native scuttle: 27; current tutorials: 287; retained tutorials: 59; controlled
  screenshot capture: 19. Input/EN/UK/NL, scroll boundaries and UI scale extremes
  were exercised. Screenshots were inspected, not just generated.
- Final same-camera Pangaea full-chart submission comparison: 6,032.6 to 808.5
  draw calls, approximately 86.6% fewer. This is separate from frame pacing.

Native navigation: 215, including 800×520 at 125% UI scale, portrait layout,
real arrow/compass clicks, nearby guide separation, world anchors, committed
damage warnings, Continue and finite owned ceremonies. Screenshots inspected.

Final broad native regressions passed: smoke 248,660; historical/current battle
175; effects 121; sea discoveries 288; retained rendering 11; merchant traffic
30,740; port economy UI 224; action-story/exact-once gates 48. Legacy UI fixtures
now wait for physical folds and acknowledge real reward papers, while historical
combat assertions remain. Restart verifies the current saved pirate policy.

### Native frame pacing — final Debug assembly

Two consecutive serial gate runs passed on the RTX 4060 Laptop, NVIDIA 581.29,
native Godot 4.7.2 .NET Compatibility renderer. Both cases have 1,759 cells and
four rivals. No compilation or other game tests ran during frame measurements.

| Run / case | Pan p95 / max ms | Interaction p95 / max ms | Hover max ms |
| --- | --- | --- | --- |
| Final / Pangaea full chart | 17.028 / 32.211 | 17.145 / 54.316 | 0.440 |
| Final / Oceans live fog | 17.305 / 21.913 | 18.344 / 64.648 | 0.451 |
| Confirm / Pangaea full chart | 16.968 / 29.554 | 17.315 / 53.370 | 0.429 |
| Confirm / Oceans live fog | 17.187 / 33.010 | 17.150 / 71.918 | 0.462 |

Thresholds are unchanged: pan p95 <=25 ms/max <=80 ms; interaction p95 <=25 ms/
max <=100 ms; hover <=16 ms. Before the fix, live fog failed with 120.824 and
102.701 ms movement frames. There were no accompanying GC or shader compilations,
and managed command/cache scopes were small. Native retained-source updates were
being queued across all changed regions in one frame. The fix records masks and
hides unsafe old images immediately, but prepares native source changes only for
the scheduled flight. Prepared generation tokens prevent an in-flight revision
from masquerading as completed paint. The 21-check native raster/fog suite passed
again after this change. Optional profiling remains disabled in ordinary play.

Evidence is retained in `docs/diagnostics/v020.7b-working/`; failed pre-fix frame
reports are identified separately from passing final/confirmation reports.
No real user save or valid user backup was used for testing. Historical save
fixtures and controlled save/Continue paths passed; a new Windows export, Android
device/touch run, other GPU/renderer and full manual human campaign were not run.

### Git publication

Publish a reviewed snapshot directly above remote `299d99c`, with no oversized
`9ccfa57` ancestor. Preserve `9ccfa57` under local
`backup/v0207b-original-20261006`, and the complete final local source under
`backup/v0207b-local-complete-20261006`. These backup branches must remain local.
Compare the full source manifest and Git-cleaned staged blobs, retain all 32
historical ZIPs byte for byte, then fast-forward public `main`. Canonical main/index
alignment uses an expected-old ref check and no working-file checkout or reset.

## Historical archive integrity

Untouched archive hashes (SHA256):

| Archive | SHA256 |
| --- | --- |
| Finalized v020.7 Windows PLAYER_CLEAN | `53d27c7e8c1db12b4a915e7b7ed09def17bb03fd830f22e7e25df552d0fc1f60` |
| Finalized v020.7 Source FINALIZED | `7f53dce935e7823272269cf3b0a9cb195e9324fed6f4183e117337738f126b7e` |
| Corrected 2026-10-05 Windows candidate | `97dd1ec07f9c9ddc80ceabf2a77ba29b5f05123302b50f13c0a6f181548d2b9d` |
| Corrected 2026-10-05 source candidate | `538d4f5a94d44b4900d5d91ac88df7e6085e648d240bed94c4946665bfb34d5d` |

No v020.7b Windows export, source ZIP or installer was requested or generated.
Historical ZIPs do not display these latest source changes. Use the current
Godot source project for this checkpoint.
