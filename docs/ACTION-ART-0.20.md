# Action artwork — 0.20

Edited with the built-in `image_gen` tool under the [imagegen skill](C:/Users/User/.codex/skills/.system/imagegen/SKILL.md), 1 October 2026. Both PNGs have transparent backgrounds. The original 0.19 art is retained.

| Action | Source composition | New bundled asset |
|---|---|---|
| Village claim | `assets/ui/harbor-claim-0.19.png` | `assets/ui/harbor-claim-0.20.png` |
| Treasury discovery | `assets/ui/treasury-awakening-0.19.png` | `assets/ui/treasury-awakening-0.20.png` |

Harbor edit prompt: preserve the wide composition and silhouette, ship on the left, sail and crew, harbor town on the right, sea along the bottom. Change only drawing style to a monochrome dry-brush sketch in dark burgundy (#562332 / #6b2938), with broken bristle marks, hatching and transparent gaps. No full-color painting, paper background, frame or text. Keep details readable at approximately 300 screen pixels.

Treasury edit prompt: preserve the chest on the left, standing carved relic on the right, waves, fish, plants and rocks; retain the original wide layout and shapes. Use the same dark-burgundy dry-brush sketch, rough broken edges and fine hatching on transparent ground. No paper, frame, text or full-color shading.

The local original tool outputs are retained in `C:/Users/User/.codex/generated_images/01a0e7ff-74f7-74f2-a9e1-11850add3fac/exec-1ee8a497-e316-4305-8966-4b3e575ef99d.png` and `exec-f1175c4f-7dc0-4f24-b3b8-46b384e8b0cf.png`. Versioned copies above are the assets actually loaded by the game.

`ActionPapyrus` maps each illustration onto a semicircular paper strip using explicit convex quads. It opens equally to both sides from a top hinge, floats gently, rolls inward and burns. The parchment uses the ship fan's paper/bronze palette. The drawing, animation and decorative sparks never consume the battle random generator.

Currency coins and fleet monuments are native vector geometry, rather than generated bitmap assets. The six monuments share floor/height projection across ships, captured towns and the fleet chooser.
