# iOS preparation — v020.8b, 10 October 2026

Canonical source: `C:/__Beygelman/! -11/dev-ancient-naval`, based on main
`eab911185d577b0c5e142b2ad51d478f4769b320`. The game version and gameplay remain
v020.8b. Older Windows/source archives are immutable and have not been replaced.
The pre-existing audit whitespace change and imported nested clone are unrelated
user work and are excluded from this delivery.

## Implemented

- Landscape iPhone-only export preset 9, opaque vector app icon, complete
  project-only Xcode export using the matching Godot 4.7.2 .NET templates.
- Isolated source staging, portable public NuGet feed, .NET SDK 8.0.425 pin,
  Mac/Xcode preflight, unsigned native build validation, ZIP and SHA256 output.
- Manual GitHub Actions macOS workflow with no signing secrets or Apple account
  credentials. Export placeholder Team/identity must be replaced for device use.
- Native touch hit testing, pan/pinch permissions, panel gesture ownership,
  cancellation and safe-area coordinate conversion for device cutouts.
- Stable-command background save; suspension never applies staged battle effects.
- Generated Core/session/tutorial/localization/diagnostic JSON metadata, preserving
  the existing schema, enums, optional initializer defaults, explicit null/zero,
  geometry, statistics, identity, rules and deterministic RNG state.
- iOS excludes desktop runtime-check classes from compilation and player resources.
- Separate [Russian installation guide](INSTALL-iPHONE-RU.md).

## Actual checks

- Debug desktop build: passed, zero warnings/errors.
- Full Core suite: passed, eight complete AI matches and 41 persistence checks
  (including 18 new serialization parity/compatibility cases). Historical fixture
  saves load/resave; four world types compare byte-for-byte with the previous
  reflection-based serializer.
- Core NativeAOT analyzer build: passed, zero warnings/errors. A disposable
  reflection-disabled Core runner passed seven production cases.
- Native Windows generated-JSON probe: passed 10 checks, including Continue,
  backup recovery, sole new-game slot, optional historical defaults, tutorial
  history and bundled UK/NL catalogs. Reflection is explicitly disabled before
  initialization because the Godot desktop host owns its runtime configuration.
- Native Windows synthetic touch/safe-area checks: passed 15. Initial hold test
  exposed a test-clock synchronization error: SceneTreeTimer could inherit a large
  startup delta. The test now waits monotonic elapsed time like production input;
  its exact-once assertions remain unchanged. Original failure log is retained.
- Export staging runs on Windows and excludes caches, releases and the user's
  imported nested clone. Scripts/preset were reviewed against Godot 4.7.2 source.

- Native Windows v020.8b UI regression: passed 187 checks (advice, wheel isolation,
  relic and nation/ship icons). Menu/Continue/save recovery regression: passed 141.
  The older menu test still expected the pre-v020.8b 239px counsel height; updated
  its bounded-height assertion to the existing 286px layout without changing UI.
- Nine staging integrity/guard checks passed; canonical presets remain unchanged,
  generated SDK pin/public feed verified, reused/unsafe destinations rejected.
- Bash syntax/native Mac commands cannot run on this Windows host (no Bash/Mac
  toolchain); the workflow performs Bash syntax validation before native export.

Cloud results will be recorded only after an actual workflow execution.

## Limits / remaining device gate

No Mac or iPhone is attached to this Windows host. A signed IPA, TestFlight link
and actual iPhone installation have not been produced. A Windows native-AOT
link attempt was also blocked by the absent Visual Studio C++ linker; analyzer
and reflection-disabled checks are not substitutes for iOS compilation.

The manual workflow is a reproducible build path, not evidence of a successful
cloud run. Its success must include both NativeAOT export and unsigned Xcode build.
The user must then sign with their Apple Account using Xcode on Mac or
AltStore Classic/AltServer on Windows and validate launch,
Continue, background/resume, paper scrolling, Dynamic Island/home indicator,
long-battle thermal/memory behavior and frame pacing on the physical iPhone 16.

C# iOS support is experimental in Godot. The retained Compatibility renderer
and existing bounded terrain/object caches are preserved; no mobile performance
claim or new threshold is made without device measurements.

## Outputs

`tools/Build-iOS.sh` writes a fresh directory outside source, including logs,
`xcode/AncientNaval.xcodeproj`, required sibling PCK/frameworks, and
`Ancient_Naval_v020.8b_iOS_Xcode_UNSIGNED.zip` plus `SHA256-iOS.txt` after success.
Preserve the entire Xcode folder. An unsigned Xcode ZIP is not an iPhone installer.
The same successful native build packages `Ancient_Naval_v020.8b_iPhone_UNSIGNED.ipa`
from the physical-device `.app`, checks its arm64 executable and hashes it. This
IPA needs personal signing: AltStore Classic/AltServer provide a Windows route,
documented in the installation guide. It is not an App Store/TestFlight package.

The Windows-host source handoff is named `iOS_SOURCE_HANDOFF`, to distinguish it
from those future native exports. It contains source and installation/build
instructions; it deliberately contains no executable disguised as an IPA.
