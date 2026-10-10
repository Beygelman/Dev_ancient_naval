# Ancient Naval — AI development guide

**Guide revision:** 4.2 · working notes 7 October 2026<br>
**Frozen delivery baseline:** `v020.7 — The Sacred Voyage`<br>
**Engine:** Godot 4.7.2 .NET<br>
**Code:** C# / .NET 8<br>
**Primary platform during development:** Windows<br>
**Long-term release target:** touch-first Android as well as Windows, without migrating away from Godot .NET/C#<br>
**Canonical local project:** `C:/__Beygelman/! -11/dev-ancient-naval`<br>
**Repository:** `Beygelman/Dev_ancient_naval`

This file is the operational contract for an AI agent continuing Ancient Naval. Read it before modifying source, data, saves, release scripts or UI.

The user's newest explicit request always determines scope. Historical notes are evidence and design context, not permission to implement every old idea.

## Current working scope — v020.8b, 7 October 2026

The latest request explicitly advances the working identity to **v020.8b**.
It supersedes the earlier navigation/shrine/end-turn presentation below: the
guide circle is now 23% of the shorter physical viewport side and eligible
targets appear whenever outside that circle, including onscreen targets.
Owned shrines flare once per committed opponent turn serial, not human round.
The enlarged bottom-edge nation relic ends the turn; its inset numeral cycles
Core-ready owned objects. Only hints-on confirmation shows their action list.
New voyages have Granado movement 3 and `PirateCautiousRounds=4`; missing pirate
policy defaults to 0, preserving historical patrol behavior and embedded catalogs.
Captain/Admiral infrastructure, relay and escort policies use observed danger,
remembered terrain and the normal Core command facade; Boatswain remains simple.

`WorldEdgeFalls` retains one fog-scoped perimeter mesh behind the world. Shader
time alone animates flow; pan/hover and interior exploration must not reupload
the perimeter. Black clear color belongs to project settings, not simulation.
All paper button states, including focused/hover-pressed, inherit dark ink.
`MapInput.PointerOverInterface` is a final native-hover guard for wheel events
that bubble at scroll endpoints; consuming reveal surfaces remain required.
Advice cannot reveal a topic with empty heading/body; it starts hidden and uses
a smaller bounded sheet. Mothership second-attack upgrades trigger the existing
double-salvo topic without requiring a Kolonel. Nation names are localized
display strings; faction enum and saved identity values never change.

MSBuild excludes docs/release C# sources to isolate the user's nested imported
project under docs without deleting or changing it. Review source inventory
before export; do not package that unrelated clone. Keep v020.7b archives intact.

## Previous working scope — v020.7b, 6 October 2026

The latest explicit request changes the primary working identity to **v020.7b**.
It authorizes render detail/one distant LOD, world-anchored wrapping command paper,
numbered current-menu shortcuts, faction monument ready counter, current counsel
with stat glyphs, Granado movement 4 and built Cannon Tower damage 5 with optional
wall bypass. Preserve the primary's intentional later source and all historical
packages. The subsequent explicit request on 6 October authorizes a portable
Windows player and matching source/SHA256 archives for this same v020.7b identity.

Both menu signatures read `application/config/version` and
`application/config/release_date` from project.godot; never use runtime today's
date or the old hardcoded v0.13 footer. Update both checked-in identity fields for
future issues. Windows resource metadata must match the working identity too.

Scuttle always opens a native confirmation, independent of hints. Show the Core
actual-price refund; commit only after acceptance and revalidation of the original
battle/ship. A world replacement cancels the awaiter. Keep footer actions outside
the scrolling narrative. Tutorial topic history remains the bounded v1 sidecar;
screenshots are native controlled fixtures, never a user's real voyage/save.
Nonmodal paper still owns wheel input across its entire footprint, including its
footer and scroll endpoints: `MouseFilter.Stop` alone is insufficient because
Godot defaults `MouseForcePassScrollEvents` to true. A clipped reveal containing a
Node2D also needs an explicit consuming content surface.

Close terrain detail is a bounded 64-chunk retained cache (520×520 padded pixels
per chunk, at most 17,305,600 RGBA pixels), separate from the coarse atlas. Fog
downgrades suppress stale images immediately, and GPU completion must match the
latest requested generation before restoring them. One far object tier uses
hysteresis (<0.44 enter, >=0.52 leave); camera motion never rebuilds static
artwork every frame. Sharp town text samples at 3× and retains logical size.
Fog invalidation updates desired masks/tokens and hides unsafe old images
immediately. Defer native source visibility/tint/redraws to the scheduled region
or chunk flight; updating every queued source at once caused >100 ms movement
frames despite small managed CPU scopes. Draw callbacks stamp prepared tokens,
never the latest requested token; reentrant revisions must rearm before exposure.
Optional performance scopes distinguish cache preparation from deferred rendering.

`CannonTowersIgnoreWalls` defaults false when absent. New voyages enable it;
historical embedded catalogs/policies are not changed on Continue. Core
`VillageShotDamage` drives the actual salvo, safe-action query and Admiral town
forecast. Built Cannon Towers alone bypass town walls; ancient mortar towers and
other hulls retain their existing rules.

The ready counter click cycles owned Core-ready objects, never opens a separate
list. Only hints-on end-turn confirmation contains the object/action pictograms.
Hints-off still displays the nation counter. Repairs/radar/ports are intentionally
excluded by the existing Core ReadyActions useful-action policy; do not silently
change that contract while editing its cosmetics.

