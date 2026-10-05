# Reproducing the finalized v020.7 release

## Authorized same-version correction, 5 October 2026

Use `codex/v0207-corrections` or its corrected source ZIP for the narrow changes
documented in `MILESTONE-v020.7-CORRECTIONS.md`. This retains the v020.7 version
and default pirate balance. Do not replace the primary newer v020.8 source with
the complete frozen tree. Compatible fixes are merged there selectively, with
the original files preserved separately.

Run the normal verification below plus native `merchant-corrections`,
`pirates-corrections`, `corrections-ui`, `amphora-corrections`, `story` and
`refinement020` cases. Use fresh disposable save/interface paths. Measure final
frame pacing serially after the last Debug build, without primary builds or
exports running at the same time.

Invoke the packager with **`-Corrected`**, a new outside-project output path and
the reviewed full correction commit. This writes separate
`Ancient_Naval_v020.7_Windows_PLAYER_CORRECTED.zip`,
`Ancient_Naval_v020.7_Source_CORRECTED.zip` and
`SHA256-v020.7-corrected.txt`. Copy verified artifacts additively under
`releases/v020.7/corrected-2026-10-05/`; do not change the frozen archives,
manifest, tag or `latest.json`. Keep extracted source outside the canonical
project: nested source `.cs` files under `releases/` can be compiled as duplicate
types by the default project include patterns.

The older finalized delivery below remains historical evidence.

The frozen gameplay baseline is commit
`b03c5bdc85498f5f5d76f03f4d070adcaa13aa34`. Finalization changes release tooling,
exclusions and documentation; it does not backport the preserved v020.8 work.
Use the verified finalization branch/source archive, never a conflicted `main`
checkout or a Windows player package as development source.

## Prerequisites and source checkpoint

Use Godot **4.7.2 .NET**, its installed Windows export templates and the .NET 8
SDK. Retain `assets/`, import metadata, `scenes/`, `data/`, localization,
`src/Core`, presentation, project references and `tests/`. Core remains pure .NET.
Machine-local NuGet settings, credentials, editor/import caches and generated
assemblies are not source artifacts. `release-evidence/` is a source-archive
sidecar with `.gdignore`, and must remain excluded from the player PCK.
Verify export again from an extracted source archive, not just the checkout. Restoring a clean source archive requires
the normal installed SDK/Godot packages or network access to restore them.

Inspect Git status/version/conflict markers first. Preserve primary user work.
Commit the reviewed finalization sources before creating the final source ZIP;
the archive inventory records both the frozen baseline and the current snapshot
HEAD. Modified tracked files are packaged as their actual working-tree bytes.
New source/evidence must be reviewed and explicitly included, or committed.

## Verification before export

Run from the source root:

```powershell
dotnet build Dev_ancient_naval.csproj
$env:ANCIENT_NAVAL_LEGACY_FIXTURES = (Resolve-Path tests/CoreChecks/Fixtures/Historical).Path
dotnet run --project tests/CoreChecks/CoreChecks.csproj --configuration Release
```

Run the existing native Godot gameplay, save/recovery, localization and version
regressions with disposable explicit `--save-file=` and settings paths. Never
use a real user slot. Record actual exit codes/logs and inspect native captures. The serial
`tools/Check-FinalizationRuntime.ps1 -GodotPath ... -ReportDirectory ...` runner
uses a new outside-project directory and disposable saves/settings.
`-Cases menu -ResumeSavePath ...` copies a historical/current diagnostic fixture
before testing Continue; it never writes to the source fixture.

After the **final Debug build**, run `tools/Check-ReleaseSmoothness.ps1` serially,
with no heavy compilation/test/export in parallel. Preserve failures and the
unchanged frame thresholds; headless runs cannot prove rendering or input.

## Export outside the project

The active Windows Desktop preset must exclude development resources from the
PCK, not merely from the outer ZIP. Required active settings include:

```text
exclude_filter includes releases/*,release-evidence/*,docs/*,tests/*,tools/*,work/*,.local/*
dotnet/include_debug_symbols=false
dotnet/include_scripts_content=false
binary_format/embed_pck=false
encrypt_pck=false
encrypt_directory=false
```

Preserve older preview/Linux presets as historical metadata; this release uses
the explicit `Windows Desktop` preset and output path. Do not invoke the old
`Sync-Release.ps1` against a corrected checkpoint: it updates the original
version manifest and `latest.json`.

```powershell
& ./tools/Export-FinalizedWindows.ps1 `
  -GodotPath 'C:/path/to/Godot_v4.7.2-stable_mono_win64.exe' `
  -OutputPath 'C:/release-work/v0207-export'
```

The output must be a **new** directory outside the source tree. The tool starts
the actual Godot executable in a hidden process, waits for its exit, retains
stdout/stderr/exit status and rejects reported export/build failures. Use the
main executable instead of a console launcher that may outlive its child.
Godot performs the normal self-contained Release build; do not strip .NET DLLs.

