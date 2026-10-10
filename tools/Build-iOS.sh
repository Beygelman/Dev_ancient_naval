#!/usr/bin/env bash
# NativeAOT/Xcode export, deliberately UNSIGNED for later Personal Team signing.
set -euo pipefail
if [[ "$(uname -s)" != "Darwin" ]]; then
  echo "iOS export requires macOS and full Xcode; Windows cannot build this port." >&2
  exit 2
fi
root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
godot="${GODOT_BIN:-/Applications/Godot_mono.app/Contents/MacOS/Godot}"
out="${IOS_OUTPUT:-${TMPDIR:-/tmp}/AncientNaval-iOS-$(date +%Y%m%d-%H%M%S)}"
bundle="${IOS_BUNDLE_ID:-com.beygelman.ancientnaval}"
team="${IOS_TEAM_ID:-AAAAAAAAAA}" # Export placeholder, NOT a signature/certificate.
[[ -x "$godot" ]] || { echo "Set GODOT_BIN to Godot 4.7.2 .NET's executable." >&2; exit 2; }
command -v dotnet >/dev/null || { echo "Install .NET SDK 8.0.425." >&2; exit 2; }
command -v python3 >/dev/null || { echo "Install Python 3." >&2; exit 2; }
xcodebuild -version
xcrun --sdk iphoneos --show-sdk-path
xcrun --sdk iphonesimulator --show-sdk-path
"$godot" --version | grep -F '4.7.2.stable.mono' >/dev/null || { echo "Use matching Godot 4.7.2 .NET." >&2; exit 2; }
[[ ! -e "$out" ]] || { echo "Use a fresh IOS_OUTPUT; existing output is never removed." >&2; exit 2; }
mkdir -p "$out"
python3 "$root/tools/Stage-iOSExport.py" --source "$root" --destination "$out/source" \
  --team-id "$team" --bundle-id "$bundle"
mkdir "$out/xcode"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
cd "$out/source" # Pin SDK lookup to the staged global.json, including Godot's child dotnet calls.
"$godot" --headless --path "$out/source" --editor --import --quit 2>&1 | tee "$out/import.log"
"$godot" --headless --path "$out/source" --export-release "iOS iPhone" "$out/xcode/AncientNaval.zip" \
  2>&1 | tee "$out/export.log"
project="$out/xcode/AncientNaval.xcodeproj"
[[ -d "$project" ]] || { echo "Native export did not create the Xcode project; inspect export.log." >&2; exit 1; }
if grep -E '(^ERROR:|Export failed|Build FAILED\.)' "$out/export.log"; then exit 1; fi
xcodebuild -list -json -project "$project" > "$out/xcode-schemes.json"
scheme="$(python3 -c 'import json,sys; s=json.load(open(sys.argv[1]))["project"]["schemes"]; print("AncientNaval" if "AncientNaval" in s else s[0])' "$out/xcode-schemes.json")"
xcodebuild -project "$project" -scheme "$scheme" -configuration Debug \
  -destination 'generic/platform=iOS' -derivedDataPath "$out/derived" \
  CODE_SIGNING_ALLOWED=NO CODE_SIGNING_REQUIRED=NO CODE_SIGN_IDENTITY='' DEVELOPMENT_TEAM='' build \
  2>&1 | tee "$out/xcode-build.log"
cp "$root/docs/INSTALL-iPHONE-RU.md" "$out/xcode/INSTALL-iPHONE-RU.md"
printf '%s\n' 'UNSIGNED Xcode project: open it on a Mac, choose your own Team and run on iPhone.' \
  'This archive is not a signed IPA and cannot be installed by opening it in Files.' > "$out/xcode/UNSIGNED.txt"
ditto -c -k --keepParent "$out/xcode" "$out/Ancient_Naval_v020.8b_iOS_Xcode_UNSIGNED.zip"
shasum -a 256 "$out/Ancient_Naval_v020.8b_iOS_Xcode_UNSIGNED.zip" > "$out/SHA256-iOS.txt"
echo "Export and unsigned Xcode build passed. Open: $project"
echo "Keep the ENTIRE xcode folder; frameworks/PCK beside the project are required."
