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

Native desktop `--portrait-layout-test` passed 566 assertions in seven cases:
EN/UK/NL at UI scales 80% and 125%, plus narrow Ukrainian 125% stress. Real native
windows rotate 390x844 to 844x390 and back. Tests check safe-area conversion,
unchanged battle JSON/RNG and camera, restored landscape world anchors, main/shipyard glyph hit
regions and native Build click, counsel scrolling, fresh touch pan after rotation,
settings and menus. These are graphical desktop checks, not physical iPhone
rotation tests. Final captures were visually reviewed; the portrait description
joins the existing fact scroller to keep the selected hull/commands usable.

The existing mobile gesture check passed 15 native assertions. The existing
wide UI regression passed 187 assertions after the counsel restructuring. The package
verifier accepted the preserved IPA with explicit historical landscape policy
and correctly rejected it under the new adaptive policy. Bash syntax checks
passed. Native Mac export and Simulator verification are still pending.

Earlier native failures exposed a stale inflated panel minimum, touch ownership
using a curved control's whole rectangle, and counsel overlapping command glyphs.
These were corrected without weakening assertions. Failure evidence remains in
outside-project outputs/ios-portrait-checks, ios-portrait-diagnostic and
ios-portrait-actions-final.
Final centered evidence is outputs/ios-portrait-centered-final. Earlier 545-check
run is retained separately; the subsequent requested dock has 566 checks.

Physical iPhone installation, real rotation/touch, thermal behavior and iOS
frame pacing cannot be tested on the Windows host. No claim is made for them.
New artifacts will be additive under `releases/v020.8b/ios-portrait-2026-10-10/`.
The separate installation guide explains Windows AltStore personal signing.
