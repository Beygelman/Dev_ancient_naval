# Ancient Naval change history

## 0.20.2 — 2026-10-02 — Bays and Blessings

This user-requested version label builds on the completed 0.21 checkpoint and retains its salvos, camera flights, claim banners, ports and save compatibility.

- Fish resources use approximately 70% of the previous map budget, with one guaranteed first catch per fleet. Resources and coastal settlements concentrate toward the center; Pangaea retains its inland river/lake emphasis.
- Mothership improvements require **3 / 4 / 5 / 6 resources**. New maps guarantee two black, fortified level-3 pirate bays; ordinary neutral towns sometimes start at level 2 or 3.
- Each fifth personal turn grants the player **2 Thors per living owned town and Mothership**. Rivals receive **2 Thors per enemy Mothership they directly destroyed**. A top plaque and finite rays announce the reward without revealing unknown nations.
- Brig and Fishing Schooner evade radar. Mountains cast optical shadows, while radar coverage remains unaffected.
- Sparse groves alternate with plains. Inset mountain ranges follow island geometry, beaches vary in width and fill rounded land cutouts, and deeper sea is slightly darker. Town soil is irregular and clipped away from beaches; wheat sits nearer houses, walls divide it from the houses, taller towns gain taller monuments, and flags remain attached to their poles.
- Expanded Latin-letter captain and town names follow six color themes: colonial British, Egyptian, Japanese, German, woodland fantasy and Andean fantasy; pirate bays have their own names. Continue retains saved identities and rules.

[Validation and compatibility](docs/MILESTONE-0.20.2.md).

## 0.21 — 2026-10-02 — Encounters and Salvos

- Place symmetric command papyri beneath ship/town progress, with size-dependent hull progress distance and a compact arc inset. Keep Info on the right.
- Ready claim/treasury artwork unfolds once as a straight horizontal hovering banner. Selection, temporary locks and camera visibility do not restart it. Activation burns immediately for 0.42 seconds before the one validated commit.
- New voyage balance: Mothership move 2, Fishing Schooner move 3, Brig range 2, Galleon price 7, Kolonel price 12 and cannon damage 4.
- Hold a cannon target for at least 0.4 seconds, then release to spend two shots: Kolonel fires twice for 4 base damage each; a Mothership needs Second Attack and uses its own cannon damage. The salvo receives one nonrecursive counterattack. Radar outcomes stay anonymous.
- Pay Mothership level rewards of 2 / 4 / 5 / 7 Thors at levels 2 / 3 / 4 / 5. First optical contact with each rival nation grants 5 Thors once. Radar and God's eye do not identify nations or generate encounter income.
- Lead visible attacks/counterattacks/outpost shots/bombs with a 0.25-second camera flight; focus a first encounter in 0.5 seconds. Quintic easing starts and stops at zero speed, independent of distance; manual pan cancels a flight safely.
- Compact the currency/turn plaque. A separate colored top plaque names met captains and uses “Other nations are taking their turns” until contact. Remove central opponent messages.
- Coalesce hover input to one frame, retain sea submission buffers and build local radius forecasts rather than whole-map distance tables. Synchronize world UI with the final camera transform; save directly to UTF-8 without intermediate string/JSON-document clones.

[Validation and compatibility](docs/MILESTONE-0.21.md).

## 0.20 — 2026-10-01 — River Kingdoms

- Automatically show ready claim/treasury scenes at their world targets. Dark-burgundy dry-brush artwork opens downward from a top hinge, floats, rolls inward and burns before capture/rewards commit. Remove yellow capture/loot action icons and the old capture-marker click region.
- Restore ship/town/resource action fans beside the world object. Add Thor coins and prices above purchase icons, and zero-cost labels on level rewards. Center the voyage setup on parchment with six fleet emblems, including Red.
- Share color-specific monuments between Motherships and owned towns: gold dome, silver faceted cap, pink pyramid/white diamond, black cross, dark-green tree and red crystals. Neutral houses, varied roof shades, growing/taller buildings, foreground wheat, detailed tower masonry and woven balloon baskets.
- Pangaea gains connected lake basins, winding arteries, dead-end branches and narrow bypasses. Prioritize two inland villages and one outer village per territory, with more interior resource fish.
- Enable unrestricted narrow/coastal sailing for all hulls in new voyages, and anti-air cannons for Kolonel. Saved optional rules keep older voyages' policies. Curved white trade routes meet ports straddling the town/sea edge, with roads to town centers.

[Validation and limits](docs/MILESTONE-0.20.md), [new assets and generation prompts](docs/ACTION-ART-0.20.md). Start **New game** to generate the new waterways and use the new rule settings; **Continue** preserves the saved voyage.

