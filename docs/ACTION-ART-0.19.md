# Action friezes — 0.19

Two transparent original bitmap illustrations generated with the built-in `image_gen` tool,
using the **imagegen** skill. No existing game screenshot or third-party artwork was edited.
The PNGs are bundled directly in the authoritative repository and Windows PCK.
No post-generation cropping, recoloring or alpha modification was performed.
`ActionPapyrus` fits the original aspect ratio in its native clipped ink area over procedural paper.

## Harbor claim

Asset: [harbor-claim-0.19.png](../assets/ui/harbor-claim-0.19.png).
Original generated file: `exec-7c7954c7-2d40-45e0-ab3a-d6711081f5d0.png`.
`transparent_background=true`.

Prompt:

> Use case: illustration-story. Asset type: horizontal hand-painted ink frieze for an Ancient Naval game action papyrus. Create one isolated very wide flat scene in ancient Egyptian wall painting / papyrus dry-brush style, transparent background, no paper behind it, no frame, no modern written words, no Latin lettering. Warm dark umber and worn ochre brushwork with small muted turquoise accents. A detailed stylized ancient wooden naval ship at left sails beside a tiny coastal town at right: clustered beige stepped houses, a small quay, a flag being raised by sailors; depict a peaceful claim of the harbor, in clear symbolic side profile, rows of flowing water strokes underneath, decorative invented hieroglyph-like pictograms along bottom. Brush strokes should be dry, imperfect, elegant, like a believable ancient painted register. Composition very readable at 320 by 95 pixels, approximately 3.5:1 wide aspect ratio, one continuous narrative scene, full length, no UI elements, no large empty margin, no photorealism, no perspective screenshot, no gradients or glowing lighting. Ship and town grounded in same flowing water line. Make the central key silhouettes bold and refined, opaque pigment strokes over genuine transparent alpha.

## Sunken treasury

Asset: [treasury-awakening-0.19.png](../assets/ui/treasury-awakening-0.19.png).
Original generated file: `exec-13ff0e4b-991b-4aab-862b-9609ab0bd4f1.png`.
`transparent_background=true`.

Prompt:

> Use case: illustration-story. Asset type: horizontal dry-brush painted ancient Egyptian narrative frieze used across the full width of a small game papyrus. One transparent-background isolated illustration, no actual paper or border, no modern text, no Latin letters, no UI elements. Very wide horizontal register, around 3.5:1 ratio, clear at 320x95 pixels. Match Ancient Naval muted antique ink style: warm dark umber, ochre, beige pigment and restrained turquoise. Underwater scene: left a clearly recognizable open ancient treasure chest; center/right a carved standing stone artifact with invented Egyptian-like pictogram inscriptions, activated by a subtle golden circular emblem on its face; small fish and coral around the seabed, a few flowing symbolic wave strokes across the upper register to indicate that the scene is below the sea. Flat side-profile Egyptian manuscript composition with imperfect dry brush lines, opaque pigments on genuine transparent alpha, elegant worn painted details and bold central silhouettes. Show chest, activated inscribed stone and fish clearly as one continuous ancient pictorial story. No people diving in scuba, no photorealism, no perspective 3D scene, no neon glow, no frame, no huge empty margin.

## Interaction

The story opens only when Core says the crew may capture/plunder. Both the normal action
icon and the wide illustration invoke the same command wrapper. During its 0.85-second
roll/burn, `Busy` blocks other orders and Core ownership, treasury rewards, crew actions
and voyage totals remain unchanged. Main revalidates the captured battle/target after
animation, then executes the usual command; treasure impact staging remains intact.
