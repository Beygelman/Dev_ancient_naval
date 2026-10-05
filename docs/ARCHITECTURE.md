# Ancient Naval architecture

## v020.5 voyage and construction contracts

`MapSize` is optional saved board metadata. Explicit Lake/Bay/Sea/Ocean areas scale the mesh independently of rival count. Generation validates the existing separated-coastal-settlement quota and retries a bounded deterministic terrain salt on the same mesh; a missing size follows the historical generation path exactly. `FishingCannonTowers` defaults false, `PortRules.MaximumRouteLength` defaults zero (unlimited), and nullable `VillageAutoRepairAmount` falls back to historical active repair for older snapshots. Serialized `ShipClass.Fishing` remains unchanged; its current display/model is Support Brig.

`ConstructionPreview` retains noninteractive wireframe commands for a legal, visible build cell. It never mutates the simulation, terrain cache or random state. Tower fire and lighthouse sight outlines filter to explored cells. Main clears it whenever command context or observation changes. Town and ship producers use the same legal-cell facade.

`RollingVoyagePaper` uses a clipped native scroll with a hidden scrollbar, retained grain, and short-lived rolled-edge/choice animations. Settings owns language and UI scale in both menus. Generation runs on the existing session worker while the handprint/roll/fade owns input. `VoyageWelcome` guards map commands during the camera descent and elder acceptance; Continue resumes without replaying the ceremony.

Town decoration fits continuous adjacent inland soil while Core town coordinates and port berths stay fixed. Beach/hole/obstacle clipping applies to the full ground footprint. River mouths are retained cosmetic gradients crossing the painted beach and fading into existing shallows; neither they nor their wider meanders change navigation or simulation terrain. Sea ruin glow depends on uncollected observed ruins, then fades after collection while scenery remains noninteractive.

## Dependency direction

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

`BattleState` remains the single writer for gameplay commands. Separating its
partial files organizes rules without duplicating state or introducing ordering
ambiguities between independent controllers. `BattleNavigation`, `BattleCombat`,
`BattleProduction` and `BattleTurns` expose the existing API. Feature modules
handle villages, balloons, discoveries and progression. `Ship` contains per-unit
state and derived statistics; `BattleRules` contains configured definitions.

## Navigation lifetime

`PathSearch` implements stable Dijkstra with `(cost, insertion order)` priority.
`NavalNavigationQuery` indexes visible enemy occupancy and threat cells once per
query, then memoizes visited terrain/passability. Player estimates and actual
movement use different knowledge modes. A query must not survive a command. `MovementPreview` is instead an immutable
cost/predecessor snapshot, reused for cursor destinations only while the battle,
selected ship, position, budget and optical-vision revision still match.
During actual movement, only the moving friendly ship and optical visibility
change; the query uses real terrain and enemy occupancy, which remain constant.

This avoids rescanning every ship for every explored edge. It does not add a
global position index, which would require a new mutation/invalidating contract.
Optical/radar coverage on organic maps uses a cached bounded breadth-first
search. Its result is tested against full-board distance coverage at radii 0–64
on all four world sizes. It follows corner adjacency, including land, and never
uses the moving ship's navigation restrictions.

`NavigationReference` in tests freezes the pre-refactor implementation so costs,
route ties and fog behavior can be compared independently.

## Scene and animation

`Main` builds the scene and wires events. `MainSelection` interprets clicks and
previews; `MainCommands` executes or stages commands then animates; `MainView` refreshes
HUD/map projections; `MainSession` manages start/continue and opponent turns. `MainSaving` copies a
save DTO on the game thread and serializes/flushed-writes it on a worker while
commands remain locked. No mutable live battle or Godot object crosses threads.

`FleetView` prepares stable depth order and composes the frame. `FleetShipDrawing`
draws units; `FleetCommands` plays command results; `FleetAnimation` interpolates
movement and projectiles; `FleetEffects` renders transient water/air effects.
Projectile orders call `BattleState.Prepare`, which resolves deterministically
on an isolated aggregate with shared immutable board/rules. Saved phases reuse
one geometry snapshot. `PresentedCommand.Impact` applies each Core phase only
after the last shell in that salvo lands; counterattack has its own phase.
Surviving ship/village instances keep their identity. Visual pellet count cannot
change damage or RNG. The immediate facade remains available to simulations.
Movement/build/resource orders stay direct to avoid unnecessary cloning.

