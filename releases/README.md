# Release copies

This directory is part of the authoritative project at
`C:/__Beygelman/! -11/dev-ancient-naval`. Every new delivery is copied here in
addition to the external `outputs` directory. Sources are edited directly in
the repository's `src`, `assets`, `data`, `scenes` and `tests` folders; the root
`README.md` describes the current version.

## Current version: v020.8b — 7 October 2026

- [Windows ZIP](v020.8b/Ancient_Naval_v020.8b_Windows.zip)
- [Source ZIP](v020.8b/Ancient_Naval_v020.8b_Source.zip)
- [Delivery notes](../docs/RELEASE-v020.8b.md), [checksums](v020.8b/SHA256-v020.8b.txt)
- Local launch: `v020.8b/playable/Ancient Naval v020.8b/Ancient Naval.exe`.

Chart-edge waterfalls and black void, revised nation relic commands, compact
dark-ink counsel/advice, localized nations and ship outlines, shrine rituals,
stronger fleet infrastructure and flagship protection. Native frame-pacing
gates passed without relaxed thresholds. Historical archives below are preserved.

## Previous checkpoint: v020.5 — The Painted Voyage

- [Windows ZIP](v020.5/Ancient_Naval_v020.5_Windows.zip)
- [Source ZIP](v020.5/Ancient_Naval_v020.5_Source.zip)
- [Russian notes](v020.5/Ancient_Naval_v020.5_Notes_RU.md), [checksums](SHA256-v020.5.txt)
- Local launch: `v020.5/playable/Ancient Naval.exe`.

Painted rolling voyage setup, independent map areas, Support Brig, construction blueprints, limited direct port connections and natural estuaries. Historical saves retain embedded rules.

## Previous checkpoint: v020.4 — Harbors and Blessings

- [Windows ZIP](v020.4/Ancient_Naval_v020.4_Windows.zip)
- [Source ZIP](v020.4/Ancient_Naval_v020.4_Source.zip)
- [Russian notes](v020.4/Ancient_Naval_v020.4_Notes_RU.md), [checksums](SHA256-v020.4.txt)
- Local launch: `v020.4/playable/Ancient Naval.exe`.

Paid town levels, compact scalable parchment, cosmetic merchant traffic and shared naval icons. Historical saves preserve their economic rules.

## Previous checkpoint: v020.3

- [Windows ZIP](v020.3/Ancient_Naval_v020.3_Windows.zip)
- [Source ZIP](v020.3/Ancient_Naval_v020.3_Source.zip)
- [Russian notes](v020.3/Ancient_Naval_v020.3_Notes_RU.md), [checksums](SHA256-v020.3.txt)
- Local launch: `v020.3/playable/Ancient Naval.exe`.

## Previous checkpoint: v020.2 — corrected build

- [Windows ZIP](v020.2/Ancient_Naval_v020.2_Windows.zip)
- [Source ZIP](v020.2/Ancient_Naval_v020.2_Source.zip)
- [Russian notes](v020.2/Ancient_Naval_v020.2_Notes_RU.md), [checksums](SHA256-v020.2.txt)
- Local launch: `v020.2/playable/Ancient Naval.exe`.

This correction uses a separate folder. Historical 0.20.2 archives and checksums remain unchanged.

## Previous checkpoint: v0.20.2

User-requested version number; this build includes the completed 0.21 work.

- [Windows ZIP](0.20.2/Ancient_Naval_0.20.2_Windows.zip)
- [Source ZIP](0.20.2/Ancient_Naval_0.20.2_Source.zip)
- [Russian notes](0.20.2/Ancient_Naval_0.20.2_Notes_RU.md), [checksums](SHA256-0.20.2.txt)
- [Landscape](0.20.2/Ancient_Naval_0.20.2_landscape.png), [pirate bay](0.20.2/Ancient_Naval_0.20.2_pirate-bay.png), [heavenly blessing](0.20.2/Ancient_Naval_0.20.2_blessing.png)

Local launch: `0.20.2/playable/Ancient Naval.exe`. Keep the whole runtime folder together.

## Previous checkpoint: 0.21

- [Windows ZIP](0.21/Ancient_Naval_0.21_Windows.zip)
- [Source ZIP](0.21/Ancient_Naval_0.21_Source.zip)
- [Russian notes](0.21/Ancient_Naval_0.21_Notes_RU.md), [checksums](SHA256-0.21.txt)
- [Actions](0.21/Ancient_Naval_0.21_actions.png), [claim banner](0.21/Ancient_Naval_0.21_claim.png), [first encounter](0.21/Ancient_Naval_0.21_encounter.png)

Local launch: `0.21/playable/Ancient Naval.exe`. Keep the whole runtime folder together.

## Previous version: 0.20

- [Windows ZIP](0.20/Ancient_Naval_0.20_Windows.zip)
- [Source checkpoint ZIP](0.20/Ancient_Naval_0.20_Source.zip)
- [Russian release notes](0.20/Ancient_Naval_0.20_Notes_RU.md)
- [Checksums](SHA256-0.20.txt)
- [Setup](0.20/Ancient_Naval_0.20_setup.png), [Pangaea](0.20/Ancient_Naval_0.20_pangaea.png), [red fleet](0.20/Ancient_Naval_0.20_red-fleet.png), [claim scroll](0.20/Ancient_Naval_0.20_claim.png)

Local launch: `0.20/playable/Ancient Naval.exe`. Keep the whole runtime folder together.

## Previous version: 0.19

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
./tools/Sync-Release.ps1 -Version '0.20' -OutputsPath 'C:/Users/User/Documents/Codex/2026-09-29/x20/outputs'
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
