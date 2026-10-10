#!/usr/bin/env bash
# Optional startup check of an exported NativeAOT player. Never uses a real phone
# or an existing Simulator's data; only the device created below is removed.
set -euo pipefail
[[ "$(uname -s)" == Darwin ]] || { echo "This check requires macOS/Xcode." >&2; exit 2; }
artifact="${IOS_ARTIFACT_DIR:?Set IOS_ARTIFACT_DIR to the downloaded iOS artifact directory.}"
out="${IOS_SMOKE_OUTPUT:-${TMPDIR:-/tmp}/AncientNaval-iOS-smoke-$(date +%Y%m%d-%H%M%S)}"
archive="$artifact/Ancient_Naval_v020.8b_iOS_Xcode_UNSIGNED.zip"
[[ -f "$archive" && ! -e "$out" ]] || { echo "Expected the complete Xcode ZIP and a fresh output path." >&2; exit 2; }
mkdir -p "$out/export"
ditto -xk "$archive" "$out/export"
project="$out/export/xcode/AncientNaval.xcodeproj"
[[ -d "$project" ]] || { echo "The archive must contain the entire exported xcode folder." >&2; exit 1; }
xcodebuild -version | tee "$out/xcode-version.log"
uname -m | tee "$out/host-architecture.log"
xcodebuild -list -json -project "$project" > "$out/xcode-schemes.json"
scheme="$(python3 -c 'import json,sys; a=json.load(open(sys.argv[1]))["project"]["schemes"]; print("AncientNaval" if "AncientNaval" in a else a[0])' "$out/xcode-schemes.json")"

choose_runtime() {
  xcrun simctl list runtimes --json > "$out/runtimes.json"
  python3 - "$out/runtimes.json" <<'PY'
import json, sys
runtimes = [r for r in json.load(open(sys.argv[1]))["runtimes"]
            if r.get("isAvailable") and r["identifier"].startswith("com.apple.CoreSimulator.SimRuntime.iOS-")
            and int(r["version"].split(".")[0]) >= 18]
if runtimes:
    print(max(runtimes, key=lambda r: tuple(int(n) for n in r["version"].split(".")))["identifier"])
PY
}
runtime="$(choose_runtime)"
if [[ -z "$runtime" ]]; then
  # The caller/workflow bounds this potentially large download. Do not download
  # a platform when the runner already has an eligible iPhone 16 runtime.
  xcodebuild -downloadPlatform iOS 2>&1 | tee "$out/runtime-download.log"
  runtime="$(choose_runtime)"
fi
[[ -n "$runtime" ]] || { echo "No available iOS 18+ Simulator runtime." >&2; exit 1; }
xcrun simctl list devicetypes --json > "$out/device-types.json"
device_type="$(python3 -c 'import json,sys; a=[d for d in json.load(open(sys.argv[1]))["devicetypes"] if d["name"] == "iPhone 16"]; print(a[0]["identifier"] if a else "")' "$out/device-types.json")"
[[ -n "$device_type" ]] || { echo "This Xcode does not provide iPhone 16." >&2; exit 1; }

udid=""; bundle=""; console_pid=""
cleanup() {
  local status=$?
  trap - EXIT
  if [[ -n "$udid" ]]; then
    if [[ -n "$bundle" ]]; then xcrun simctl terminate "$udid" "$bundle" >/dev/null 2>&1 || true; fi
    xcrun simctl shutdown "$udid" >/dev/null 2>&1 || true
    xcrun simctl delete "$udid" >/dev/null 2>&1 || true
  fi
  if [[ -n "$console_pid" ]]; then kill "$console_pid" >/dev/null 2>&1 || true; wait "$console_pid" 2>/dev/null || true; fi
  exit "$status"
}
trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM
udid="$(xcrun simctl create "AncientNaval-Smoke-$(date +%s)-$$" "$device_type" "$runtime")"
[[ "$udid" =~ ^[A-Fa-f0-9-]{36}$ ]] || { echo "Invalid newly created Simulator ID." >&2; exit 1; }
printf 'Owned Simulator: %s\nRuntime: %s\n' "$udid" "$runtime" > "$out/simulator.log"
xcrun simctl boot "$udid"
xcrun simctl bootstatus "$udid" -b 2>&1 | tee -a "$out/simulator.log"
xcodebuild -project "$project" -scheme "$scheme" -configuration Debug \
  -sdk iphonesimulator -destination "id=$udid" -derivedDataPath "$out/derived" \
  ONLY_ACTIVE_ARCH=YES CODE_SIGNING_ALLOWED=NO CODE_SIGNING_REQUIRED=NO CODE_SIGN_IDENTITY='' DEVELOPMENT_TEAM='' build \
  2>&1 | tee "$out/simulator-build.log"