Info sectors/popups have been explicitly removed by this request. Current object
counsel opens on selection. Command paper follows its world anchor even offscreen;
do not clamp it to the viewport and detach it from its object. Numeric 1–9 keys
refer only to the currently visible submenu sectors, after native left-to-right
placement. Visible unavailable sectors reserve their number but cannot execute;
hidden sectors reserve none. Never fire behind a modal or unfinished reveal.

Stage 3 keeps selected-object, claim, treasury and salvo papers at their exact
world anchors without viewport clamping. `OffscreenNavigationHud` uses the
physical viewport for offscreen tests and a centered circle with radius 30% of
its shorter side, independent of UI scale. Blue markers denote eligible claims,
gold eligible treasuries and red recent damage to owned towns/Mothership only.
Marker and bottom-center compass clicks move the camera without Core commands;
narrow counsel layouts must reserve the compass space. Guide placement reserves
the compact compass's native hit circle without changing the 30% radius or its
true bearing. Clamp intervals must tolerate float-rounding at that circle's seam.
Red alerts compare owned
committed HP only while `Battle.PendingPresentation` is null; never inspect the
prepared future impact. Clicking dismisses an alert, it lasts through the
following round at most, and replacing the world clears the cache.

Owned town/Mothership shrines pulse once per stable human round for 1.1 seconds
at 30 Hz. Continue marks its already-active human round as presented rather than
replaying the flare. Repeated refreshes or camera moves cannot restart it. Hidden
or offscreen pulses stop frame processing and expire; they never rebuild static
art, alter Core/save state or consume simulation RNG.

The user explicitly authorizes future reviewed commits and pushes to GitHub
`main`, with README updated and an informative description for each update.
Fetch and compare first; never force-push, reset newer local work or include
unrelated user edits blindly. Preserve existing local commits and inspect the
staged source inventory. This authorization supersedes older read-only Git
finalization notes below. The v020.7b player is now explicitly requested.

Export/package/verify scripts accept `-Version v020.7b`; their default remains
the frozen `v020.7` to preserve historical naming. Use a reviewed external source
snapshot when a local editor has imported a nested project under docs: default
MSBuild globs otherwise compile that unrelated clone too. Do not delete the clone
or overwrite user edits. Player checks must run the actual exported executable;
runtime regression hooks require the debug feature and do not run in a Release
player merely because `--menu-test` is passed. Keep all automated player saves,
preferences and movie captures under an explicit disposable outside-project path.

The preserved local checkpoint `9ccfa57` contains a 109,413,888-byte raw executable
over GitHub's regular-file limit. Keep that commit under a local backup ref and
retain its on-disk files. Publish reviewed clean source commits based on remote
`299d99c`, excluding only the newly extracted
`releases/v020.7/corrected-2026-10-05/Ancient_Naval_v020.7_Windows_PLAYER_CANDIDATE/`
folder. Never rewrite historical ZIPs/hashes or delete backups to make a push pass.

## Explicitly requested same-version correction — 5 October 2026


The user now authorizes a corrected **v020.7** artifact: reference-shaped clay
vessels, distinct floating-city Mothership glyph, real right-edge Info sectors,
fold/burn action papers and centered captions, foreground town labels, current
information, saved pirate toggle, menu translations and persistent merchant
traffic through beacon chains. This is a narrow amendment, not permission to
replace v020.7 with the entire later v020.8 tree. Preserve the primary newer
working source, frozen tag and historical archives.

The subsequent explicit economy amendment applies to **new voyages**:
`Ports.ConnectedCityIncome=true`, `Ports.Income=0`; a port earns one Thor per
other friendly living port city in its connected sea-lane component. Beacon
relays are not cities and duplicate routes never multiply income. The optional
field defaults to **false** when absent, preserving the embedded fixed-income
rules and stored source amounts of historical saves. Connected port income is
derived in Core from cached immutable `TradeNetwork` components at query/credit
time; never bake the result into saved income sources or reroll it on Continue.
Cache lifetime follows the `TradeRoutes` instance; construction, capture,
defeat, hazards and staged state restoration must invalidate topology first.
Do not expose enemy connection counts or derived income through optical city
information: own cities show actual income; foreign city trade is unobserved.

New hull prices are Support Brig 5, Brig 6, Galleon 9, Kolonel 16, Granado 22;
radar equipment costs 4 and Mothership mortar costs 14. Historical price fixture
files remain unchanged. Currency, receipt, lifetime-statistic and dismantling
checks must use the applicable saved rules and actual purchase price.

`PiratesEnabled` is an optional save field with **true** as the missing-field
historical default. Disabled new voyages have no pirate patrols or settlements;
they do not silently enable later difficulty-based pirate populations. Continue
must restore generated objects, RNG, rules and the saved choice without rerolling.

Merchant destination endpoints are cities with ports. A lighthouse is a normal
intermediate lane node, even when the maximum length of an individual Core link
is six; that length is not a limit on a cosmetic multi-link merchant voyage.
Cosmetic departures remain every 12–20 seconds and may not consume simulation
RNG. Route cache replacement/construction must preserve active boats and their
port schedules; hidden boats reveal no unknown information.

Corrected release tooling uses `Package-FinalizedRelease.ps1 -Corrected`, a fresh
output directory, and separate corrected filenames/manifests. Never overwrite
the prior finalized archive or run Sync-Release as a shortcut.


---

# 1. Source of truth and startup protocol

## 1.1 Canonical source

Prefer, in order:

1. the user's canonical local working project:
   `C:/__Beygelman/! -11/dev-ancient-naval`;
2. a verified branch/commit representing the requested version;
3. a verified source release ZIP;
4. never the compiled Windows player ZIP as a development source.