`Finish` applies the final state and releases the pending order. Core rejects
actor commands, EndTurn and snapshots while an impact order is pending; scene
input is locked as well. Exit explicitly finishes before saving. A destroyed
Mother leaves its followers temporarily drawable until its wreck has submerged,
then `CompleteFleetCollapse` removes them and their sinking begins. Hidden wrecks
do not introduce a spectator view. Healing receipts report actual HP changes,
never a second application of healing.

Static terrain has retained surface, shore and low grass sources for each cell
inside one isolated `SubViewport`. Trees and peaks live outside that raster in
the Fleet native Y-sort root, alongside villages and moving hull canvases. Each
tall object uses its own projected ground anchor; nested Y-sort groups flatten
into the same ordering. Static scenery retains draw commands. Visible town
flags/mills redraw at 30 Hz in their village canvas, and hidden explored towns
retain their last observed drawing. Unknown cells hide their scenery, including
overhanging geometry. A cell
builds drawing commands on first discovery; subsequent fog changes only set its
visibility/tint. A new projection replaces the sources and reuses the render
target. The GPU updates the bounded atlas as a whole; this is not partial GPU
texture upload. A texture sprite displays its last raster; updates are `Once`,
not continuous. A post-draw version check explicitly stops updates without losing
a request raised later in the same frame. Limits
are 4096 pixels per axis and 8,388,608 pixels (32 MiB RGBA color; conservatively
64 MiB for render/sample attachments, excluding auxiliary driver allocations).
Fog masks are compared before invalidation; hover, selection, combat health and
repair cannot trigger an unchanged terrain rebuild. Dynamic objects are culled
to a padded viewport and refreshed after camera changes. Whole-island contour
geometry is clipped per tile so an unseen shore cannot leak through the texture.

Known resources, contacts, radar and movement/target contours have a
separate retained observation layer. Refresh it after game state/selection or
camera changes. Cursor previews redraw only the route, selected outline and
cached destination coverage. The coverage cache is bounded to 96 destinations
and invalidated by projection, selection, vision or mortar changes. Never
reuse the observation layer across a visibility change without invalidation.

World ambience updates its stationary-point caches on `RefreshVisibility`.
Reusable vertex buffers are consumed by draw calls before the next use. Menu
layout responds to resize events. Procedural scenery remains bounded and separate
from simulation randomness.

## Balance and persistence contracts

`data/balance.json` supplies ship definitions and typed mortar, balloon and
treasury settings. Validation rejects invalid/null groups and invalid probability
totals. Defaults preserve 0.14 rules when new groups are absent from a v1 save.
Existing saves carry their own rule snapshot; changing the bundled JSON affects
new games, not the balance of an already saved game.

The session envelope and battle snapshot remain version 1. Keep serialized enum
identities and obsolete-but-persisted fields until there is an explicit migration.
The save layer validates references and values before restoring indexed state.
Camera state belongs to the session envelope. Fleet palette choices and roster
are persisted in Core because they must remain consistent across all factions.
Captain identities and town names are optional v1 fields. A separate seeded
cosmetic stream supplies them for old saves without advancing simulation RNG.

`SaveStore` writes a flushed temporary file, atomically replaces the primary and
retains a previous valid backup. After recovery from a corrupt primary it keeps
the known-good backup rather than backing up the corrupt file over it. New game
atomically installs a new voyage and removes the prior voyage backup; subsequent
backups are earlier checkpoints of that same voyage.

## Verification and limitations

The core suite includes 100 generated map seeds and complete AI matches.
Navigation refactor checks compare all traversable edge costs in rectangular and
organic worlds, hidden/visible enemies, radar, narrow passages and pirate patrols.
Synthetic fleets of 16/128/256 units measure full-board navigation allocation and
elapsed time. `--optimization-test` compares idle draw preparation for
16/128/512 units, stable ties, changing fog and menu resize behavior. These are
microbenchmarks, not a whole-frame FPS or large-fleet gameplay guarantee.