apps=("$out/derived/Build/Products/Debug-iphonesimulator/"*.app)
[[ ${#apps[@]} -eq 1 && -d "${apps[0]}" ]] || { echo "Expected one Simulator application." >&2; exit 1; }
app="${apps[0]}"
bundle="$(/usr/libexec/PlistBuddy -c 'Print :CFBundleIdentifier' "$app/Info.plist")"
executable="$(/usr/libexec/PlistBuddy -c 'Print :CFBundleExecutable' "$app/Info.plist")"
[[ "$(/usr/libexec/PlistBuddy -c 'Print :DTPlatformName' "$app/Info.plist")" == iphonesimulator ]]
xcrun lipo "$app/$executable" -verify_arch "$(uname -m)"
[[ -d "$app/Frameworks/Dev_ancient_naval.framework" ]]
framework_executable="$(/usr/libexec/PlistBuddy -c 'Print :CFBundleExecutable' "$app/Frameworks/Dev_ancient_naval.framework/Info.plist")"
xcrun lipo "$app/Frameworks/Dev_ancient_naval.framework/$framework_executable" -verify_arch "$(uname -m)"
xcrun simctl install "$udid" "$app"
installed_app="$(xcrun simctl get_app_container "$udid" "$bundle" app)"
# --console captures actual engine/C# startup diagnostics, not just build output.
# Create the log before forking so the first PID read cannot race its redirection.
: > "$out/console.log"
xcrun simctl launch --console "$udid" "$bundle" > "$out/console.log" 2>&1 &
console_pid=$!
app_pid=""
for attempt in {1..30}; do
  app_pid="$(python3 - "$out/console.log" "$bundle" <<'PY'
import pathlib, re, sys
text = pathlib.Path(sys.argv[1]).read_text(errors="replace")
match = re.search(r"(?m)^" + re.escape(sys.argv[2]) + r":\s*(\d+)\s*$", text)
print(match[1] if match else "")
PY
)"
  if [[ -z "$app_pid" ]]; then
    # A console attached to a pipe may buffer simctl's PID banner. Match the
    # exact installed executable instead of assuming a particular log format.
    app_pid="$(ps -axo pid=,comm= | python3 -c 'import sys; expected=sys.argv[1]; a=[line.strip().split(None,1) for line in sys.stdin]; print(next((p[0] for p in a if len(p)==2 and p[1]==expected),""))' "$installed_app/$executable")"
  fi
  [[ -z "$app_pid" ]] || break
  kill -0 "$console_pid" 2>/dev/null || { cat "$out/console.log"; echo "Simulator launch terminated." >&2; exit 1; }
  sleep 1
done
[[ "$app_pid" =~ ^[0-9]+$ ]] || { cat "$out/console.log"; echo "No launched application PID." >&2; exit 1; }
sleep 15
kill -0 "$console_pid"
ps -p "$app_pid" -o pid=,comm= > "$out/process.log"
xcrun simctl io "$udid" screenshot "$out/start-menu.png"
[[ -s "$out/start-menu.png" ]]
if grep -Ei '(^|[[:space:]])(ERROR:|SCRIPT ERROR:|Unhandled exception|FATAL:)|System\.(TypeInitialization|InvalidOperation)Exception|Could not load.*assembly|Could not.*script' "$out/console.log"; then
  echo "Native player reported a startup failure." >&2; exit 1
fi
ps -p "$app_pid" -o pid=,comm= >> "$out/process.log"
printf '%s\n' 'PASS: exported NativeAOT Simulator app remained alive for 15 seconds without detected startup errors.' \
  'Review start-menu.png separately. This is not a physical iPhone or frame-pacing test.' | tee "$out/result.txt"
