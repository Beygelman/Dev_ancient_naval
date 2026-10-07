# v020.8b — 7 October 2026

## Source and scope

Continued the canonical `C:/__Beygelman/! -11/dev-ancient-naval` checkout on
`main`, starting at `9cb4dad95f558130b2d74e9fd63039683cf8d9fe` (remote matched).
No source/data merge markers were found. Preserved the user's audit whitespace
edit, imported nested `docs/dev-ancient-naval` project and all historical builds.
The editor's reordered release-date key is retained in place with the new date.

This implements the explicit v020.8b request, not earlier brainstormed national
combat abilities. Core remains Godot-independent; staged damage/save contracts,
RNG streams, radar anonymity and retained terrain/LOD budgets are unchanged.

## Changes

- Retained explored-perimeter waterfall mesh against a black infinite void.
  Animation uses shader time. Interior discoveries/pan/hover never upload it.
- A 23%-shorter-side guide circle with outward-fading light. Any eligible target
  outside it has an arrow, whether onscreen or offscreen. No rival eligibility
  or uncommitted damage enters the guidance list.
- Enlarged bottom-edge nation relic: upper monument ends the turn; inset count
  cycles useful owned actions. Hints-on confirmation remains the only action
  list. Cancelling a pending end-turn transition restores active light.
- Owned town/flagship shrine flares occur once per committed opponent turn
  serial; Continue does not replay an existing turn. Green/White/Purple effects
  refreshed; Purple rebuilt as an engraved silver stepped reliquary.
- All paper control states use dark ink. Native hovered interface controls guard
  wheel zoom even after scroll endpoint propagation; clipped paper surfaces
  explicitly consume scroll events. Advice starts hidden, rejects empty topics
  and uses a smaller 330px sheet with 15/11px heading/body fonts.
- Primary counsel facts precede secondary lookout/crew details. Mothership
  second-attack upgrade triggers double-salvo advice before Kolonel unlock.
  Shared ship glyphs use open hulls and readable mast/sail silhouettes; all six
  nation names and revised advice have EN/UK/NL display translations.
- Captain/Admiral retain coordinated volley/assembly planners and invest in
  paired ports, beacon chains, batteries, research and development reserves.
  Support builders advance incomplete chains. Threatened low-health flagships
  anticipate enemy movement, seek friendly bases and use legal escort screens.
  Fatal counterfire defeat now safely advances the active captain's turn.
- New voyages use Granado movement 3 and four cautious pirate opening rounds.
  Pirates still defend against armed intruders. Historical missing
  `PirateCautiousRounds` defaults to 0; saved movement catalogs retain old values.
- Root compilation excludes unrelated docs/release C# instead of deleting
  imported projects. Export metadata and explicit release-tool version choices
  accept v020.8b while historical defaults/artifacts remain intact.

## Verification

Evidence is written only to disposable paths under
`C:/Users/User/Documents/Codex/2026-09-29/x20/outputs/v0208b`.

- Debug build: 0 warnings/errors.
- Full final Core suite: exit 0; 1,201,675 combat/progression assertions, eight AI
  matches, 23 real historical-save checks, 23 new v020.8b checks; retained
  generation/economy/tactical/pirate suites passed.
- Native initial/final suites passed: guidance 47; navigation 229; tutorial 287;
  setup 366; command menu 184; counsel 45; smoke 248,660; battle 175; effects 121;
  sea 288; optimization 11; new UI 194; identity 7; new world 11; merchant 30,740;
  scuttle 27. All runs use the actual Compatibility renderer, not headless.
- Captures inspected: chart lip/void, silver/green/white shrine variants,
  open-outline ship gallery, Ukrainian nation picker and selected-object counsel.
- The first navigation run failed an obsolete viewport-only expectation;
  updated it to the newly requested circle criterion and reran successfully.

The new UI suite also tests opening a menu during the relic fade: the turn and
simulation bytes remain unchanged, and the completed stamp/fold transition
restores the active relic. Retained hint/action suites passed 56/74 checks.
No test result is inferred from compilation.

## Known inherited limitation

`AdmiralThreats.Positions.Passable` and `NavalNavigationQuery.Passable` consult
the live forbidden-cell set for unseen whirlpools. They do not reveal enemy
class/HP/identity, but can influence route/threat estimates through fog. The
v020.8b investment/escort changes do not introduce this behavior. A focused
navigation information-boundary audit is recommended before changing this
shared contract; it was not silently redesigned during this update.

## Final native pacing

After the final Debug build and native checks completed, the user closed World
of Tanks. The unchanged release gate ran serially on Godot 4.7.2 Compatibility /
RTX 4060 Laptop, with no concurrent compilation/Core tests:

- Pangaea, four rivals, Ocean size / wide view: pan p95 17.120 ms; interaction
  frame p95 17.380 ms, max 53.234 ms; route hover p95 0.061 ms, max 0.434 ms.
- Oceans, four rivals, Ocean size / live fog: pan p95 17.251 ms; interaction
  frame p95 17.987 ms, max 64.123 ms; route hover p95 0.050 ms, max 0.439 ms.
- Both native gates exited 0; the 25 ms p95 thresholds were not relaxed. Raw
  reports are `outputs/v0208b/performance-final/performance-*.txt` and logs.

These measurements describe this Windows/GPU setup, not every device. No Android
or touch-device frame measurement was performed. One incorrectly hyphenated
test invocation launched the normal title screen and timed out; the correctly
named guidance/navigation tests were rerun and passed 47/229 checks.

## Delivery

Game source commit: `bc28085f986c2e8022ae02c3af9fd6f307473718`, pushed to `main`.
The source archive contains that tracked snapshot plus three reviewed Godot
script UID sidecars produced during import. Those same stable UIDs are copied
back into the canonical tree. The later delivery commit adds archives/links;
it changes no gameplay or compiled presentation.

`releases/v020.8b` contains the Windows/source ZIPs and SHA256 manifest, mirrored
in the external `outputs` directory. The player has 190 files: executable, PCK,
minimal player README and the complete required runtime. No PDBs, developer
commands, diagnostic documentation, test saves or imported nested project ship.
The raw export already excluded those items, so the post-export removal list is
empty. Player PCK inventory: 198 resources. Source archive: 1,171 entries.

The actual raw export and independently ZIP-extracted player passed English,
Ukrainian, Dutch and map-preview startup (eight rendered frames each, exit 0).
Startup captures do not substitute for the serial source frame measurements.
No real user save or valid backup was used in any verification.