Godot runtime modes: `--smoke-test`, `--battle-test`, `--effects-test`, `--sea-test`,
`--menu-test`, `--optimization-test`, `--world-test`, `--income-test`,
`--performance-test`. Performance checks accept `--opponents=1..4`, `--scaled-map`,
`--live-fog`, `--report=...`; autosave measurements require an explicit test slot. Menu tests require an explicit disposable
`--save-file=`. Save compatibility and backup tests use temporary directories.
Visual smoke checks should use the compatibility renderer and capture a PNG with
the test's `--capture=` option where supported.

To include actual released save fixtures in the core suite, set
`ANCIENT_NAVAL_LEGACY_FIXTURES` to a directory containing at least two untouched
session saves named `baseline-menu-*.json`. The regular suite also creates its
own v1 compatibility cases when external fixtures are unavailable.

The project still has intentionally shared state in the battle facade, legacy
compact methods and some fixed rule constants. Future extractions should follow
real feature changes. World streaming and multi-faction generalization have not
been implemented by this refactor.

## Multiple fleets and scarcity

Core owns a persisted 2–5 faction roster plus neutral pirates. Every different
owner is hostile. Eliminating a Mother withdraws its fleet and neutralizes its
towns. Alive factions determine turn order and the eventual model winner. The
human scene stops at PlayerDefeated instead of forcing a full spectator campaign.
EconomyRules keeps backward-compatible defaults for old rule snapshots; current
JSON supplies slower income growth and naval upkeep. IncomeReceipt events describe
source gains/deductions; presentation only shows the human fleet’s private receipts.


## Deck and ambience projection (0.16)

DeckProjection rotates a horizontal x/y floor before applying the sea's vertical
foreshortening. Height z is an independent screen-vertical offset. CityShipArt
reuses its buffers, orders building footprints by projected depth and draws only
visible wall faces; nearly edge-on faces are guarded triangles. Convex floor
surfaces retain nonzero area at every yaw. Shell aiming converts screen direction
back to the deck plane; movement uses the same convention.

FishSchools caches visible school geometry on the normal visibility refresh and
renders three/fourteen swimming fish in WorldAmbience, never in TerrainRasterCache.
Wildlife remains capped (18 transient gulls, one dolphin); three dock gulls are
procedural. Schools, clouds and docks are viewport culled. The title harbor is a
static bundled TextureRect and has no processing loop.

VillageCombatRules and BalloonRules.AntiAirRange are optional persisted balance
settings with defaults for released saves. FireOutposts resolves once at owner
turn end before automatic repair, marks active town attacks, and records
visibility-safe OutpostShot snapshots. Presentation only animates those results.
The policy skips manually repairing towns and never damages air units.
FlagshipSafety uses observed enemies only and compares known weapon exposure
across legal routes; it is deliberately local, not an omniscient future planner.


## 0.18 navigation and presentation contracts

`TradeNetwork` builds deterministic shortest maritime links between live same-owner ports, with diagonal land-corner and whirlpool exclusions. Its command-independent cache is invalidated by ownership/zero HP/hazards and aggregate restore. Naval occupancy remains query-specific. `TradeNavigation` uses (cell, lane run) search states and cumulative integer rounding, so the minimum two-tile/20% gain survives both previews and split orders. Off-lane steps reset the run; extra terrain/threat costs remain. `MovementPreview` captures a fixed origin/path reconstruction, never a mutable ship position.

`GodEye` and `AiDifficulty` are optional v1 save fields; missing difficulty defaults to Captain. Port rules and saved town/ship lane state also have legacy-compatible defaults. `BattleVision` keeps genuine memory and the human full-map override separate. Allied radar supplies coordinates to weapons irrespective of an individual unit's radar; observations still require optical sight. Human victory activates full visibility.

Town art is retained apart from mill/flag animation and a Z=6 interface canvas. Gameplay invalidation refreshes town UI even when Vision.Revision did not change. Sea strokes/triangles are submitted in batches with retained buffers; there is one color per multiline segment. Range/resource/trade commands use native viewport clipping and are not reissued on pans. Hulls are not toggled hidden/visible each frame: native position/rotation carries bobbing; state, heading, barrel, sinking and slower city life trigger art redraw.

Admiral is bounded one-turn planning, not an omniscient tree search. It ranks finishing blows, counterfire and an upper-bound next-turn enemy reach forecast; coordinates supporting hulls and avoids costly healthy flagships until firepower is sufficient. Only observed ships feed that forecast. Cheap scouts use anonymous radar positions. Boatswain/Captain/Admiral share rules, prices, income, RNG and visibility.

