# iOS port — v020.8b, 10 October 2026

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
  relic and nation/ship icons). Menu/Continue/save recovery regression: passed 156,
  including real staged-outpost background suspension and stable camera save.
  The older menu test still expected the pre-v020.8b 239px counsel height; updated
  its bounded-height assertion to the existing 286px layout without changing UI.
- Nine staging integrity/guard checks passed; canonical presets remain unchanged,
  generated SDK pin/public feed verified, reused/unsafe destinations rejected.
- Bash syntax passed on Windows using the bundled Git `sh.exe` (GNU Bash
  5.2.37), and again in the cloud workflow. Native Mac commands run only in CI.

## Actual cloud build / artifact audit

[Run 38016489220](https://github.com/Beygelman/Dev_ancient_naval/actions/runs/38016489220)
passed NativeAOT export, the unsigned physical-device Xcode build, arm64 app and
framework validation, IPA/Xcode ZIP packaging and artifact upload. Native source:
`ac3df4135550188ad4aba1f814e24077b9236633`, Godot 4.7.2 .NET, SDK 8.0.425,
Xcode 26.3 / iOS SDK 26.2. The app deployment target remains iOS 15.

Downloaded artifact ZIP SHA256 was independently checked against GitHub's
artifact digest. `tools/Verify-iOSPackage.py` passed on the original downloaded
IPA/Xcode ZIP and manifest: 21 IPA members, valid arm64 device Mach-O app/framework,
matching PCK hashes, 202 clean PCK resources including 155 whitespace-only C#
script placeholders, balance and UK/NL catalogs. No test/dev/signing files or
C# source content are shipped. NativeAOT's dylib is legitimately mode 0644;
the app entry point is mode 0755. The verifier preserves both and checks their
distinct Mach-O roles. A simulator launch is checked separately below.

Earlier failed executions were retained as diagnostics rather than erased:
the initial stage omitted the required `.sln`; its inclusion was corrected.
The first native link used the runner's default Xcode 16.4 and failed on
Godot template Metal/CoreAnimation symbols. The workflow now explicitly selects
Xcode 26.3; the script rejects iOS SDKs older than 26 before export.
Run `38015959434` passed NativeAOT export and the unsigned Xcode device build,
then exposed a packaging validation error: `lipo -verify_arch` must receive the
binary before the architecture-list option. Corrected without removing the
architecture gate. The successful repeat above produced the IPA/ZIP artifacts.

The first optional Simulator run `38044127875` failed at native engine linking
on an ARM64 host. The exported engine's simulator `libgodot.a` was independently
inspected: all 2,305 Mach-O objects are x86_64, despite the XCFramework plist
listing both architectures. The game's NativeAOT framework is universal, but
the engine is not. The optional smoke workflow now uses `macos-15-intel`;
the validated physical-device ARM64 IPA is unchanged.

## Limits / remaining device gate

No Mac or iPhone is attached to this Windows host; the real Mac build ran in
GitHub Actions. A personally signed IPA, TestFlight link and physical iPhone
installation have not been produced. A Windows native-AOT
link attempt was also blocked by the absent Visual Studio C++ linker; analyzer
and reflection-disabled checks are not substitutes for iOS compilation.

The cloud build is verified; structural audit and simulation are still distinct
from signing and running on the user's physical phone.
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
from the native exports. It contains source and installation/build
instructions; it deliberately contains no executable disguised as an IPA.

Permanent local delivery: `releases/v020.8b/ios-2026-10-10/` in the canonical
project, with a mirror in the chat's outputs. The IPA is 52,822,821 bytes; the
complete Xcode ZIP is 181,962,551 bytes and is intentionally Git-ignored because
it exceeds the hosting regular-file limit. Preserve its local copy and original
SHA256 manifest. The source handoff is generated from committed Git blobs with
sorted paths/fixed ZIP metadata by `tools/Package-iOSSource.py`; unrelated local
files and historical release archives are excluded. The separate installation
guide is updated after the native build; the immutable cloud Xcode ZIP contains
the guide at its earlier source checkpoint, so use the delivered standalone guide.