The verifier parses the standalone unencrypted PCK directory (formats 2, 3 or
4), normalizes relative or `res://` entry names to the same resource identity,
and rejects development paths, encryption/delta/removal flags, duplicate paths
and invalid bounds, and requires balance, project and external UK/NL catalogs.
English is compiled into the primary catalog. Directory layout follows the
[versioned Godot pack loader](https://github.com/godotengine/godot/blob/4.7.2-stable/core/io/file_access_pack.cpp).
`pck-resources.txt` records the exported resource inventory.

## Player versus source/evidence packages

```powershell
& ./tools/Package-FinalizedRelease.ps1 `
  -PlayerExportPath 'C:/release-work/v0207-export/export' `
  -OutputPath 'C:/release-work/v0207-finalized' `
  -SourceCommit b03c5bdc85498f5f5d76f03f4d070adcaa13aa34
```

Use `-AdditionalSourceFiles @(...)` for reviewed untracked new source/docs.
Default entries cover these three scripts and this procedure. If reviewed
historical save fixtures are newly added, commit them or include each explicitly.
The tool refuses unknown untracked source files rather than omitting them.
Optional `-EvidenceFiles @(...)` accepts explicitly selected diagnostic logs or
captures, not saves, settings, secrets or nested release archives. Supply
`-EvidenceRoot` to preserve relative validation subdirectories and avoid ambiguous
duplicate log basenames; otherwise basenames must be unique. Optional
`-PlayerReadmePath` supplies a minimal approved player README.

If verification is incomplete, use `-Candidate`. It creates explicitly named
`Windows_PLAYER_CANDIDATE` and `Source_CANDIDATE` ZIPs and
`SHA256-v020.7-candidate.txt`, marks the inventory as a candidate and adds a
short player notice. This supports package/source testing without falsely
certifying a final release. Do not tag/promote it as finalized.

The player ZIP contains only:

- `Ancient Naval.exe`;
- `Ancient Naval.pck`;
- `READ_ME_RU.md` (minimal Russian/English launch guidance);
- the complete `data_Dev_ancient_naval_windows_x86_64/` runtime, excluding PDBs.

Outer `docs/`, `Preview map.cmd`, debug symbols and console wrappers are not
copied into the player folder. All other exporter runtime files are retained and
SHA256-compared against their originals. Required Core/game/Godot/.NET runtime
assemblies are checked explicitly; the full runtime is preserved as a unit.

`Ancient_Naval_v020.7_Source_FINALIZED.zip` packages Git-inventoried source,
required assets/config, tests and development docs. It excludes historical
release ZIPs, `.godot`, `bin/`, `obj/`, temporary work, private machine settings,
credentials, OS metadata and unused original sound-library distribution. All
tracked files under `assets/` remain included. Historical images under
`docs/diagnostics/` stay in Git and the original releases rather than being
recursively copied into every source snapshot; selected current captures are
separate validation evidence. `source-excluded-files.txt` records the exclusions.
Archive files are checked against the ordinary GitHub 100 MiB limit; see
[GitHub large-file policy](https://docs.github.com/en/repositories/working-with-files/managing-large-files/about-large-files-on-github). Required project
entrypoints are checked. A per-file SHA256 inventory and PCK/packaging audit
evidence live under `dev-ancient-naval/release-evidence/` in the source ZIP.

Before committing hash-bound evidence, add a `.gitattributes` scoped to the new
delivery directory with `* -text whitespace=cr-at-eol` and `*.log -diff`.
Compare actual Git blob bytes against archive/evidence hashes. If staged before
these attributes, re-add only that new folder with scoped `git add --renormalize`;
never normalize the primary tree or historical releases. The source snapshot
HEAD and later artifact delivery commit are distinct evidence.

Outputs are created with exclusive/new-file semantics. ZIP paths use ordinal
ordering, fixed UTC `2000-01-01` entry timestamps, zero external attributes and
Optimal compression. With identical source/runtime/evidence inputs and the
same PowerShell/.NET compressor, repeated packaging produces identical ZIP
bytes. This does **not** claim different SDK/Godot versions create byte-identical
assemblies; the exported runtime/compiler versions remain release evidence.

Outputs:

```text
Ancient_Naval_v020.7_Windows_PLAYER_CLEAN.zip
Ancient_Naval_v020.7_Source_FINALIZED.zip
SHA256-v020.7-finalized.txt
player/                  # unpacked launch/test copy
evidence/                # inventory and excluded-file record
```

## Verify the actual delivered artifacts

```powershell
& ./tools/Verify-FinalizedRelease.ps1 `
  -PlayerZip 'C:/release-work/v0207-finalized/Ancient_Naval_v020.7_Windows_PLAYER_CLEAN.zip' `
  -SourceZip 'C:/release-work/v0207-finalized/Ancient_Naval_v020.7_Source_FINALIZED.zip' `
  -ManifestPath 'C:/release-work/v0207-finalized/SHA256-v020.7-finalized.txt'
```

Verify actual exported executable startup in EN/UK/NL and map-preview mode;
retain logs/exit status. Extract the source into a new disposable directory and
build/run CoreChecks there. Package layout and hash validation alone do not prove
that a player launches or that a clean extracted project builds.

Copy the verified final package pair, evidence and manifest **additively** into
a new documented finalization folder under the canonical repository's release
tree. Never replace original shipped archives/manifests/`latest.json`. Existing
historical archives must keep their original hashes. A local source commit/tag
may record the verified checkpoint; remote publishing/merging follows the
user-authorized repository workflow and must not overwrite preserved v020.8.