## Information cards — 0.18.1

`Presentation/UI/AncientLore` reads observed current state into `LorePage` sections
and labelled rows. It does not catalogue uninstalled upgrades or future unlocks.
`InformationHud` renders these records as native containers inside a bounded
scrolling papyrus card; identical visible text reuses its nodes and reading
position. Keep the title/close controls outside the scroll and derive layout
from native content/header margins. Radar contacts may be inspected even on
uncharted terrain, but only their anonymous description is available. Gameplay
calculations and saved rules remain in Core.

## Worlds, clay seals and victory — 0.19

WorldKind is an optional SavedBoard field, default Oceans. The original Oceans terrain
branch is unchanged. WorldLandscapeGenerator applies continuous envelope/islet/ridge
policy to the existing mesh, carves edge-connected shallow rivers and connects trapped
water to the sea. WorldSettlementPlacement assigns three coastal sites per starting
territory for the new policies; normal treasury fairness and pirate counts remain.
Fleets keep safe ocean berths; small hulls may navigate narrow rivers.

VoyageStatistics is an immutable Core value, captured/restored by all staged command
snapshots. Gross receipts and rewards, actual shipyard hull construction and direct
player weapon/counter/outpost kills update once at their Core event. Forced fleet
collapse is excluded. Old snapshots without this optional value start at zero; loading
never reconstructs lifetime totals from current money or surviving ships.

MainActionStories waits for the cosmetic ActionPapyrus ceremony while Busy blocks
orders, validates the same battle/target afterwards, then calls the existing command
facade. Treasure commands still use staged impacts. Both the large illustration and
ordinary icon use this wrapper. Art has no access to simulation RNG.

VesselArt uses independent floor XY and height Z, reusable face buffers, visible wall
culling and projected footprint ordering. HealthAmphorae canvases at Z7 follow position
and bobbing without deck yaw. AmphoraHealthAnimation observes actual visible snapshot
changes; unchanged draws do not restart it. Town interfaces at Z6 use the same helper
and only queue extra 30-Hz paint while their health animation is active. Feedback is
Z8 and originates at the digits. Radar-only destroyed hulls never gain a retained
class/health snapshot. Connected cloud solids share side/top boundaries and one shadow.

SceneryAtlas packs the original seeded tree/peak primitives in ground-Y order into
2048×2048 transparent pages at 2× resolution. Every object remains a Sprite2D at its
original ground anchor in the shared native Y sort; texture offset carries its height.
AtlasTexture regions share page RIDs and a single premultiplied-alpha material, avoiding
dark translucent fringes. SceneryAtlasPage disables its viewport after FramePostDraw
confirms the painter ran, then frees the primitive canvas and disconnects its callback
exactly once. A new projection replaces both pages and anchors. Hover/camera/fog changes
never rebake; each sprite's original owning cell controls visibility and tint. Towns
remain separate retained/animated canvases. Cosmetic placement and Core RNG are unchanged.

MainOutcome waits until all command/impact/sinking presentation finishes before opening
a CanvasLayer60 modal result. It disables map input and hides the regular HUD; Return
closes fireworks/input ownership before showing Home. Repeated Refresh does not restart
the result. Fireworks are finite (7 bursts, at most 224 sparks) and processing stops.

## River kingdoms, world actions and compatibility — 0.20

`FreeCoastalNavigation` defaults false in old rule snapshots and is true in the new balance. NavalNavigationQuery receives this policy per search; when enabled it removes the Mother narrow-cell veto and terrain multipliers. Threats, forbidden cells, land, occupancy, knowledge and corner policy remain common to previews, actual sailing and AI. `Balloon.KolonelAntiAir` likewise defaults false; HasAntiAir/AntiAirCovers are shared by damage, attacks, counters, gun-position searches and Admiral forecasts. Red is appended to FleetColor, preserving existing enum identities.

Pangaea generation retains the existing organic mesh. Seeded lake envelopes are linked by sampled meanders and shared-edge paths, plus tributaries, external mouths and paired basin bypasses. ConnectWater guarantees one edge-connected water network. PangaeaWaters classifies the inner world-space envelope, used to prioritize two interior settlements per territory and interior fish while retaining fair settlement counts and starter resources. Snapshots store the resulting terrain/resources; Continue does not call the generator.

