"""Retain a gated experimental Windows export and its exact tracked source."""
from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import shutil
import subprocess
import zipfile


def sha(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def retain(player: Path, evidence: Path, run_id: str) -> None:
    repo = Path(__file__).resolve().parent.parent
    version = "v020.9x2"
    release = repo / "releases" / version
    release.mkdir(parents=True, exist_ok=True)
    source_commit = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=repo, text=True).strip()
    archive_name = f"Ancient_Naval_{version}_Windows.zip"
    archive = player / archive_name
    digest = sha(archive)
    if f"{digest}  {archive_name}" not in (player / "SHA256.txt").read_text(encoding="utf-8-sig"):
        raise ValueError("Windows export checksum mismatch")
    with zipfile.ZipFile(archive) as packed:
        names = packed.namelist()
        if len(names) != len(set(names)) or packed.testzip() is not None:
            raise ValueError("Duplicate files or CRC failure in player")
        for name in names:
            parts = Path(name).parts
            if name.startswith(("/", "\\")) or "\\" in name or ":" in name or ".." in parts:
                raise ValueError(f"Unsafe player entry: {name}")
            if {".git", "docs", "tools", "tests", "releases"}.intersection(parts) or name.endswith(".pdb"):
                raise ValueError(f"Development files in player: {name}")
        for required in ("Ancient Naval.exe", "Ancient Naval.pck",
                         "data_Dev_ancient_naval_windows_x86_64/Dev_ancient_naval.dll",
                         "data_Dev_ancient_naval_windows_x86_64/coreclr.dll"):
            if required not in names:
                raise ValueError(f"Missing runtime: {required}")
    destination = release / archive_name
    if destination.exists() and sha(destination) != digest:
        raise FileExistsError("Refusing to replace a different released player")
    shutil.copyfile(archive, destination)

    spec = importlib.util.spec_from_file_location("source_pack", repo / "tools/Package-iOSSource.py")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    _, entries = module.tree(repo, source_commit)
    # The sparse checkout already materializes all selected source directories.
    # Read immutable blobs and verify working files are not substituted.
    missing = sorted({entry["git_blob"] for entry in entries if not (repo / entry["path"]).exists()})
    if missing:
        subprocess.run(["git", "-c", "fetch.negotiationAlgorithm=noop", "fetch", "--no-tags",
                        "--no-write-fetch-head", "--recurse-submodules=no", "--filter=blob:none",
                        "origin", "--stdin"], cwd=repo, input=("\n".join(missing) + "\n").encode(), check=True)
    contents = module.blobs(repo, entries)
    source = release / f"Ancient_Naval_{version}_Source.zip"
    if source.exists():
        raise FileExistsError("Refusing to overwrite released source")
    with zipfile.ZipFile(source, "x", compression=zipfile.ZIP_DEFLATED, compresslevel=9) as packed:
        for entry in entries:
            info = zipfile.ZipInfo("dev-ancient-naval/" + entry["path"], (2026, 10, 17, 0, 0, 0))
            info.compress_type = zipfile.ZIP_DEFLATED
            info.external_attr = int(entry["git_mode"], 8) << 16
            packed.writestr(info, contents[entry["path"]])
    with zipfile.ZipFile(source) as packed:
        if packed.testzip() is not None:
            raise ValueError("Source CRC failure")
    inventory = {"source_commit": source_commit, "files": entries}
    (release / "SOURCE-INVENTORY.json").write_text(json.dumps(inventory, indent=2) + "\n")

    template = (repo / "tools/Install-ExperimentalRelease.template.ps1").read_text()
    installer = release / "Install-PlayableRelease.ps1"
    installer.write_text(template.replace("__WINDOWS_SHA256__", digest), encoding="utf-8-sig")
    # Do not require a historical binary directory in CI's sparse checkout.
    command = (
        '@echo off\r\nsetlocal\r\n'
        'powershell.exe -NoLogo -NoProfile -Command "'
        "$ErrorActionPreference='Stop'; [Net.ServicePointManager]::SecurityProtocol=[Net.SecurityProtocolType]::Tls12; "
        "$installer=Join-Path ([IO.Path]::GetTempPath()) ('AncientNaval-'+[Guid]::NewGuid().ToString('N')+'.ps1'); "
        "try { $client=New-Object Net.WebClient; try { $client.DownloadFile('"
        f"https://raw.githubusercontent.com/Beygelman/Dev_ancient_naval/codex/ancient-naval-v020.9x2/releases/{version}/Install-PlayableRelease.ps1"
        "',$installer) } finally { $client.Dispose() }; "
        f"if ((Get-FileHash -LiteralPath $installer -Algorithm SHA256).Hash.ToLowerInvariant() -ne '{sha(installer)}') "
        "{ throw 'Installer checksum mismatch.' }; & powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File $installer -Launch; "
        "if ($LASTEXITCODE -ne 0) { throw 'Game preparation failed.' } } "
        "finally { if (Test-Path -LiteralPath $installer) { Remove-Item -LiteralPath $installer -Force } }"
        '"\r\nif errorlevel 1 (\r\n  pause\r\n  exit /b 1\r\n)\r\nendlocal\r\n'
    )
    (release / "Install_Ancient_Naval_v0209x2.cmd").write_bytes(command.encode("ascii"))
    copied = []
    for path in sorted(evidence.rglob("*.png")):
        target = release / "screenshots" / path.name
        target.parent.mkdir(exist_ok=True)
        shutil.copyfile(path, target)
        copied.append(target.relative_to(release).as_posix())
    for path in sorted(evidence.rglob("*.log")):
        target = release / "evidence" / path.name
        target.parent.mkdir(exist_ok=True)
        shutil.copyfile(path, target)
    if not any("balance" in name for name in copied):
        raise ValueError("New economy/combat screenshots missing")
    manifest = "\n".join(f"{sha(path)}  {path.name}" for path in (destination, source, installer)) + "\n"
    (release / "SHA256-v020.9x2.txt").write_text(manifest)
    validation = {"version": version, "source_commit": source_commit, "workflow_run": run_id,
                  "windows_sha256": digest, "player_members": len(names), "screenshots": copied,
                  "gates": ["core_and_historical_saves", "native_graphical_regressions", "windows_export_startup"],
                  "limits": ["experimental_balance_not_validated_by_human_playthrough", "no_user_PC_install", "no_physical_mobile_test"]}
    (release / "VALIDATION.json").write_text(json.dumps(validation, indent=2) + "\n")
    latest = {"version": version, "channel": "experimental", "windows": f"{version}/{archive_name}",
              "source": f"{version}/{source.name}", "playable": f"{version}/playable/Ancient Naval.exe",
              "launcher": f"{version}/Install_Ancient_Naval_v0209x2.cmd",
              "prepare": f"{version}/Install-PlayableRelease.ps1",
              "notes": f"{version}/Ancient_Naval_v020.9x2_Notes_RU.md", "copied_files": len(names)}
    (repo / "releases/latest.json").write_text(json.dumps(latest, indent=2) + "\n")
    print(f"PASS: retained {version}, {len(names)} Windows files, source {source_commit}, {len(copied)} captures")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--player", type=Path, required=True)
    parser.add_argument("--evidence", type=Path, required=True)
    parser.add_argument("--run-id", required=True)
    args = parser.parse_args()
    retain(args.player, args.evidence, args.run_id)