For the v020.7 checkpoint, GitHub PR #6 records:

```text
branch: codex/ancient-naval-v020.7
head:   b03c5bdc85498f5f5d76f03f4d070adcaa13aa34
PR:     https://github.com/Beygelman/Dev_ancient_naval/pull/6
```

At the time this guide was prepared, repository `main` visibly contained unresolved conflict markers and stale version text in important files. **Do not begin with a blind pull/reset from main.** Inspect first.

## 1.2 First commands in any development session

From the repository root:

```powershell
git status --short
git branch --show-current
git log --oneline --decorate -10
git remote -v
```

Then inspect:

```text
AGENTS.md
README.md
CHANGELOG.md
project.godot
export_presets.cfg
data/balance.json
docs/ARCHITECTURE.md
relevant source subsystem
relevant tests
```

Before editing, search for conflict markers:

```powershell
rg -n '^(<<<<<<<|=======|>>>>>>>)' . -g '!releases/**' -g '!.godot/**' -g '!**/bin/**' -g '!**/obj/**'
```

If conflict markers are present, stop treating that tree as a clean baseline. Preserve user work and recover from a verified branch/checkpoint.

## 1.3 Never destroy user state to make development easier

Never overwrite, delete or silently reset:

- real user save slots;
- historical release archives;
- unrelated working-tree edits;
- the user's primary Git index/branch just to run an experiment;
- signing credentials or machine-local settings.

Use explicit disposable save/settings paths in automated tests.

---

# 2. Project identity and non-negotiable technology

- Ancient Naval is a turn-based ancient naval strategy/tactics game with exploration, fog of war, economy, settlement interaction, multiple rival fleets and persistent voyage state.
- Keep **Godot 4.7.2 .NET + C# / .NET 8** unless the user explicitly authorizes an engine/runtime migration.
- Do **not** migrate gameplay to GDScript as a convenience refactor.
- Windows is the current development/export environment; touch-first/Android compatibility remains a design constraint.
- Preserve landscape/touch usability: mouse-only solutions are insufficient for interactions intended to ship to mobile.

---

# 3. Preserve the playable game

- Refactors must preserve rules, seeded outcomes, visuals and existing saves unless the user requested a behavior change.
- State every intentional gameplay/compatibility change explicitly.
- Historical save behavior is a contract, not test clutter.
- Internal serialized enum/class names may differ from display names. Do not rename serialized identities merely to match UI labels without a migration.
- Known legacy examples include internal identities such as `Invader`, `Togus`, `Garrison` while player-facing names have evolved to Galleon, Granado, Brig/Support Brig.
- Faction enum identities and colors persist through saves.
- World generation must remain deterministic for the same saved rules/seeds unless the requested feature explicitly changes generation.
- Cosmetic randomness must not consume simulation randomness.

---

# 4. Architectural boundaries

## 4.1 Dependency direction

```text
Godot scene / input / HUD
          | commands                  ^ results / snapshots
          v                           |
      BattleState (Core, no Godot reference)
       |      |       |       |
   Navigation Combat Turns Production
       |              |
   PathSearch     Rules / Units / World / Vision

SaveStore (Presentation disk adapter)
       -> session envelope -> Core save DTOs -> validated BattleState
```

## 4.2 `src/Core`

`src/Core` is pure .NET.

It must not depend on:

- Godot nodes;
- rendering APIs;
- scene paths;
- UI state;
- machine-local file paths.

Core owns:

- rules;
- mutable battle state through the command facade;
- turns;
- movement/navigation policy;
- combat and counterattacks;
- production/economy;
- villages/structures;
- fog/vision/radar model;
- world/gameplay geometry;
- deterministic generation state;
- save DTO data and compatibility rules.

## 4.3 `BattleState`

`BattleState` is the primary gameplay command facade and owner of mutable battle state.

Partial files may split cohesive rule areas, but do not turn `BattleState` into a dumping ground. Independently reusable algorithms belong in separate classes.

No presentation callback should mutate underlying fields behind the command facade.

## 4.4 `src/Presentation`

Presentation:

- translates player input into Core commands;
- stages commands where physical timing matters;
- renders current/observed state;
- owns Godot UI/animation;
- performs disk I/O through adapters;
- must never become a second source of gameplay truth.

If an animation is skipped, accelerated, hidden or run headless, gameplay outcome must remain correct.

## 4.5 `Main`

`Main` composes the game and routes major concerns through dedicated files/modules, e.g. selection, commands, view refresh, saving/session lifecycle.

Avoid adding business logic directly to HUD button handlers.

---

# 5. Commands, staging and exact-once effects

One of the most important project lessons is the separation between **simulation result** and **presentation timing**.

## 5.1 Damage belongs to Core exactly once

Visual shell count, projectile timing or animation duration may not change damage.

For staged projectile actions:

1. prepare the deterministic Core command/result;
2. animate the appropriate projectile/salvo;
3. commit the corresponding Core impact phase at the physical impact boundary;
4. animate follow-up/counter phase separately;
5. finish the pending order;
6. only then permit save/next command where required.

Never call Core damage again because the visual effect looked incomplete.

## 5.2 Counterattacks

- counterattacks never recurse;
- one counter phase is distinct from the initiating attack;
- counterattack must not be counted as a new active attack for repair/action-lock rules unless explicitly designed otherwise;
- presentation must not cause a second reply.

## 5.3 Destruction sequencing

A destroyed flagship/Mothership and fleet-collapse presentation has explicit ordering. Do not remove followers before required wreck/submersion presentation or leak hidden wrecks as spectator information.

