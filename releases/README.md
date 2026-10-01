# Release copies

This directory is part of the authoritative project at
`C:/__Beygelman/! -11/dev-ancient-naval`. Every new delivery is copied here in
addition to the external `outputs` directory. Sources are edited directly in
the repository's `src`, `assets`, `data`, `scenes` and `tests` folders; the root
`README.md` describes the current version.

## Current version: 0.19

- [Windows ZIP](0.19/Ancient_Naval_0.19_Windows.zip)
- [Source checkpoint ZIP](0.19/Ancient_Naval_0.19_Source.zip)
- [Release notes in Russian](0.19/Ancient_Naval_0.19_Notes_RU.md)
- [SHA256 checksums](SHA256-0.19.txt), with paths relative to this directory
- [Pangaea](0.19/Ancient_Naval_0.19_pangaea.png), [fleet](0.19/Ancient_Naval_0.19_fleet.png), [clay health seals](0.19/Ancient_Naval_0.19_clay-stages.png), [victory](0.19/Ancient_Naval_0.19_victory.png)

For local play, use `0.19/playable/Ancient Naval.exe`. The complete folder includes
the PCK, bundled .NET runtime and diagnostics. Keep these files together.
That raw launch folder is local-only in Git, because the executable is larger
than GitHub's 100 MiB limit. The complete ZIP is tracked instead.
[GitHub limits](https://docs.github.com/en/repositories/working-with-files/managing-large-files/about-large-files-on-github).

## Updating a release copy

Package and verify the new version first, then run from the repository root:

```powershell
./tools/Sync-Release.ps1 -Version '0.19' -OutputsPath 'C:/Users/User/Documents/Codex/2026-09-29/x20/outputs'
```

The tool copies all matching artifacts and every unpacked runtime file,
verifies SHA256, checks archive/manifest integrity and writes `latest.json`.
An existing version is immutable: identical files are reused, differing files
are refused. Use a new version for gameplay updates. Historical source ZIPs
remain release checkpoints; later README/workflow edits live in the repository.

Review the source and release changes in your Git client, commit them and push
to upload them to GitHub. Uncommitted local files do not update the remote.
This copy tool does not commit or push.

`.gdignore` prevents Godot from importing these copies. Exclude this whole
directory from source ZIP generation so archives never contain themselves.
