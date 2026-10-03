# v020.4 — Harbors and Blessings

3 October 2026. The v020.3 release remains an immutable checkpoint.

New voyages pay for town levels: 5, 8, 12 and 16 Thors to reach levels 2–5. The town upgrade occupies the central command sector and consumes its construction action. City HP, income, shipyard unlocks and fleet capacity update immediately. AI uses the same legal paid commands. Historical saved voyages retain their automatic growth policy. Each new flagship level pays exactly 2 Thors, once at resource progression; choosing a blessing does not pay again.

Active repair commits the whole object's remaining action budget. A repaired ship cannot move, attack, collect, build, install a radar/mortar or receive a construction gift until its next turn. A repaired town cannot build, fortify, open a port or buy a level. Core validates these restrictions; UI shortcuts cannot bypass them.

Interface scale is adjustable from 80% to 125%, on the title and in the game menu, and persists independently of saved battles. Native CanvasLayer transforms scale controls and their hit testing while world coordinates remain unchanged. Ship/city anchors and target input holes are converted back to logical UI coordinates. Vertical modal parchments use a wider 340-pixel paper family, never exceed 60% of the screen height and scroll their contents on small viewports.

Merchant boats use the existing owned port graph, cosmetic seeded randomness and three small skins. Each connected port schedules departures 12–20 seconds apart; boats move at 12 world pixels/second toward another reachable owned city. The population is bounded to 64 and resets obsolete routes when port/hazard topology changes. Fog hides traffic outside optical sight. It neither consumes fleet capacity nor changes resources, damage, ships or simulation randomness. Merchant drawing is retained; only transforms advance during travel.

Cosmetic rivers are more frequent, wind around actual town/peak footprints and reserve accepted channels before further placement. Independent rivers never cross; a designated branch may join a trunk at a shared downstream reach. Bank shading follows bends. All river geometry remains nonfunctional and clipped to land. Early houses are taller, reducing the contrast between low and high town levels. The church rests on an irregular round plaza. The port connector is a single beige road below houses and mills and meets the actual shore pier.

Clay badges and shipyard commands draw the same reusable vessel/structure motifs. Fishing uses a fish, mortar cups differ from crenellated defensive towers, and veteran amphorae gain an elongated neck and two burgundy painted stripes. Veteran hull decoration uses subtle stern ribbons rather than an oversized attached deck structure.

English, Ukrainian and Dutch descriptions cover paid growth, action locks, reward amounts and interface scaling. Evidence from functional native checks and the mandatory final serial frame-pacing gates is recorded in `docs/diagnostics/v020.4/Validation.md`. Choose New game for the revised economic rules.