## 5.4 Ready rewards and claims

Pictorial claim/treasury/reward ceremonies commit at a defined boundary. Revalidate the original battle/target before the commit. Prevent double reward under interrupted/replayed animation.

Any async open/fold/burn acknowledgement must resolve safely if replaced or interrupted.

---

# 6. Navigation and movement

## 6.1 Pathfinding boundary

`Core/Navigation/PathSearch` should know positions, neighbors and costs — not Godot or visual fog.

`NavalNavigationQuery` supplies the current policy for one synchronous search. Do not cache it across:

- movement;
- commands;
- turn changes;
- topology changes;
- relevant visibility revision changes.

An immutable movement preview may be reused only while its identity keys remain valid: battle, selected ship, position, remaining budget and vision revision.

## 6.2 One calculation everywhere

Player preview, actual movement and AI must apply consistent:

- terrain costs;
- friendly transit;
- hostile occupancy;
- threat penalties;
- diagonal/corner rules;
- coast/shallow restrictions;
- trade-lane modifiers where enabled.

Never “fix” the UI path independently from Core legality.

## 6.3 Trade routes

Trade routes depend on active owned ports/hazards and may include stateful run history. If speed depends on the continuous run, the run is part of route-search state and must survive save/resume of partial movement when the rule requires it.

Preview routes retain their original origin. Clear route topology after aggregate restore when required.

---

# 7. Fog, exploration, radar and information security

Fog is a gameplay boundary, not a shader.

## 7.1 Unknown means unknown

- movement previews must not use hidden terrain as if the player knows it when the rule says unknown;
- player overlays cannot leak hidden ships through path blocking, labels or mouse behavior;
- hidden enemies must not enter shared-tile selection cycling.

## 7.2 Radar

Radar may provide a coordinate/contact without full optical information.

A radar-only target must not leak:

- exact hull identity/class if not allowed;
- faction/nation when not observed;
- HP;
- damage digits;
- “sunk X” identity through destruction animation;
- encounter rewards intended for first optical contact.

Radar attacks use shared known coordinates while preserving anonymity.

## 7.3 God's eye

God's eye is a **human display override** only. It must not:

- contaminate real explored memory;
- change AI observation;
- generate first-contact rewards;
- change deterministic world/simulation state.

Victory may reveal the chart for presentation, but that is not retroactive exploration state.

## 7.4 Terrain/vision

`TerrainFeatures` is the deterministic source for mountains/forest/coast-depth features where used. Rendering cannot invent extra optical blockers. Optical rays may respect actual mountain mesh geometry; radar intentionally follows its own policy.

---

# 8. Saves and backward compatibility

Save DTOs are an explicit compatibility contract.

## 8.1 New fields

For a new optional rule/setting:

1. add a typed field;
2. define a historical default;
3. include it in the saved rules/settings snapshot;
4. validate it during load;
5. test missing-field behavior using real historical fixtures;
6. do not regenerate an old world merely because a new field is absent.

## 8.2 Historical behavior

New-voyage rules can differ. Continue preserves saved behavior.

A common pattern in this project:

```text
missing new optional field -> historical behavior
new voyage -> current behavior
existing save -> saved embedded rule
```

## 8.3 RNG

Random state and rewards cannot reroll after save/load.

New simulation randomness must either be deterministically derivable or serialized in the state needed to resume exactly.

Cosmetic random draws use an independent source and cannot alter simulation random order.

## 8.4 Recovery

- malformed nested save data must be rejected before indexing;
- a corrupt primary may fall back to a valid backup;
- saving after recovery must not overwrite the good backup with the known corrupt primary;
- starting a new voyage atomically replaces the slot and removes prior backup only after successful write;
- tests must use disposable save paths, never real user slots.

---

# 9. Data and balance

Configurable rules belong in typed rules/data, primarily `data/balance.json`, rather than scattered UI literals.

When a number becomes configurable:

- update Core rule access;
- update validation;
- update saved rule snapshot/default;
- update player-facing tooltip/info text;
- update tests;
- remove contradictory hardcoded UI values.

Do not add a generic framework for a value with one concrete caller.

Difficulty should change decision policy rather than secretly buffing combat stats or income unless the user explicitly requests a numeric difficulty rule. Later work allowed difficulty to influence initial pirate composition; that is world setup, not a hidden stat multiplier.

---

# 10. v020.7 frozen rules

While maintaining/finalizing v020.7, preserve these values/behaviors.

## 10.1 Weighted capacity

```text
Mothership    0
Brig          1
Support Brig  1
Galleon       2
Granado       3
Kolonel       4
Balloon       0
Structures    0
```

Use the shared fleet-slot calculation in production, ready actions and UI. Do not duplicate slot-cost tables in presentation.

## 10.2 Dismantling

- refund 40% of **actual paid price**;
- round to whole Thor;
- paid construction receipt persists;
- starting/gift/free/creative hull = zero receipt/refund;
- older missing receipts use the documented compatibility fallback;
- command can pay once only.

## 10.3 Diagonal berths/ports

- town shipyards may use legal diagonal shores;
- occupancy, rim and saved-rule checks still apply;
- `PortCell` persists the selected sea berth;
- port preview selects the least-cost navigable route to the nearest owned port/beacon;
- later world/network changes cannot silently relocate that saved port.

## 10.4 Balloon crash

- separate staged sea-impact boundary;
- 2 damage to underlying cell + eight closest neighbors;
- friendlies may be hit;
- impact occurs once;
- hidden origin/victims must not leak through presentation.

## 10.5 Voyage evidence

