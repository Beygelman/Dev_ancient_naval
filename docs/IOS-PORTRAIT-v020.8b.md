# iPhone portrait addition — v020.8b, 10 October 2026

Baseline `216a4ca366b637abd92c60cb34d7a9dec60d368f`, canonical source
`C:/__Beygelman/! -11/dev-ancient-naval`. User requests portrait viewing alongside
existing landscape. Version/rules/save schema unchanged; only iOS internal build
number advances to 2083. Historical landscape exports and Windows archives are
immutable. Unrelated audit/nested clone are preserved outside this change.

## Implementation

- Sensor-all handheld policy (6), permitting portrait and both landscape sides.
- Mobile-only portrait canvas 540x960; restore the original landscape baseline.
  Desktop window scaling unchanged. Safe areas continue using screen transforms.
- Adaptive setup/language grids, bounded title/footer, narrow metrics placement,
  and nonoverlapping counsel/compass/relic targets. The subsequent user instruction
  centers portrait menus, docks compact selected-object commands at the bottom,
  places counsel above them, and moves compass/end-turn controls to the top.
  Landscape keeps its existing world-anchored command paper.
- Cancel old gestures on resize; retain active camera/selection/battle. Initial
  startup and LoadScenario still fit the new board normally.
- Package verification defaults to adaptive; historical landscape packages use
  `--orientation=landscape`. Native build and Simulator assert the actual plist.

## Verification / native export in progress

Final Debug build passed with zero warnings/errors. CoreChecks Release exited 0,
including 1,535 baseline assertions, 41 save validation/compatibility checks,
actual historical fixtures and the later policy/economy suites. No Core source,
serialized fields or balance data changed.

Native desktop `--portrait-layout-test` passed 587 assertions in seven cases:
EN/UK/NL at UI scales 80% and 125%, plus narrow Ukrainian 125% stress. Real native
windows rotate 390x844 to 844x390 and back. Tests check safe-area conversion,
unchanged battle JSON/RNG and camera, restored landscape world anchors, main/shipyard glyph hit
regions and native Build click, counsel scrolling, fresh touch pan after rotation,
settings and menus. These are graphical desktop checks, not physical iPhone
rotation tests. Final captures were visually reviewed; the portrait description
joins the existing fact scroller to keep the selected hull/commands usable.

The existing mobile gesture check passed 15 native assertions. The existing
wide UI regression passed 194 assertions including seven native captures after
fixing the relic activation to use its actual local GUI event. The package
verifier accepted the preserved IPA with explicit historical landscape policy
and correctly rejected it under the new adaptive policy. Bash syntax checks
passed. Both final native frame gates passed serially after the last Debug build with
the host GPU idle and World of Tanks closed. Pangaea pan p95 17.195 ms,
interaction p95 17.106 ms / max 55.429 ms; live-fog Oceans pan p95 17.519 ms,
interaction p95 17.831 ms / max 79.076 ms. Existing thresholds were unchanged.
This is desktop renderer evidence, not iPhone GPU performance.

Native Mac export and Simulator verification are still pending. Reviewed native
first source was `ba00ae336bb7efd7f8deeefcb10b380cc5cccdcf`, pushed to main.
Its successful native workflow 38063605761 is superseded before delivery by the
local-pointer relic repair. The final native source/run will be recorded below.

Earlier native failures exposed a stale inflated panel minimum, touch ownership
using a curved control's whole rectangle, and counsel overlapping command glyphs.
These were corrected without weakening assertions. Failure evidence remains in
outside-project outputs/ios-portrait-checks, ios-portrait-diagnostic and
ios-portrait-actions-final.
Final centered evidence is outputs/ios-portrait-centered-final. Earlier 545-check
run is retained separately; the subsequent requested dock has 587 checks including native top-relic confirmation/cancel.

Physical iPhone installation, real rotation/touch, thermal behavior and iOS
frame pacing cannot be tested on the Windows host. No claim is made for them.
New artifacts will be additive under `releases/v020.8b/ios-portrait-2026-10-10/`.
The separate installation guide explains Windows AltStore personal signing.

A fair desktop UI rerun on the idle GPU exposed a pre-existing native-input trap:
upper relic activation queried global mouse position rather than the local GUI
event. Old synthetic input happened to pass when the real pointer matched;
failure diagnostics showed the hovered relic but stale local mouse (-153,492).
The production handler now classifies counter/upper input from the event. Native
wide checks and portrait end-turn confirmation/cancel passed without changing
simulation rules. Failed evidence remains under outputs/ios-portrait-ui-recheck
and ios-portrait-relic-diagnostic; final portrait evidence is
outputs/ios-portrait-final-touch-verified.