ActionStoriesHud tracks all eligible `(action kind, target ID)` pairs independently of selection. Ready scenes follow their projected target on camera changes; offscreen scenes stop their levitation. The UI sends target IDs to MainActionStories, which selects that object, blocks input while consuming its scroll, revalidates battle identity and eligibility, and commits once after the cosmetic ceremony. No invisible capture-marker hitbox remains. Artwork follows a top-anchored half-circle via explicit convex textured quads, avoiding triangulation of an almost-zero-width closing annulus. Ship/resource RadialPapyrus uses the opposite upward half-circle and clamps only at viewport edges. Purchase costs are active-rule values; repair/cooldown annotations are not mistaken for prices.

FactionSanctuaryArt shares projected floor XY and upright Z between ships, towns and setup emblem previews. Neutral towns omit this monument; owned towns scale it with level. Static house randomness depends only on map seed/town ID, never simulation RNG. Existing native ground-Y anchors and observed-town redraw rules remain; hidden towns keep the last painted ownership/level. Trade routes are retained world commands, with cubic interpolation, checked water bends and white dashes. Only their visual endpoints extend to the port's land/sea midpoint; the Core trade network still begins at the unoccupied sea berth.

## Encounters, salvos and camera — 0.21

Optional `DoubleSalvo`, `EncounterCurrencyReward` and four `LevelCurrencyRewards` settings default to false/zero in historical snapshots. Saved `Encounters` stores one faction and first optical location per rival; validation rejects duplicates, foreign rosters and invalid cells. UpdateVision consults underlying optical visibility, excluding God's eye and radar. RestoreProgress replaces encounters before recomputation, preventing repeated rewards. Level income belongs to GrantResources' level transition, not to the upgrade choice dialog. Both sources count toward VoyageStatistics gross receipts.

Attack/AttackAt/AttackVillage accept an optional double-salvo command. Only Kolonel or a Second-Attack flagship with two charges may use it; mortars are excluded. Both charges are spent atomically. Active ship impacts have separate `attack` and `attack2` snapshots, followed by one `counter`; destroyed targets stop remaining fire. Village double fire plays two salvos before the single structural impact and response. Presentation does not multiply Core damage. Mouse/touch releases distinguish tap from a real monotonic-time hold; drag/focus loss/pinch cancel the gesture. Hover events are coalesced per frame.

MapCamera.FocusAsync uses real elapsed time and a quintic smoothstep, 0.25 seconds for observed ordnance and 0.5 for encounters. ViewChanged fires only after the final constrained canvas transform. Camera cancellation always resolves its awaiting task; no killed Tween can strand an order. FleetView focuses a visible target before aiming/launching; radar identities remain anonymous. Main presents each new encounter after the revealing command and excludes saved encounters when restoring a session. Opponent turn UI names a captain only after HasMet, without central action notices.

ActionPapyrus now draws a straight cropped rectangular illustration, unfolds once and starts a 0.42-second burn immediately on activation. Temporary canAct locks preserve banners but disable hit testing; off-screen clipping preserves reveal age. A new battle or actual eligibility loss discards a banner. RadialPapyrus uses lower-half geometry. Visible-arc inset places it under the scaled class-size progress row without moving it to the ledger.

SeaGeometryBatch reuses exact-count native upload arrays between matching populations; changing viewport populations resize them. RouteCoverage uses OrganicMesh.Within local-radius results and known-cell filtering instead of creating full-board step tables for cursor positions. SaveStore serializes an immutable SessionWrite directly to UTF-8 on its existing worker thread, retaining the same disk JSON contract and atomic recoverable writes.


## v0.20.2 terrain, culture and heavenly aid

`TerrainFeatures.For(board)` weakly retains deterministic mountain cells, whole-cell forest density, inset peak footprints and coast-distance bands. Island-component principal axes guide ridges; a full surrounding land cell buffers each mountain from water. Core optical coverage casts rays against actual mountain mesh polygons, retaining coverage by origin/radius/shape and bounding the cache. Radar bypasses mountains and excludes small stealth hulls only when the saved optional rule is enabled.