Persist direct-kill and structure evidence through staged snapshots and Continue.

```text
swift limit = 6 + 4*rivals + 3*sizeIndex
```

- Admiral: at least 8 direct kills and >= 2× strongest rival.
- Conqueror: >= 6 built towers/beacons and >= 70% towns.
- Docks count toward economic construction evidence, not military strongholds.

---

# 11. AI rules

AI receives only information it is allowed to know.

- forecasts use observed enemies;
- radar contacts obey radar anonymity;
- flagship retreat uses known threats and legal routes;
- difficulty changes decision quality/strategy, not hidden universal stat buffs;
- AI must call the same Core commands as the player rather than mutating state directly;
- automated outpost shots are Core turn results, not presentation timers;
- AI/income/production/healing/cooldowns advance through turns/commands, not real wall-clock timers.

Do not introduce an ECS, global event bus, streaming system or heavy abstraction because “large strategy games usually use one.” Measure the current bottleneck first.

---

# 12. Rendering and performance contracts

## 12.1 Retained rendering

Static terrain/scenery should be retained and invalidated by actual state changes, not redrawn because the mouse moved.

Known principles from previous optimization work:

- terrain raster/cell sources are bounded;
- discovery creates newly known cell visuals;
- fog changes tint/visibility of retained known visuals;
- hover must not rebuild the terrain raster;
- world-space overlays may be retained across camera movement when native clipping suffices;
- manually culled overlays need explicit camera invalidation;
- fog/visibility belongs in cache identity where required;
- hidden UI/world animation should stop processing.

## 12.2 Y/depth ordering

Tall scenery, villages and hulls share world depth logic using ground anchors. Do not use the top of the art sprite as its gameplay/depth location.

Ship floor x/y and visual height z must remain conceptually separate. Test full-circle headings after renderer changes.

## 12.3 Animation populations

Keep cosmetic populations bounded and preferably viewport culled.

Examples:

- ambient schools at controlled update frequency;
- merchant traffic bounded;
- finite fireworks/fire;
- lighthouse/battery cosmetic motion only when useful/visible;
- menu scenery stops when hidden.

## 12.4 Performance validation

Never claim FPS/scalability from a microbenchmark alone. Report:

- world/map size;
- rival/fleet size;
- operation measured;
- iterations;
- allocations where available;
- p95/max frame timing for native renderer where appropriate.

Final smoothness measurement must run **serially after the final Debug build**, without background heavy tests/builds.

Headless mode is not a valid frame-pacing/pixel-quality proof.

---

# 13. UI/UX and art direction

The UI is intentionally physical and ancient, not a modern dashboard with a papyrus texture pasted behind it.

## 13.1 Core visual vocabulary

Use:

- aged/fibrous papyrus;
- irregular painted edges;
- dark ink;
- dry brush accents;
- clay/amphora badges;
- wax seals and cords;
- faction-colored ornaments;
- hand/stamp acknowledgement;
- ancient maritime motifs;
- restrained volumetric paper/clay/wood materials.

Avoid:

- glossy SaaS panels;
- modern neon gradients;
- generic mobile-game rounded rectangles where a physical motif exists;
- excessive clean vector symmetry that erases the handmade character.

## 13.2 Faction styling

Faction art may include persistent symbols/faith-based ornament. Preserve proper nation names and intended symbols. Cosmetic faction visuals cannot alter rules or RNG.

## 13.3 Papyrus interaction

v020.7 rules:

- menus/welcome/outcome may unfold/fold;
- hidden scrollbars are acceptable when the content scrolls visibly;
- footer actions remain accessible;
- modal wheel input must not zoom the map;
- advice stays fixed while selecting objects;
- end-turn paper folds after the command boundary and returns next human turn.

Lessons learned in subsequent UI work:

- reveal/fold a **fixed-size content tree through clipping**;
- never animate the content by squashing `Control.Scale`;
- rolled-edge radius/thickness may vary while folding;
- interrupted waits must complete/cancel safely;
- hidden papers stop animation/process;
- reserve ceremonial rods/beads/ornaments for important narrative/reward/result papers; keep utility menus simpler.

## 13.4 Brush buttons

When the requested design uses a brush stroke, the brush is a painted layer **behind the text**, not a standard button background rectangle.

Preserve native input/focus/hit regions even when button chrome is visually invisible.

## 13.5 Command sectors

Radial/sector command menus contain **real commands only**. Do not create invisible blank sectors just to manufacture symmetry. Arrange the actual sectors symmetrically.

## 13.6 Object information card direction

For later versions, the established direction is a compact lower-left selected-object counsel card:

- small physical panel;
- faction-colored embossed wax seal with tied cord near upper-left;
- vertical faction ornament along the left margin;
- object name prominent;
- level small/subordinate;
- short descriptive identity;
- detailed stats below in a scrollable two-column region;
- do not redundantly show HP there when HP is already represented in the world HUD;
- observed enemies may use their observed faction ornament;
- radar-only contacts remain neutral/anonymous.

This is a style/UX lesson, not a requirement to backport the v020.8 card into frozen v020.7.

---

# 14. Input rules

Input must be tested with real native pointer/touch paths for important UI.

- modal overlays own input while open;
- wheel over modal scroll content and must not zoom the map;
- right-click/cancel semantics remain consistent where supported;
- UI scaling must not offset hit boxes;
- test at interface scale extremes, not only 100%;
- touch-first interactions cannot depend on hover for essential meaning; hover may add desktop explanation but not be the only way to discover a command.

Shared-cell selection cycles only currently observed selectable occupants and does not spend actions.

---

# 15. Localization