## 0.19 — 2026-10-01 — Worlds and Clay

- Modal victory after projectile/wreck completion, finite fireworks and saved player-attributed voyage totals.
- Paired clay health/class seals with four damage stages, impact shake/chips and gentle two-flash repair, including towns and structures.
- Generated pictorial harbor/treasury papyri; roll and burn before one validated Core commit.
- Upward action fan beside the selected-object ledger, camera-independent placement and rightmost Info.
- Volumetric hulls, decks, cabins, sails, quay buildings and connected-square clouds; no hidden radar kill class/HP leak.
- Four saved world policies: tiny island chains, original Oceans, river-cut Continents and central Pangaea. Fair coastal settlements and safe fleet routes.
- Bake trees/peaks into shared 2× texture atlases with independent depth/fog anchors. Disable bake viewports and free primitive painters after the first completed render; use premultiplied blending to preserve transparent edges.
- Preserve the organic mesh, saved balance and historical releases. [Evidence](docs/MILESTONE-0.19.md), [art prompts](docs/ACTION-ART-0.19.md).

## 0.18.1 — 2026-10-01 — Readable Charts

- Short counsel followed by separate current-stat, weapon, crew and installed-equipment rows.
- Remove future upgrade catalogues and list only currently available shipyard classes.
- Bound information cards to the viewport; keep title/close fixed, scroll long bodies and retain reading position on unchanged updates.
- Use actual town names, active saved numeric rules and anonymous radar descriptions.
- Keep all 0.18 gameplay/rendering changes; source, notes and complete release copies remain in the authoritative repository.

## 0.18 — 2026-10-01 — Ports and Captains

- Batch waves/fish; retain camera-independent overlays and hulls, with native bobbing and explicit redraw invalidation.
- Front-layer black city names/stats, rightmost Info, God’s eye and victory exploration.
- Saved Boatswain/Captain/Admiral setting, observed-only risk forecasts, focus fire, scouts and economic recovery.
- 15-HP base Mothership; lower prices including 4-Thor fishers and 8-Thor docks.
- Seeded level-scaled settlements, wheat/mills, wooden/stone outposts; level-3 ports cost 6, yield +1 and discount local ships by 20%.
- Cached shortest sea links and stateful progressive lane travel: minimum +2 or +20%, persisted across orders/resume.
- Allied radar coordinates are attackable by all armed units without disclosing identity/health.
- Source and complete release copies live in the authoritative repository. [Evidence and compatibility](docs/MILESTONE-0.18.md).

## 0.17 delivery follow-up — 1 October 2026

- Keep the authoritative source project and README in the Git repository;
  additionally copy every delivered artifact to `releases/0.17/` in that project.
- Add a verified repeatable copy tool, release manifest and README download links.
- Keep a complete unpacked local launch copy while tracking the Windows ZIP,
  source ZIP, notes and screenshots. Ignore the oversized raw runtime in Git.
- Prevent Godot from importing copies and exclude release mirrors from export
  and future source packaging. Previously shipped artifacts remain unchanged.

## 0.17 — 1 October 2026 — Tides and Parchment

- Stage projectile commands in an isolated Core aggregate; commit impact HP and
  veterancy after flight, counterattacks after their own flight, and save only
  completed commands. Preserve surviving entity identities when applying phases.
- Fracture/submerge the Mothership before sinking its fleet; animate other wrecks.
- Depth-sort individual forest trees, ridge peaks, villages and moving ships
  together. Move village flags/mills into their village canvas; retain last
  observed town appearance behind fog. Smooth whole-island shore contours.
- Seed forests and mountain ridges across tile boundaries; vary tree silhouettes.
- Reduce gull populations, decimate animated shore-wave samples, and drift unified
  connected-square cloud shapes above offset shadows.
- Open papyrus width and thickness from the bottom center; show icon-only sectors,
  inner-arc HP/progression and number-anchored damage/healing instead of health bars.
- Draw winding burgundy dashed routes and destination crosses; project separate
  cannon/collection/mortar contours and show selected mortar firing annuli.
- Retain one-step clear-water diagonals and two-step enemy threat costs.
- Increase Cannon Tower range to 4; persist seeded unique captains/town names.
  Keep opponent status visibility-safe and announce the human turn centrally.
- Keep movement/build/resource commands direct instead of cloning combat phases;
  profile actual hover/movement and validate released save fixtures.

See [verification and measured limits](docs/MILESTONE-0.17.md).

## 0.16 — 1 October 2026 — City and Watchtowers

- Correct ship floor/height projection at every heading, depth-sort city buildings,
  retain only visible walls, round the capital's twin keels and unified terraces.