Resource placement uses an independent seeded central-weighted stream, approximately 70% of the original budget and one near-home catch per fleet. Settlement selection preserves local access and central contested sites. Generated worlds apply initial settlement levels and two fortified pirate bays; hand-authored fixtures retain their supplied initial state. Terrain features reconstruct from saved geometry without consuming simulation randomness.

`ConvexSoilClip` subtracts each convex beach strip from triangulated village ground by half-plane partition. The result consists of disjoint filled pieces, so fully enclosed beach holes cannot be drawn as soil. Town stamps and terrain/scenery atlases are retained through pan and selection.

Personal-turn and direct flagship-kill counters are optional saved arrays. New rules enable payout at every fifth personal turn start, carried by `CommandResult.HeavenlyReceipts`. Staged snapshots include currency/counters; presentation only announces the committed receipt. The finite heavenly screen overlay never invalidates world geometry or accepts mouse input. Unknown rival aid omits the captain, amount and kill tally.

`CultureNamePools` owns color-specific Latin-letter names. New voyages can retheme before their first command; restored voyages preserve saved identities. Missing old naming flags use the exact historical seeded naming stream. Optional rules default to their historical behavior, including no heavenly aid, no mountain shadows, no small-hull radar stealth and no additional flagship resource requirement. Start a New game to use the new gameplay rules.

## Corrected v020.2 presentation

`TerrainRasterCache` divides its bounded texture into 512-pixel regions with padded, disjoint atlas interiors. Discoveries retain one source per covered cell/layer and local fog changes enqueue affected GPU regions one per frame. Empty corner regions have no sprites. World identity resets the queue; hover and pan do not invalidate it. `SeaGeometryBatch` retains native triangle/line buffers and uses alpha-fringed triangle strips to submit many shore/trade lines together.

`TownArtworkRaster` retains independent back/front snapshots for observed towns. Health changes do not rebuild artwork; a wall change rebuilds only the foreground. Hidden town changes cannot alter their last observed texture. `TownLayout` reserves a monument plaza and spaces house footprints on a deterministic warped lattice; animated mills remain between back houses and foreground walls/wheat.

`MainSalvoChoice` stores the current battle, actor and target identity until an icon is chosen. `SalvoChoiceHud` positions two cost-free sectors under target progress. A valid double command carries `SalvoCharges=2`, even if its first impact is fatal. `FleetCommands` launches both charges together, commits active impact keys after the shared flight and presents one counterflight. Actual damage remains exclusively in Core.

`Language` registers a Godot Translation bridge so native controls retain English-origin keys and render their active locale. `LocalizedMessages` validates placeholders, translates complete dynamic clauses and handles Ukrainian/Dutch plural forms. Catalog caches are bounded. Proper names remain Latin-letter identities. Language preferences are separate from battle saves; tests use explicit disposable companion paths.

Lighthouse is an appended serialized class and optional saved rule. It has fixed sea-tile artwork, sight 4 and an optional paid radar. Mother/village production restrictions use the existing production facade. Trade routing exposes logical routes plus deduplicated rendering edge chains; rendering never changes navigation bonuses.


### Circular command parchment and coastal towns

Object command sectors share a proportional arc capped at one circle with a small seam. Their world target hit exclusions use only existing legal target coordinates, update with camera transforms and preserve radar anonymity. Target salvo and resource parchment remain separate. Mystic upgrade labels belong to Presentation; hover effects come from saved Core rules.

TownPlacement fits the complete future-town ground envelope inside land minus beach with a 0.3-pixel clearance. All static art, mills and flags share that transform through level changes. Roofs remain upright; port piers intentionally bridge the shoreline. Fortification invalidates both raster layers because the closed square wall has rear and front halves.

The released 2D renderer is OpenGL Compatibility after repeated native D3D12 first-move stalls. Frame-pacing gates run against the project's configured renderer. The timing harness rejects external GUI/map input while invoking production handlers; actual-pointer UI regressions run separately.

The smooth island surface is clipped per cell across both land and adjacent sea seams; clockwise lake contours subtract triangulated holes. Sea beneath the contour replaces whole-tile sand fill. Large visual ridge summits can span adjacent land, and smaller connecting peaks retain their parent mountain cell for fog and optical obstacles. Forest sampling uses a continuous seeded field. Animated waves retain visibility-scoped mesh geometry and update a clock uniform; a new vision/world rebuilds it.

