"""Package reviewed Git blobs, never working files, as an additive iOS source handoff.

ZIP_STORED and fixed metadata make identical Git revisions byte reproducible even
across zlib versions. No caches, signing material, player archives or nested clone
are selected. Run --dry-run before the final documentation commit if desired.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import pathlib
import stat
import subprocess
import zipfile

STAMP = (2026, 10, 10, 0, 0, 0)
ARCHIVE_NAME = "Ancient_Naval_v020.8b_iOS_SOURCE_HANDOFF.zip"
PREFIX = "Ancient_Naval_v020.8b_iOS_Source/"
ROOT_FILES = {
    ".editorconfig", ".gitattributes", ".gitignore", "AGENTS.md", "README.md",
    "CHANGELOG.md", "project.godot", "export_presets.cfg", "icon.svg",
    "Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props",
    "global.json", "LICENSE", "LICENSE.md", "LICENSE.txt", "COPYING",
}
ROOT_TREES = {"src", "assets", "data", "scenes", "tests", "tools", ".github"}
FORBIDDEN_PARTS = {".git", ".godot", "bin", "obj", "__MACOSX", "node_modules"}
FORBIDDEN_SUFFIXES = {".pdb", ".exe", ".dll", ".zip", ".7z", ".ipa", ".tpz"}


def git(repo: pathlib.Path, *args: str, payload: bytes | None = None) -> bytes:
    result = subprocess.run(["git", "-C", str(repo), *args], input=payload,
                            stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=False)
    if result.returncode:
        raise RuntimeError(result.stderr.decode("utf-8", errors="replace"))
    return result.stdout


def selected(name: str) -> bool:
    path = pathlib.PurePosixPath(name)
    if path.is_absolute() or ".." in path.parts or not path.parts:
        raise ValueError(f"Unsafe Git path: {name!r}")
    if FORBIDDEN_PARTS.intersection(path.parts) or path.suffix.lower() in FORBIDDEN_SUFFIXES:
        return False
    if any(part.lower() in {"nuget.config", "nuget.local.config", ".ds_store"}
           for part in path.parts):
        return False
    if len(path.parts) == 1:
        return name in ROOT_FILES or path.suffix.lower() in {".csproj", ".sln", ".slnx"}
    if path.parts[0] in ROOT_TREES:
        return True
    if path.parts[0] == "docs":
        return len(path.parts) >= 2 and path.parts[1] != "dev-ancient-naval" \
            and path.suffix.lower() in {".md", ".txt", ".rst"}
    return False


def tree(repo: pathlib.Path, revision: str) -> tuple[str, list[dict]]:
    commit = git(repo, "rev-parse", "--verify", f"{revision}^{{commit}}").decode().strip()
    entries = []
    for row in git(repo, "ls-tree", "-rz", commit).split(b"\0"):
        if not row:
            continue
        header, raw_name = row.split(b"\t", 1)
        mode, kind, oid = header.decode("ascii").split()
        name = raw_name.decode("utf-8")
        if not selected(name):
            continue
        if kind != "blob" or mode not in {"100644", "100755"}:
            raise ValueError(f"Selected path must be a regular tracked file: {name} ({mode}/{kind})")
        entries.append({"path": name, "git_blob": oid, "git_mode": mode})
    entries.sort(key=lambda entry: entry["path"])
    required = ROOT_FILES.intersection({"AGENTS.md", "README.md", "CHANGELOG.md",
                                       "project.godot", "export_presets.cfg", "icon.svg"})
    required |= {"Dev_ancient_naval.csproj", "Dev_ancient_naval.sln",
                 "tools/Build-iOS.sh", "tools/Stage-iOSExport.py", ".github/workflows/ios.yml",
                 "docs/INSTALL-iPHONE-RU.md", "docs/IOS-PORT-v020.8b.md"}
    missing = required.difference(entry["path"] for entry in entries)
    if missing:
        raise ValueError(f"Required reviewed files missing from Git revision: {sorted(missing)}")
    return commit, entries


def blobs(repo: pathlib.Path, entries: list[dict]) -> dict[str, bytes]:
    batch = git(repo, "cat-file", "--batch",
                payload=("\n".join(entry["git_blob"] for entry in entries) + "\n").encode("ascii"))
    position, contents = 0, {}
    for entry in entries:
        newline = batch.index(b"\n", position)
        oid, kind, size_text = batch[position:newline].decode("ascii").split()
        size = int(size_text)
        if oid != entry["git_blob"] or kind != "blob":
            raise ValueError(f"Unexpected batch header for {entry['path']}")
        position = newline + 1
        data = batch[position:position + size]
        position += size
        if batch[position:position + 1] != b"\n":
            raise ValueError("Corrupt Git batch response")
        position += 1
        entry.update(size=size, sha256=hashlib.sha256(data).hexdigest())
        contents[entry["path"]] = data
    if position != len(batch):
        raise ValueError("Unexpected data at end of Git batch")
    return contents


def member(name: str, mode: str = "100644") -> zipfile.ZipInfo:
    info = zipfile.ZipInfo(PREFIX + name, date_time=STAMP)
    info.create_system = 3
    info.compress_type = zipfile.ZIP_STORED
    info.external_attr = (stat.S_IFREG | (0o755 if mode == "100755" else 0o644)) << 16
    return info


def encode_json(value: dict) -> bytes:
    return (json.dumps(value, ensure_ascii=False, sort_keys=True, indent=2) + "\n").encode("utf-8")


def verify(archive: pathlib.Path, inventory: dict) -> None:
    with zipfile.ZipFile(archive, "r") as packed:
        expected = {PREFIX + entry["path"] for entry in inventory["files"]}
        expected.add(PREFIX + "HANDOFF-INVENTORY.json")
        if set(packed.namelist()) != expected or len(packed.namelist()) != len(expected):
            raise ValueError("Archive inventory differs or contains duplicate members")
        if packed.testzip() is not None:
            raise ValueError("Archive CRC verification failed")
        for entry in inventory["files"]:
            name = PREFIX + entry["path"]
            data = packed.read(name)
            info = packed.getinfo(name)
            if len(data) != entry["size"] or hashlib.sha256(data).hexdigest() != entry["sha256"]:
                raise ValueError(f"Archive content verification failed: {name}")
            if info.date_time != STAMP or info.compress_type != zipfile.ZIP_STORED:
                raise ValueError(f"Nonreproducible archive metadata: {name}")
        if packed.read(PREFIX + "HANDOFF-INVENTORY.json") != encode_json(inventory):
            raise ValueError("Embedded inventory mismatch")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", required=True)
    parser.add_argument("--revision", default="HEAD")
    parser.add_argument("--output-dir")
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()
    repo = pathlib.Path(args.repo).resolve()
    commit, entries = tree(repo, args.revision)
    contents = blobs(repo, entries)
    inventory = {
        "format_version": 1, "source_commit": commit, "version": "v020.8b",
        "purpose": "iOS source handoff; source code, not an installable iPhone application",
        "archive_root": PREFIX, "fixed_zip_timestamp": "2026-10-10T00:00:00",
        "compression": "ZIP_STORED; exact byte reproducibility independent of zlib version",
        "selection": "Reviewed tracked source, assets, data, scenes, tests, tools, workflows, root metadata and engineering documentation text",
        "omissions": ["Historical releases", "Untracked/local edits", "Caches/bin/obj", "Debug screenshots outside required assets", "Nested docs/dev-ancient-naval", "Windows-only NuGet configs", "Sound FX Starter Pack Vol. 1 (unused development library)"],
        "files": entries,
    }
    if args.dry_run:
        print(json.dumps({"source_commit": commit, "files": len(entries),
                          "raw_bytes": sum(entry["size"] for entry in entries),
                          "paths": [entry["path"] for entry in entries]}, ensure_ascii=False, indent=2))
        return
    output = pathlib.Path(args.output_dir).resolve() if args.output_dir else \
        repo / "releases" / "v020.8b" / "ios-2026-10-10"
    archive = output / ARCHIVE_NAME
    inventory_file = output / "SOURCE-HANDOFF-INVENTORY.json"
    manifest_file = output / "SHA256-iOS-SOURCE-HANDOFF.txt"
    for target in (archive, inventory_file, manifest_file):
        if target.exists():
            raise FileExistsError(f"Historical/existing output is never replaced: {target}")
    output.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(archive, mode="x", compression=zipfile.ZIP_STORED) as packed:
        for entry in entries:
            packed.writestr(member(entry["path"], entry["git_mode"]), contents[entry["path"]])
        packed.writestr(member("HANDOFF-INVENTORY.json"), encode_json(inventory))
    verify(archive, inventory)
    sha = hashlib.sha256(archive.read_bytes()).hexdigest()
    receipt = dict(inventory, archive_name=ARCHIVE_NAME, archive_sha256=sha,
                   archive_size=archive.stat().st_size, verified=True)
    with inventory_file.open("xb") as stream:
        stream.write(encode_json(receipt))
    with manifest_file.open("x", encoding="utf-8", newline="\n") as stream:
        stream.write(f"{sha}  {ARCHIVE_NAME}\n")
        stream.write(f"{hashlib.sha256(inventory_file.read_bytes()).hexdigest()}  {inventory_file.name}\n")
    print(json.dumps({"archive": str(archive), "sha256": sha, "source_commit": commit,
                      "files": len(entries), "verified": True}, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