- Bundle a generated static city-ship menu illustration; remove the procedural menu loop.
- Set Cannon Tower guns/sight/radar to 3/2/4; add purchasable radar and tower veterancy.
- Set Kolonel/Galleon/Brig ranges to 2/1/1, Balloon sight to 6 and flagship AA to 2.
- Give level-2+ fortified villages automatic weakest-target fire at range 2,
  level damage and level+1 replies. Manual repair suppresses the automatic shot;
  village yards no longer offer Cannon Towers.
- Retreat wounded AI flagships from known gun coverage before aggressive orders.
- Animate submerged resource/reef fish, add more gulls, faceted cloud shadows and V wakes.
- Keep friendly ships selectable on resource schools; keep terrain and route caches intact.
- Persist/validate the optional new rule sections with legacy defaults.

See [verification and measured limits](docs/MILESTONE-0.16.md).

## 0.15 — 1 October 2026 — Charted Seas

### Changed

- Select 1–4 independent rivals with persistent unique colors; scale the map and
  balance village/treasury/pirate counts across their starting territories.
- Start with 8 Thors and income 3; charge combat fleet upkeep, increase ship
  investment, and keep Mothership income constant across levels.
- Restrict resource collection to the same or adjacent cell. Add a level-2
  Cannon Tower; set Kolonel and Galleon cannon range to two.
- Generate shared flowing seams with reduced distortion; draw connected rounded
  shores with dense groves, larger ridges and translucent cloud shadows.
- Rebuild the floating capital as asymmetric city terraces with a golden-domed
  temple, varied houses, market, faction markings, laundry, residents and carts.
- Unfold papyrus action sectors including locked choices; offer sage-style Info
  with current numerical rules. Right-click cancels from panels as well as the map.
- Aim cannon ships broadside before salvos; swivel mortar barrels separately.
  Show hostile HP in red and turn income at each actual source.

### Fixed

- Reuse an immutable route plan across hover positions; invalidate after commands
  and visibility changes. Separate retained terrain from live selection overlays.
- Bound terrain raster memory and retain per-cell drawing commands.
  Known fog cells change tint; discoveries build new geometry; ordinary movement/hover frames reuse pixels.
- Copy immutable save snapshots on the main thread and serialize/write on a worker.
- Starting a new voyage atomically replaces the sole slot and removes the old
  voyage's backup. Subsequent recovery copies belong to the current voyage.
- Preserve released v1 save identities, geometry, roster defaults and balance.

Validation and performance evidence: `docs/MILESTONE-0.15.md` and
`docs/DEBUG-0.15.md`. Existing voyages retain their saved balance; start a new
voyage to use the new economy.

## 0.14.1 — 30 September 2026

### Changed

- Organize the battle facade by navigation, combat, production and turn lifecycle;
  organize the Godot composition root by selection, commands, view and session.
- Separate reusable Dijkstra traversal from the ship/fog navigation policy. Index
  enemy occupancy and threat zones once per query and memoize visited terrain.
- Move mortar purchase/splash/village bonus, balloon bombs/cooldown, treasury
  weights/rewards and dock/pirate resource rewards to validated balance data.
- Separate fleet drawing and command playback; reuse draw-order and ambient
  vertex buffers; respond to menu resize events instead of every frame.
- Adapt the supplied AI development guide to actual turn-based architecture,
  compatibility rules, tests and specialist ownership. Preserve its original text.

### Fixed

- Keep a good `.bak` when autosaving after recovery from a corrupt primary.
- Keep that backup when New game writes before reading an existing corrupt slot.
- Reject malformed nested save collections and geometry references before indexing.

### Validation

- Clean Godot/.NET build; pure Core remains independent of Godot.
- 1,535 basic checks; 1,195,229 gameplay/navigation/map checks and 8 AI games.
- 20 persistence checks including two pre-refactor release saves; 9 disk-recovery
  assertions exercised through the menu checks.
- Godot smoke: 474,809; graphical combat: 99; graphical effects: 41; sea events:
  108; graphical menu/continue/recovery: 27; rendering optimization: 11.
- Continue in a separate process: 13 checks; exported Windows executable opens
  the menu and renders a generated map successfully.
- Navigation differential oracle preserves all tested step costs and route ties.
- See `docs/MILESTONE-0.14.1.md` for measured allocations and timing limitations.

## 0.14 — 30 September 2026

Living Seas: animated title/color selection, recoverable Continue, irregular hexagon,
weighted treasures, enemy-only mortar splash, updated balloons, village shipyards,
terrain details and naval motion/effects. Full notes: `docs/MILESTONE-0.14.md`.

Earlier playable checkpoints are documented in `docs/MILESTONE-0.11.md`,
`docs/MILESTONE-0.12.md` and `docs/MILESTONE-0.13.md`.