Player-facing text supports:

```text
English
Ukrainian
Dutch
```

When adding a player-visible string:

- add all three catalogs in the same change;
- preserve proper names/Latin-letter nation identities where intentionally designed;
- localize dynamic messages, tooltips and errors, not only static buttons;
- verify layouts in all languages because line lengths differ;
- do not use English fallback as the planned Ukrainian/Dutch experience.

---

# 16. Testing strategy

Tests protect invariants, not implementation line-by-line.

For each change:

1. identify the rule/save/render invariant;
2. add/update focused regression coverage;
3. run the relevant existing suite;
4. inspect real exit code/log;
5. run native checks for input/rendering changes;
6. inspect captures when pixels/layout matter;
7. run release smoothness for final delivery.

## 16.1 Core commands

Baseline commands:

```powershell
dotnet build Dev_ancient_naval.csproj
dotnet run --project tests/CoreChecks/CoreChecks.csproj --configuration Release
```

## 16.2 Godot/native tests

Use the installed Godot 4.7.2 .NET executable and project command-line test hooks such as the existing smoke/battle/effects/sea/optimization/UI/version-specific suites.

Persistence tests always use disposable explicit paths.

Graphical changes require a graphical/native run. Headless success proves neither pixels nor input coordinates.

## 16.3 v020.7 evidence baseline

The final v020.7 record reported:

- 0 build warnings/errors;
- 1,204,156 Core assertions / 18 suites;
- eight AI matches;
- 61 v020.7-specific regression checks;
- real native pointer/keyboard tests;
- EN/UK/NL;
- 1280×720, 618×1400, 800×520 at 80–125% UI scale;
- successful final Pangaea and live-fog frame gates;
- exported EN/UK/NL/map-preview startup checks.

Do not lower this standard for a “small” release edit.

---

# 17. Release/version workflow

## 17.1 Version scheme

Project convention:

```text
v020
v020.1
...
v020.9
v021
```

Do not invent `v020.10` if the convention has moved to `v021`.

## 17.2 Historical builds are immutable

Do not overwrite a shipped release directory/ZIP just because the source has improved.

Make a new version or a clearly documented corrected artifact, preserving the original archive/hash when required.

## 17.3 Release folders

Complete delivered artifacts belong under the repository's release structure, not only in an external `outputs` mirror.

Typical delivery set:

```text
releases/<version>/
    Ancient_Naval_<version>_Windows.zip
    Ancient_Naval_<version>_Source.zip
    Ancient_Naval_<version>_Notes_RU.md
    screenshots / previews
releases/SHA256-<version>.txt
```

An unpacked playable copy may be Git-ignored if raw executable size exceeds Git hosting limits.

## 17.4 Prevent recursive export pollution

Exclude `releases/` from Godot export and source ZIP enumeration. Otherwise a future source/export can recursively include old builds.

Also exclude generated caches, `.godot`, `bin`, `obj`, temporary work, saves and credentials from release source packaging unless a specific artifact intentionally requires them.

---

# 18. Clean Windows player package

The player ZIP should contain runtime/user files, not the whole developer evidence tree.

For v020.7 keep:

```text
Ancient Naval.exe
Ancient Naval.pck
READ_ME_RU.md
complete required data_Dev_ancient_naval_windows_x86_64 runtime folder
```

Remove from player ZIP:

```text
Preview map.cmd
docs/**
*.pdb
```

Preserve the removed diagnostics in source/evidence release material.

Do **not** manually remove arbitrary .NET DLLs. Prefer `dotnet/include_debug_symbols=false` plus the standard Godot self-contained export and package exactly what the exporter produced.

---

# 19. Git workflow and coordination

## 19.1 Preserve user work

Before edits:

- inspect status;
- understand untracked/generated files;
- do not stage unrelated changes;
- do not run a destructive reset;
- do not replace the user's branch/index for a test.

## 19.2 Multiple agents

If multiple agents/specialists work at once, assign non-overlapping ownership, e.g.:

- combat/data;
- navigation/ships;
- world generation;
- persistence/economy;
- rendering/UI.

One integrating agent reviews shared entrypoints/build output. Do not let two agents rewrite `Main`, balance data or release outputs simultaneously.

## 19.3 Commit quality

A delivered change should include:

- source/data change;
- regression tests;
- documentation/version text where needed;
- validation evidence;
- release artifact only after tests pass.

Do not commit generated caches, personal saves or credentials.

---

# 20. Prohibited shortcuts

Do not:

- develop from the exported `.exe/.pck` instead of source;
- blindly `git pull`/`reset --hard` when source-of-truth is unclear;
- keep unresolved merge markers in a build;
- rename serialized enums to prettier UI names without migration;
- put Godot references into `src/Core`;
- apply damage/healing/rewards in both Core and presentation;
- let counterattacks recurse;
- save in the middle of an unfinished staged impact unless the architecture explicitly snapshots that phase safely;
- reveal hidden/radar-only identity or HP through UI/animations/logical previews;
- make God's eye change actual exploration/AI knowledge;
- consume simulation RNG for cosmetic art;
- cache navigation/vision queries beyond their invalidation lifetime;
- redraw static terrain every hover/frame;
- claim headless rendering proves pixel correctness;
- run performance gates while CPU-heavy builds/tests run in parallel;
- relax performance thresholds to make a release pass;
- overwrite old release archives;
- ship tests/logs/diagnostics/PDBs in the normal player ZIP;
- strip random .NET runtime assemblies from a self-contained export without verification;
- silently backport v020.8 features into frozen v020.7;
- add architecture frameworks/dependencies without a real measured need;
- rely on mouse hover as the only way to understand a touch-targeted action.