## v020.6 guidance and cosmetic lifecycle

ReadyActions is a pure command-derived Core query, one record per eligible owned object. Presentation caches exact owned-object inputs, credits/capacity/visibility; counter clicks never apply a command. Hints are independent UI preferences. Confirmation and stamp phases own keyboard/map input. Lighthouse trade previews operate on known terrain and observed hazards without changing the live route cache. New mechanics use optional embedded rule defaults for historical snapshots.

WreckModels constructs bounded independent solid components with floor x/y and height z. WreckRenderer retains their faces, applies individual release/drift/rotation/gravity and clips against physical sea height before isometric projection; exposed faces remain opaque. No raster slices or per-frame mesh construction are used. Wreck canvases are released on completion or world replacement. Gull alarms consume cosmetic state only and observable gunshots; shrine animation never redraws static town rasters. Treasury sprites hide after observed demolition without rebuilding the scenery atlas.

TutorialAdvice observes public owned-object events only and stores once-per-voyage topic IDs in a separate bounded presentation sidecar. Its nonmodal upper-left paper uses actual native-rendered controlled-fixture screenshots, suspends during commands/menus/modal rewards and shares UiHints. LoadScenario alone cannot start tutorials, so renderer diagnostics never write tutorial preferences to a real save slot.


## v020.7: faith, weighted crews and shared occupants

`WeightedFleetCapacity`, `DiagonalVillageBerths`, `ScuttleRefundFraction` and `Balloon.CrashDamage` are optional saved rules: absent fields preserve previous slot, coast, refund and no-crash behavior. New balance enables them. `FleetSlotCost` is common to production, ready actions and the ledger. New paid hulls/structures save `ConstructionPrice`; starting and granted hulls save zero, while old missing receipts use the active catalog price. `ScuttleRefund` uses whole-number rounding and the command pays once.

`PortCell` persists the sea berth selected by `PreviewPortBerth`; this pure query searches legal water steps using the same corner/cost policy as `TradeNetwork`. Later network changes cannot relocate existing ports. Diagonal production still uses occupancy, outer-rim and saved-rule checks. Board art extends to the actual coast between the village and its saved water endpoint.

`BalloonCrash` is a separate impact boundary after the killing/scuttling boundary. Core applies all nine-cell ship damage before collapsing a killed flagship, and keeps village hits in the same result. Native presentation reconstructs curved solid cloth gores, struts, burner and basket; it commits the crash at sea impact, then presents destroyed victims. Hidden origins/victims are never rendered. `DirectShipKills` counts each captain's direct hull losses; `VoyageStatistics.StructuresBuilt` and `SeaStrongholdsBuilt` distinguish all paid sea structures from cannon towers/lighthouses. All evidence survives staged copies and saves; missing old evidence starts at zero.

`VoyageJudgement` reads final public evidence without commands. Combat distinction requires at least eight direct kills and twice the strongest rival; conquest needs six constructed towers/beacons plus 70% of towns; swift limit is `6 + 4*rivals + 3*mapSizeIndex`; mature economy uses rivals, size, construction and owned-town counts. Difficulty chooses the acclaim tone. `NationIdentity` proper names and `NationOrnament` vector crests share faction faith, including Rose Crown and the thick serif crimson R.

`RollingModalPaper` resolves interrupted animations safely. `BrushPaperButton` retains native input/focus and paints transient ink/hand effects. Advice remains unchanged across selection. Welcome/outcome put action buttons in a fixed footer, scrolling only narrative/statistics with no visible bars; most modal paper is bounded to 60% height. The explicitly revised setup instead has equal top/bottom/right margins. `MapInput.PointerEnabled`/`ZoomEnabled` isolate open overlays from background gestures. `MainTileSelection` cycles only currently observed occupants; attack/build orders keep priority and hidden objects cannot enter the cycle.

Trade traffic revalidates its recorded lane cells after Core cache replacement, retaining legal voyages instead of resetting them. Shrine/lighthouse/battery idle effects use cosmetic clocks and visible-object bounds; they never consume battle RNG. Shader wave and terrain retention contracts remain unchanged. Final native frame gates must run alone after the final Debug assembly is built.
