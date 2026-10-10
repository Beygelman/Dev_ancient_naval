# iPhone portrait checkpoint — v020.8b, 10 October 2026

Canonical checkout: `C:/__Beygelman/! -11/dev-ancient-naval`. Baseline
`216a4ca366b637abd92c60cb34d7a9dec60d368f`; final production source
`05ff8339fb1af2dde60013785391618038c8110e` on main. Version remains v020.8b;
only internal iOS build metadata advances to 2083. Core, rules, balance and save
schema are unchanged. Unrelated local audit edits and the nested documentation
clone remain untouched.

## Implemented

- Sensor-all orientation policy (6), permitting portrait and both landscape sides.
- Mobile-only 540x960 portrait canvas; restore the original landscape baseline.
  Desktop scaling stays unchanged; safe areas use screen transforms.
- Centered portrait home/setup, adaptive language/choice grids and bounded footer.
- Compact selected-object commands unfold at bottom center, counsel above them;
  compass upper-left, end-turn relic top-center, game menu upper-right. Portrait
  counsel shares its existing fact scroller. Landscape restores world anchors.
- Rotation cancels stale gestures and preserves camera, selection and voyage/RNG.
  Initial/new-world fitting remains separate from resize.
- Curved touch ownership uses actual native hit shapes. Relic counter/upper-part
  activation uses the local GUI event, not a potentially stale system mouse.
- Package verification defaults to adaptive orientation; explicit
  `--orientation=landscape` remains available for historical packages.

## Checks actually run

| Check | Result |
| --- | --- |
| Final Debug build | PASS, 0 warnings/errors |
| CoreChecks Release, explicit historical fixtures | PASS, exit 0; save/default/RNG suites included |
| Native portrait layout/input | PASS, 587 assertions; EN/UK/NL at 80%/125%, plus narrow UK stress |
| Native wide UI regressions | PASS, 194 assertions including seven captures |
| Native mobile gesture regressions | PASS, 15 assertions |
| Bash syntax and package orientation regression | PASS |
| Native arm64 Mac export/Xcode build | PASS, run 38064330615 |
| Actual IPA/Xcode plist/Mach-O/PCK/SHA256 audit | PASS |

The portrait harness uses real desktop windows at 390x844, rotates to 844x390 and
back, checks safe-area coordinates, native command/relic clicks, confirmation
cancel, counsel wheel isolation and unchanged battle JSON/RNG/camera/selection.
Twenty-eight captures were made; the selected delivered examples were visually
reviewed. This is desktop graphical evidence, not physical iPhone rotation.

Both final desktop frame gates passed serially after the final Debug build, with
the GPU idle and World of Tanks closed. Existing thresholds were unchanged:

| Scenario, 1,759 cells / 4 rivals | Pan p95 | Interaction p95 | Interaction max |
| --- | --- | --- | --- |
| Pangaea | 17.105 ms | 17.249 ms | 56.197 ms |
| Live-fog Oceans | 20.371 ms | 18.099 ms | 64.496 ms |

Raw reports are included in the delivery folder. These measure the desktop
renderer; they do not establish iPhone GPU/thermal/frame-pacing performance.

Earlier failed runs found inflated wrapped-panel minima, rectangular curved-hit
shortcuts, overlapping counsel/glyphs and global-mouse relic classification.
Production fixes retained the assertions. Failure evidence remains outside the
repository in thread outputs; final logs are copied into the delivery folder.

## Native package provenance

Native source `05ff833`, workflow
[38064330615](https://github.com/Beygelman/Dev_ancient_naval/actions/runs/38064330615),
artifact 11673369873. Downloaded outer artifact SHA256:
`9371f330bc62f90cf51ca97ccbd235bb7c1fdfd0e369a1efecd6cf29cba3d9ec`.

- IPA: 52,832,821 bytes; SHA256
  `73cf4447a423e3d8a9e1a3eb197fafdda63f1cdac45f5d0f779651c58ed7ddea`.
- Complete Xcode ZIP: 181,988,926 bytes; retained locally outside regular Git's
  size limit. Original native hashes are preserved in `SHA256-iOS.txt`.
- Build 2083, all four plist orientations, ARM64 device executable/framework,
  signatures/runtime and identical clean game PCK passed the strong audit.
- The earlier ba00ae3 portrait export was superseded before delivery.

Simulator workflow [38064936848](https://github.com/Beygelman/Dev_ancient_naval/actions/runs/38064936848)
used the same source and actual final Xcode archive. It was cancelled after the
configured 30-minute job limit. The GitHub check annotation confirms:
"The job has exceeded the maximum execution time of 30m0s". The combined
build/launch step did not complete; no screenshot/diagnostic artifact was uploaded.
Completed job-log retrieval returned BlobNotFound through both the connector and
the direct API redirect. The precise blocked build/boot phase is unknown, so no
Simulator startup pass or game startup failure is inferred. Preserve the annotation
and status under SIMULATOR-STATUS.json; retry this optional check on a fresh Mac
runner before claiming Simulator validation. The device export is independently
built and audited successfully.

## Delivery and limits

Additive delivery: `releases/v020.8b/ios-portrait-2026-10-10/`. The separate
[Russian installation guide](INSTALL-iPHONE-RU.md) explains Windows AltStore
personal signing and the Mac/Xcode alternative. The source handoff is generated
from reviewed committed Git blobs by `tools/Package-iOSSource.py`; generated
caches, local edits, nested clones and historical release packages are excluded.

Physical signing/installation, iPhone rotation/touch, suspension/save recovery,
thermal behavior and iOS frame pacing remain untested. No real save slots were
used by automated tests. Historical Windows/source/landscape-iOS packages and
their manifests are unchanged. No gameplay numbers or new mechanics were added.