---

# 21. Known repository hazard at this handoff

At 4 October 2026, the connected GitHub `main` showed merge-conflict markers/stale content in files including `AGENTS.md`, `project.godot` and `export_presets.cfg`.

A clean v020.7 checkpoint is represented by PR #6 / branch `codex/ancient-naval-v020.7`, head:

```text
b03c5bdc85498f5f5d76f03f4d070adcaa13aa34
```

The next agent must verify the local canonical tree before deciding how to reconcile `main`. Do not “fix” this by preferring whichever side of a conflict looks newer without comparison to the verified release.

---

# 22. Later-version lessons versus frozen baseline

The transcript also contains v020.8 work. Treat it as two categories:

### Safe to carry forward as engineering knowledge

- fixed-scale clipped papyrus reveal;
- safe interruption of async UI ceremonies;
- hidden animations stop processing;
- actual command sectors only;
- compact faction-styled information card direction;
- UI-scale hit-testing at extremes;
- optional setup fields saved and not rerolled on Continue;
- bounded victory/defeat effects;
- difficulty should not silently buff basic combat stats/income.

### Not part of v020.7 unless explicitly requested

- Include pirates checkbox;
- new pirate count scaling behavior;
- redesigned larger tilted amphora from the later UI pass;
- v020.8 information card replacement;
- v020.8 victory/defeat presentation changes;
- any version label/title after The Sacred Voyage.

Always ask the requested version/scope from context, not from the newest code fragment you encounter.

---

# 23. Definition of done for a change

A task is not done merely because it compiles.

For gameplay/data changes:

- [ ] rule implemented in Core/data;
- [ ] save compatibility defined;
- [ ] deterministic behavior preserved/tested;
- [ ] AI uses legal information and same command boundary;
- [ ] UI reflects active rule;
- [ ] Core regression passes;
- [ ] Continue/historical fixture passes where relevant.

For UI/render changes:

- [ ] layout works at supported UI scales;
- [ ] real input path tested;
- [ ] modal input isolation correct;
- [ ] fog/radar secrecy preserved;
- [ ] hidden animations stop;
- [ ] graphical capture visually inspected;
- [ ] no significant performance regression.

For release work:

- [ ] source tree clean/understood;
- [ ] version metadata consistent;
- [ ] final tests pass;
- [ ] final smoothness gate run serially;
- [ ] actual exported executable startup tested;
- [ ] clean player ZIP and separate source/evidence package generated;
- [ ] hashes updated;
- [ ] historical archives unchanged.

---

# 24. Handoff summary for another AI

If you receive this project with no other context:

1. Work from `C:/__Beygelman/! -11/dev-ancient-naval` or a verified clone/checkpoint.
2. Read this file and `docs/ARCHITECTURE.md` before editing.
3. Inspect Git for conflicts; current `main` may be unsafe/stale.
4. v020.7 is a validated frozen baseline, not a blank prototype.
5. Keep Core independent of Godot.
6. Treat saves, fog secrecy, deterministic RNG and staged exact-once commands as hard contracts.
7. Preserve the ancient physical UI language.
8. Measure performance on representative native scenes, not assumptions.
9. Keep player packages clean; preserve diagnostics separately.
10. Never invent scope from old roadmap notes. Implement the user's current request and prove it with tests.


## Verified checkpoint and release integrity

- The frozen v020.7 gameplay baseline is `b03c5bdc85498f5f5d76f03f4d070adcaa13aa34`. A primary local checkout may contain newer intentional v020.8 source; compare file contents and protect it before creating an isolated finalization checkout. Git status alone mislabels valid untracked v020.7 files as additions relative to an older HEAD. Never replace the newer working tree to match a release branch.
- Corrected packages are additive under `releases/v020.7/finalized/`; original ZIPs, manifests and `releases/latest.json` are immutable evidence of their earlier publication. The older `Sync-Release.ps1` updates the original manifest/latest pointer and must not be used for this additive freeze.
- Use `Export-FinalizedWindows.ps1`, `Package-FinalizedRelease.ps1` and `Verify-FinalizedRelease.ps1`. Audit the PCK directory as well as ZIP members; an apparently clean outer ZIP can contain development resources internally. Preserve every exporter runtime file except PDBs. Reject patch/removal PCK entries for a full player pack.
- Source archive evidence lives under `release-evidence/` with `.gdignore`; the active preset and PCK verifier must exclude/reject that folder. Test export from an extracted source archive, since a working-tree export alone cannot expose recursive evidence leaks.
- Source/archive reproducibility requires reviewed file inventory and hashes, fixed ZIP entry order/metadata, and the same compressor for byte comparison. Godot/MSBuild export itself is a repeatable procedure, not a promise of byte-identical executables. Record the actual baseline, source snapshot and later artifact delivery commit separately.
- Hash-bound release evidence must preserve raw bytes in Git. Scope `* -text whitespace=cr-at-eol` to the new delivery folder; use `*.log -diff` for raw logs. If files were staged before that attribute existed, Git stat caching can leave normalized blobs in the index: re-add only that new folder with scoped `git add --renormalize`, then compare stored Git blob SHA256 with on-disk bytes and the manifest. Never normalize the primary tree or historical releases as a shortcut.
- Godot 4.7.2 `Performance.TimeProcess` is the previous one-second maximum including rendering, not live per-frame C# CPU time. Use the recorded Stopwatch frame intervals for release thresholds and explicit trace scopes for code costs; do not infer a gameplay CPU bottleneck from the monitor alone. Run final frame gates after the last C# Debug build on a quiet host, and retain both earlier failures and the fair repeat.
- Keep historical diagnostic screenshots in Git/original releases; avoid duplicating them in each source ZIP. Include required assets and engineering text plus selected current validation captures, record exclusions, and check archive size before Git delivery. Never delete old screenshots merely to shrink packaging.
- Real historical fixture copies live in `tests/CoreChecks/Fixtures/Historical`; their hashes/provenance are documented there. Set `ANCIENT_NAVAL_LEGACY_FIXTURES` explicitly to include these cases. Automated save writes must still use disposable paths.
- Legacy runtime tests may assume instant menu close or no deferred reward modal. Update their synchronization to the current rolling-paper/claim contracts; retain bounded waits, input isolation and exact-once assertions. Do not disable correct checks or alter production behavior to accommodate an obsolete fixture.

