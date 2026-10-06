# v020.7b — portable Windows delivery, 6 October 2026

The user subsequently requested a playable transferable ZIP of the completed
v020.7b source. The identity and gameplay remain unchanged. The playable package
is a verification candidate, not a certified final release. Baseline main is
`8fd3273c5e394694a61e78d764cfd6ab0c30941e`; SOURCE-INVENTORY.json records the actual
snapshot bytes separately from the later artifact-delivery commit.

## Files and launch

- `releases/v020.7b/Ancient_Naval_v020.7b_Windows_PLAYER_CANDIDATE.zip`
- `releases/v020.7b/Ancient_Naval_v020.7b_Source_CANDIDATE.zip`
- `releases/v020.7b/SHA256-v020.7b-candidate.txt`
- Ignored local launch copy: `releases/v020.7b/playable/Ancient Naval.exe`.

Extract the entire Windows archive and start Ancient Naval.exe. Keep its PCK
and complete data_Dev_ancient_naval_windows_x86_64 directory beside it. The
export is self-contained; Godot and a separate .NET installation are not needed.
Continue uses the existing Ancient Naval user-data save slot. This is a portable
program folder, not a relocation of the user's save folder.

## Source and packaging boundaries

Release work uses an external snapshot of the reviewed tracked working-tree
bytes. Local project.godot setting reordering and the audit document's trailing
blank-line edit are preserved. The unrelated nested repository under
docs/dev-ancient-naval is not compiled or shipped and is left on disk untouched.
Historical ZIPs, backups and the user's live saves are untouched.

The scripts now accept explicit `-Version v020.7b`, defaulting to the frozen
v020.7 for historical workflows. The active Windows preset omits debug symbols,
console wrappers and development folders inside the PCK. Packaging preserves
every exported runtime file except PDBs. Player outer contents are restricted to
the executable, PCK, short player README and complete runtime folder.

The two documented pre-refactor save fixtures had been absent from main. They
are restored unchanged from the verified finalized v020.7 source archive;
their original README and SHA256 manifest preserve provenance. Tests read them
only, and use disposable save/preferences/capture paths.

## Repeat the procedure

Use Godot 4.7.2 .NET and .NET 8, fresh outside-project directories, and a reviewed
source snapshot. Run the relevant build/Core/save/native checks; measure final
native frames serially after the final Debug build, with other games stopped.

```powershell
./tools/Export-FinalizedWindows.ps1 -Version v020.7b -GodotPath $godot -OutputPath $freshExport
./tools/Package-FinalizedRelease.ps1 -Version v020.7b -Candidate -PlayerExportPath "$freshExport/export" -OutputPath $freshPackages -SourceCommit 8fd3273c5e394694a61e78d764cfd6ab0c30941e
./tools/Verify-FinalizedRelease.ps1 -Version v020.7b -PlayerZip "$freshPackages/Ancient_Naval_v020.7b_Windows_PLAYER_CANDIDATE.zip" -SourceZip "$freshPackages/Ancient_Naval_v020.7b_Source_CANDIDATE.zip" -ManifestPath "$freshPackages/SHA256-v020.7b-candidate.txt"
```

Use AdditionalSourceFiles for reviewed untracked source/doc additions before
their delivery commit. Stable ZIP metadata/order and per-file inventories are
retained. Export reproducibility is procedural, not a claim of byte-identical
assemblies across toolchains. Never overwrite old release folders or use the
historical Sync-Release shortcut for this additive delivery.

## Validation

The original broad gameplay/native/fog/UI checks are recorded in
MILESTONE-v020.7b.md. Gameplay files have not changed in this packaging task.

- External source Debug build: zero warnings/errors. Full current Core suite
  passes, including 1,199,448 Thor/progression/combat assertions, eight AI matches
  and the remaining generation, navigation, economy and rule suites.
- Focused PersistenceChecks: 23 checks, including both original historical
  fixtures; exact load/resave behavior passes. No real player save was used.
- Fresh standalone Release export succeeds; audited PCK has 197 resources,
  Godot 4.7.2 format 4. Windows metadata reports product v020.7b, version 0.20.7.2.
- First headless asset import crashed at editor shutdown after imports completed;
  a serial repeat completed with exit 0. The actual export is independently checked.

### Current native performance limitation

The first Pangaea gate failed while another Godot game was open. After the user
stopped it, two further serial runs still failed p95 thresholds, with no build
or heavy tests running during measurements. The final monitored repeat records
pan p95/max 25.469/32.916 ms and interaction p95/max 23.128/62.303 ms. Required
pan/interaction p95 is <=25 ms; max bounds remain 80/100 ms. Hover max was 0.492 ms.
The gate stopped at Pangaea; Oceans live-fog approval was not completed this turn.

An independent review compared 543 relevant source/config/assets files to the
validated checkpoint; only project.godot key ordering differed semantically
unchanged. Core command/cache costs, renderer/driver/map/draw count match.
The current native submission stalls show no GC or shader compile in the slow
frames. GPU telemetry during the monitored run stayed mostly P5 at 435–735 MHz
and about 9–10 W while AC power was connected. This suggests host/driver queue
or power/scheduling behavior but does not establish a definite cause. Thresholds
and gameplay were not changed. All failures are retained rather than relabeled
as passes. Candidate status remains until a fair complete frame gate passes.

Native Android/touch, other GPUs, and a full human campaign are not certified by
this delivery. Release debug-gated test flags do not run native regression nodes;
actual executable startup captures are separate from source runtime regressions.

### Delivered archive checks

Both raw export and the actual ZIP-extracted player passed EN/UK/NL and map-preview
startup: 8 native captured frames per case, exit 0, no reported errors. Selected
title/map captures were visually inspected; the title shows v020.7b 06.10.2026.
The actual ZIP-extracted source builds with zero warnings/errors without copying
machine-local NuGet settings. These are startup/build checks, not a manual campaign
or a release-player Continue interaction test.

Player contains 190 files and PCK contains 197 resources; source contains 1,154
entries. Runtime files in the canonical local launch copy match the exporter
byte-for-byte. No docs, tests, tools, preview launcher, console wrapper or PDBs
are shipped in the player. The runtime itself is complete.

| Artifact | Bytes | SHA256 |
| --- | ---: | --- |
| Windows PLAYER_CANDIDATE | 87,145,075 | `8758e3eaca3948920bbccaf794a7426d576295118bc316fad0ea5ff270a90a32` |
| Source CANDIDATE | 16,111,648 | `2d36e74ddedaece32de3d70df63207146d55e3535934fd80b5ad803f665fba9b` |

Complete packages are under the canonical releases/v020.7b directory and mirrored
in the chat outputs directory. The source ZIP records the source snapshot before
this delivery-results appendix and later artifact commit; its inventory binds its
own exact bytes. DELIVERY-VALIDATION.json and evidence retain the actual results.