See `docs/V0207_FINALIZATION_REPORT.md` for the measured reconciliation and `docs/RELEASE-PROCEDURE-v020.7.md` for repeatable verification/export steps.

## Canonical checkout context

### iOS / NativeAOT invariants

- iOS uses Godot 4.7.2 .NET's experimental NativeAOT export, plain `net8.0` and
  the SDK's `GodotTargetPlatform=ios` configuration. Do not convert the project
  into MAUI, `net8.0-ios`, GDScript or a browser port to avoid the native export.
- All production JSON roots use generated metadata. .NET 8 source generation can
  replace missing init-only property initializer defaults with zero/null. Keep
  `BattleRulesJsonConverter` registered in both Core and session contexts: it
  supplies missing historical defaults without changing explicit null/zero or
  making immutable rules mutable. CoreChecks compares legacy serializer bytes and
  historical fixtures; `--aot-json-test --save-file=<disposable>` explicitly turns
  off reflection in the desktop host before initialization.
- iOS player compilation excludes `tests/Runtime` and corresponding Main hooks.
  Keep JSON source generation metadata mode and normal Godot dynamic script
  loading; do not disable all reflection/trim script roots as a shortcut.
- Native safe areas are physical pixels: transform through viewport screen
  transform before CanvasLayer offset/scale. `ScreenToUi` must subtract the same
  safe origin so world-anchored paper stays attached. Desktop uses full viewport.
- Touch UI ownership uses actual finger coordinates, visible clipped native
  controls and gesture cancellation. A new UI finger must not become a sea pinch;
  app suspension/focus loss clears gestures. Background save never finishes or
  commits a partially presented battle order; retain the previous stable save.
- `tools/Build-iOS.sh` stages outside the source with public NuGet and pinned
  .NET SDK 8.0.425 (Windows' bundled-only feed is not portable). Godot runs from
  that staged directory. No source/export credentials are overwritten. Export
  preset 9 uses project-only mode and ad-hoc framework identity `-`; Apple account
  signing occurs later in Xcode. Preserve the entire exported Xcode folder.
- macOS/Xcode build and device validation are mandatory before calling the iOS port
  playable. Windows build/synthetic touch/Core checks prove neither an iOS build
  nor device frame pacing. Never claim an unsigned Xcode ZIP is installable IPA.
- The cloud Mac can also package its arm64 physical-device `.app` as an unsigned
  Payload IPA. AltStore Classic/AltServer can personally sign/install that IPA
  from Windows; the user need not own a Mac. This is not AltStore PAL/TestFlight,
  does not require JIT, and remains unverified until installed on a real device.
  Staging must include the root `.sln`: Godot export requires it even when an
  ordinary `dotnet build <csproj>` succeeds without it. Job-level workflow env
  cannot reference `runner.temp`; use step-level env instead.
  Select Xcode 26+ explicitly: macos-15 defaults to 16.4, whose iOS 18.5 SDK
  cannot link Godot 4.7.2 template Metal/CoreAnimation symbols.
  Preserve the whitespace-only `.cs` PCK resource placeholders; Godot script
  lookup still needs their paths. They are not source-code leaks. NativeAOT
  shared framework binaries can have mode 0644: dyld opens them read-only, while
  the app's MH_EXECUTE entry point needs an execute bit. Audit Mach-O platform,
  architecture and signatures separately instead of chmodding exported assets.
  The official 4.7.2 .NET Simulator engine archive contains x86_64 objects only,
  despite its XCFramework metadata listing arm64 too. NativeAOT's own Simulator
  framework is universal, which does not make the engine universal. Use the
  Intel `macos-15-intel` runner for the optional Simulator smoke; keep ARM64
  for the real-device IPA and inspect actual binaries rather than metadata alone.
  Use handheld orientation 4 (Sensor Landscape); 6 means Sensor in all directions.
  Audit the exported app plist for both landscape orientations and no portrait.


The preserved primary working tree contains intentional v020.8 work. This guide
was upgraded during the isolated v020.7 audit; do not downgrade those sources.
The prior local guide is preserved in docs/history/v0207-finalization-primary/AGENTS.md.
The clean v020.7 source checkpoint is on codex/v0207-finalization, snapshot
ce6d1e50d82c57b4971d3c28da119607b148e1d1. Both final native frame gates passed
serially after the final Debug build on 5 October 2026. Final source/player
archives and validation are additive under releases/v020.7/finalized; earlier
candidates and original archives/manifests remain untouched. Remote main and
PR #6 were not pushed/merged. Never run Sync-Release for this additive checkpoint,
since it rewrites historical manifests/latest. Read docs/V0207_FINALIZATION_REPORT.md
and FINAL-ARTIFACT-VALIDATION.json before subsequent development.
