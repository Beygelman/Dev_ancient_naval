"""Read-only structural audit of native iPhone IPA/Xcode artifacts, not a device test.

Uses only Python's standard library and runs on Windows, macOS or Linux. The
optional --pck-only mode audits an existing Godot pack without claiming iOS success.
"""
from __future__ import annotations

import argparse
import hashlib
import io
import json
import pathlib
import plistlib
import posixpath
import re
import stat
import struct
import zipfile

ARM64 = 0x0100000C
PLATFORM_IOS = 2
STAMP_ENGINE = (4, 7, 2)
DEVELOPMENT_PARTS = {"docs", "tests", "tools", "releases", "release-evidence", "work", ".local", ".git", "bin", "obj", "__MACOSX"}
PRIVATE_FILES = {"export_credentials.cfg", "nuget.local.config", "nuget.config", "preview map.cmd"}
REQUIRED_PCK = {"res://project.binary", "res://data/balance.json", "res://data/localization/uk.json", "res://data/localization/nl.json"}


def require(condition: bool, message: str) -> None:
    if not condition:
        raise ValueError(message)


def safe_name(name: str) -> str:
    require(bool(name) and "\\" not in name and not name.startswith("/"), f"Unsafe archive path: {name!r}")
    relative = name.rstrip("/")
    parts = relative.split("/")
    require(all(part not in {"", ".", ".."} and ":" not in part and "\0" not in part for part in parts),
            f"Unsafe archive path: {name!r}")
    return relative


def forbidden_resource(name: str) -> bool:
    relative = name.removeprefix("res://")
    path = pathlib.PurePosixPath(relative)
    return bool(DEVELOPMENT_PARTS.intersection(path.parts)) or \
        "Sound FX Starter Pack Vol. 1" in path.parts or \
        path.suffix.lower() in {".pdb", ".keystore", ".jks", ".p12", ".pfx", ".mobileprovision"} or \
        path.name.lower() in PRIVATE_FILES


def digest_stream(stream) -> str:
    digest = hashlib.sha256()
    while block := stream.read(1024 * 1024):
        digest.update(block)
    return digest.hexdigest()


def digest_file(path: pathlib.Path) -> str:
    with path.open("rb") as stream:
        return digest_stream(stream)


def read_exact(stream, length: int) -> bytes:
    data = stream.read(length)
    require(len(data) == length, "Truncated binary structure")
    return data


def integer(stream, fmt: str) -> int:
    return struct.unpack(fmt, read_exact(stream, struct.calcsize(fmt)))[0]


def pck_inventory(stream, length: int) -> dict:
    require(stream.seekable(), "PCK audit needs a seekable stream")
    require(integer(stream, "<I") == 0x43504447, "Not a standalone Godot PCK")
    version = integer(stream, "<I")
    require(version in {2, 3, 4}, f"Unsupported PCK directory format: {version}")
    engine = tuple(integer(stream, "<I") for _ in range(3))
    require(engine == STAMP_ENGINE, f"Expected Godot 4.7.2 pack, got {engine}")
    flags = integer(stream, "<I")
    require(not flags & 1, "Encrypted PCK directory cannot be audited")
    require(not flags & 4, "Sparse/bundled PCK is not the self-contained player pack")
    file_base = integer(stream, "<Q")
    require(file_base <= length, "PCK file base lies outside the pack")
    if version >= 3:
        directory = integer(stream, "<Q")
        require(directory < length, "PCK directory offset lies outside the pack")
        stream.seek(directory)
    else:
        read_exact(stream, 64)
    count = integer(stream, "<I")
    require(0 < count <= 1_000_000, f"Invalid PCK entry count: {count}")
    seen = set()
    script_placeholders = 0
    for _ in range(count):
        name_length = integer(stream, "<I")
        require(0 < name_length <= 1024 * 1024, "Invalid PCK path length")
        raw = read_exact(stream, name_length).decode("utf-8").rstrip("\0")
        relative = safe_name(raw.removeprefix("res://"))
        name = "res://" + relative
        offset, size = integer(stream, "<Q"), integer(stream, "<Q")
        read_exact(stream, 16)  # Godot resource MD5; ZIP CRC and whole-PCK SHA are checked separately.
        file_flags = integer(stream, "<I")
        require(file_flags == 0, f"Unsupported encrypted/removal/delta PCK resource flags: {name}")
        require(file_base + offset + size <= length, f"PCK resource lies outside the pack: {name}")
        require(not forbidden_resource(name), f"Development/private content in PCK: {name}")
        require(name not in seen, f"Duplicate PCK resource alias: {name}")
        if relative.endswith('.cs'):
            directory_position = stream.tell()
            stream.seek(file_base + offset)
            require(read_exact(stream, size).strip() == b'', f"C# source content leaked into PCK: {name}")
            stream.seek(directory_position)
            script_placeholders += 1
        seen.add(name)
    require(REQUIRED_PCK.issubset(seen), f"Required PCK resources missing: {sorted(REQUIRED_PCK - seen)}")
    return {"format": version, "engine": ".".join(map(str, engine)), "resources": count,
            "script_placeholders": script_placeholders, "paths": sorted(seen)}


class Archive:
    def __init__(self, path: pathlib.Path):
        self.path = path
        self.zip = zipfile.ZipFile(path)
        infos = self.zip.infolist()
        require(0 < len(infos) <= 100_000, "Invalid ZIP entry count")
        self.entries = {}
        folded = set()
        for info in infos:
            name = safe_name(info.filename)
            require(name not in self.entries and name.casefold() not in folded,
                    f"Duplicate/case-colliding ZIP member: {info.filename}")
            require(not info.flag_bits & 1, f"Encrypted ZIP member cannot be audited: {name}")
            self.entries[name] = info
            folded.add(name.casefold())
        for name, info in self.entries.items():
            if self.is_link(info):
                self.resolve(name)
        require(self.zip.testzip() is None, "Archive CRC verification failed")

    @staticmethod
    def is_link(info) -> bool:
        return stat.S_ISLNK(info.external_attr >> 16)

    def resolve(self, name: str) -> str:
        visited = set()
        while True:
            require(name not in visited, f"Cyclic archive symlink: {name}")
            require(name in self.entries, f"Missing required ZIP member: {name}")
            visited.add(name)
            info = self.entries[name]
            if not self.is_link(info):
                return name
            require(info.file_size <= 4096, f"Oversized symlink: {name}")
            target = self.zip.read(info).decode("utf-8")
            require(target and not target.startswith("/") and "\\" not in target and ":" not in target,
                    f"Unsafe symlink: {name}")
            name = posixpath.normpath(posixpath.join(posixpath.dirname(name), target))
            safe_name(name)

    def read(self, name: str) -> bytes:
        return self.zip.read(self.entries[self.resolve(name)])

    def open(self, name: str):
        return self.zip.open(self.entries[self.resolve(name)])

    def sha(self, name: str) -> str:
        with self.open(name) as stream:
            return digest_stream(stream)

    def plist(self, name: str) -> dict:
        require(self.entries[self.resolve(name)].file_size <= 2 * 1024 * 1024, "Oversized Info.plist")
        data = plistlib.loads(self.read(name))
        require(isinstance(data, dict), f"Info.plist is not a dictionary: {name}")
        return data

    def pack(self, name: str) -> dict:
        info = self.entries[self.resolve(name)]
        with self.open(name) as stream:
            report = pck_inventory(stream, info.file_size)
        return dict(report, member=name, sha256=self.sha(name))

    def close(self):
        self.zip.close()


def os_version(value: int) -> str:
    return f"{value >> 16}.{(value >> 8) & 255}.{value & 255}"


def macho_arm64(stream, length: int, label: str, expected_type: int) -> dict:
    magic = read_exact(stream, 4)
    fat = {
        b"\xca\xfe\xba\xbe": (">", False), b"\xbe\xba\xfe\xca": ("<", False),
        b"\xca\xfe\xba\xbf": (">", True), b"\xbf\xba\xfe\xca": ("<", True),
    }
    slices = [(0, length)]
    if magic in fat:
        endian, wide = fat[magic]
        count = integer(stream, endian + "I")
        require(0 < count <= 64, f"Invalid fat Mach-O architecture count: {label}")
        slices = []
        for _ in range(count):
            cpu = integer(stream, endian + "I")
            integer(stream, endian + "I")  # CPU subtype.
            offset, size = integer(stream, endian + ("Q" if wide else "I")), integer(stream, endian + ("Q" if wide else "I"))
            integer(stream, endian + "I")
            if wide:
                integer(stream, endian + "I")
            require(offset + size <= length and size >= 32, f"Mach-O slice outside file: {label}")
            if cpu == ARM64:
                slices.append((offset, size))
        require(slices, f"Mach-O lacks arm64: {label}")
    reports = []
    for start, size in slices:
        stream.seek(start)
        magic = read_exact(stream, 4)
        require(magic in {b"\xcf\xfa\xed\xfe", b"\xfe\xed\xfa\xcf"}, f"Expected 64-bit Mach-O: {label}")
        endian = "<" if magic == b"\xcf\xfa\xed\xfe" else ">"
        cpu, subtype, file_type, commands, command_size, flags, reserved = struct.unpack(endian + "7I", read_exact(stream, 28))
        require(cpu == ARM64 and file_type == expected_type, f"Wrong CPU/file type for device binary: {label}")
        require(0 < commands <= 100_000 and command_size <= 16 * 1024 * 1024 and 32 + command_size <= size,
                f"Invalid Mach-O load commands: {label}")
        end = start + 32 + command_size
        platform, minimum, sdk, version_command = None, None, None, None
        for _ in range(commands):
            at = stream.tell()
            require(at + 8 <= end, f"Truncated Mach-O load command: {label}")
            command, command_length = struct.unpack(endian + "2I", read_exact(stream, 8))
            require(command_length >= 8 and at + command_length <= end, f"Invalid Mach-O command bounds: {label}")
            if command == 0x32:  # LC_BUILD_VERSION
                require(command_length >= 24, f"Short LC_BUILD_VERSION: {label}")
                platform, minimum, sdk, tool_count = struct.unpack(endian + "4I", read_exact(stream, 16))
                require(24 + tool_count * 8 <= command_length, f"Invalid build-version tools: {label}")
                version_command = "LC_BUILD_VERSION"
            elif command == 0x25:  # LC_VERSION_MIN_IPHONEOS (legacy ARM64 device encoding)
                require(command_length >= 16, f"Short LC_VERSION_MIN_IPHONEOS: {label}")
                minimum, sdk = struct.unpack(endian + "2I", read_exact(stream, 8))
                platform, version_command = PLATFORM_IOS, "LC_VERSION_MIN_IPHONEOS"
            stream.seek(at + command_length)
        require(stream.tell() == end, f"Mach-O command count/size mismatch: {label}")
        require(platform == PLATFORM_IOS and minimum is not None, f"Binary is not physical-device iOS: {label} (platform={platform})")
        reports.append({"architecture": "arm64", "subtype": subtype, "platform": "iOS device",
                        "minimum_os": os_version(minimum), "sdk": os_version(sdk), "command": version_command})
    return {"member": label, "slices": reports}


def binary(archive: Archive, name: str, expected_type: int) -> dict:
    info = archive.entries[archive.resolve(name)]
    mode = (info.external_attr >> 16) & 0o777
    # dyld opens shared libraries read-only and maps their executable segments.
    # NativeAOT's framework binary legitimately has mode 0644; only the app's
    # MH_EXECUTE entry point requires a filesystem execute bit.
    require(bool(mode & 0o444), f"Native binary lacks read permission in archive: {name}")
    if expected_type == 2:
        require(bool(mode & 0o111), f"Application lacks executable permission in archive: {name}")
    with archive.open(name) as stream:
        report = macho_arm64(stream, info.file_size, name, expected_type)
        return dict(report, archive_mode=oct(mode))


def executable_name(plist: dict, label: str) -> str:
    name = plist.get("CFBundleExecutable")
    require(isinstance(name, str) and "/" not in name and "\\" not in name and name not in {"", ".", ".."},
            f"Invalid CFBundleExecutable: {label}")
    return name


def audit_ipa(path: pathlib.Path, orientation: str = "adaptive") -> dict:
    archive = Archive(path)
    try:
        names = list(archive.entries)
        require(all(name == "Payload" or name.startswith("Payload/") for name in names), "IPA contains entries outside Payload")
        apps = {name.split("/")[1] for name in names if name.startswith("Payload/")}
        require(len(apps) == 1 and next(iter(apps)).endswith(".app"), "IPA must contain exactly one top-level .app")
        app = "Payload/" + next(iter(apps)) + "/"
        require(all(name == "Payload" or name == app.rstrip("/") or name.startswith(app) for name in names), "IPA has extra Payload content")
        for name in names:
            require(not forbidden_resource(name) and ".dSYM" not in pathlib.PurePosixPath(name).parts,
                    f"Development/private file in IPA: {name}")
        plist = archive.plist(app + "Info.plist")
        orientations = {'UIInterfaceOrientationLandscapeLeft', 'UIInterfaceOrientationLandscapeRight'}
        if orientation == "adaptive":
            orientations |= {'UIInterfaceOrientationPortrait', 'UIInterfaceOrientationPortraitUpsideDown'}
        require(set(plist.get('UISupportedInterfaceOrientations', [])) == orientations,
                f'iPhone player orientations do not match requested {orientation} policy')
        require(plist.get("CFBundlePackageType") == "APPL", "IPA bundle is not an application")
        require(plist.get("DTPlatformName") == "iphoneos", "IPA was not built for physical iPhone")
        require(isinstance(plist.get("UIDeviceFamily"), list) and 1 in plist["UIDeviceFamily"], "IPA does not support iPhone family")
        require(isinstance(plist.get("CFBundleIdentifier"), str) and plist["CFBundleIdentifier"], "IPA lacks bundle identity")
        main = binary(archive, app + executable_name(plist, app), 2)
        frameworks = []
        for name in names:
            if name.startswith(app + "Frameworks/") and name.endswith(".framework/Info.plist"):
                framework = name.removesuffix("Info.plist")
                metadata = archive.plist(name)
                require(metadata.get("CFBundlePackageType") == "FMWK", f"Invalid embedded framework: {name}")
                frameworks.append(binary(archive, framework + executable_name(metadata, name), 6))
        require(any("Dev_ancient_naval" in record["member"] for record in frameworks), "NativeAOT game framework is absent")
        for name in names:
            if name.startswith(app + "Frameworks/") and name.endswith(".dylib"):
                frameworks.append(binary(archive, name, 6))
        packs = [name for name in names if name.startswith(app) and name.endswith(".pck")]
        require(len(packs) == 1, "IPA must contain exactly one self-contained Godot pack")
        report = archive.pack(packs[0])
        return {"archive": path.name, "sha256": digest_file(path), "files": len(names),
                "bundle_identifier": plist["CFBundleIdentifier"], "short_version": plist.get("CFBundleShortVersionString"),
                "build": plist.get("CFBundleVersion"), "minimum_os": plist.get("MinimumOSVersion"),
                "orientation_policy": orientation, "orientations": sorted(orientations),
                "main_binary": main, "embedded_binaries": frameworks, "pck": report,
                "installation": "Unsigned input requiring personal signing, such as AltStore Classic; actual device launch not tested"}
    finally:
        archive.close()


def audit_xcode(path: pathlib.Path) -> dict:
    archive = Archive(path)
    try:
        names = list(archive.entries)
        for name in names:
            require(not forbidden_resource(name), f"Development/private artifact in Xcode package: {name}")
        projects = [name for name in names if name.endswith(".xcodeproj/project.pbxproj")]
        require(len(projects) == 1, "Xcode ZIP must contain exactly one .xcodeproj/project.pbxproj")
        project = projects[0].removesuffix("/project.pbxproj")
        root = posixpath.dirname(project)
        prefix = (root + "/") if root else ""
        base = posixpath.basename(project).removesuffix(".xcodeproj")
        pack_name = prefix + base + ".pck"
        require(prefix + base + ".xcframework/Info.plist" in archive.entries, "Xcode ZIP is missing its sibling Godot XCFramework")
        require(prefix + base + "/" + base + "-Info.plist" in archive.entries, "Xcode ZIP is missing the target's Info.plist")
        aot = [name for name in names if name.startswith(prefix + base + "/") and name.endswith("_aot.xcframework/Info.plist")]
        require(len(aot) == 1, "Xcode ZIP is missing its complete NativeAOT XCFramework")
        metadata = archive.plist(aot[0])
        libraries = metadata.get("AvailableLibraries")
        require(isinstance(libraries, list) and libraries, "Invalid NativeAOT XCFramework metadata")
        devices = [item for item in libraries if item.get("SupportedPlatform") == "ios" and not item.get("SupportedPlatformVariant") and "arm64" in item.get("SupportedArchitectures", [])]
        require(len(devices) == 1, "NativeAOT XCFramework lacks a unique iOS arm64 device slice")
        xcroot = aot[0].removesuffix("Info.plist")
        item = devices[0]
        identifier, library = item.get("LibraryIdentifier"), item.get("LibraryPath")
        require(isinstance(identifier, str) and isinstance(library, str), "XCFramework lacks library paths")
        safe_name(identifier)
        safe_name(library)
        framework = xcroot + identifier + "/" + library.rstrip("/") + "/"
        fwmeta = archive.plist(framework + "Info.plist")
        aot_binary = binary(archive, framework + executable_name(fwmeta, framework), 6)
        return {"archive": path.name, "sha256": digest_file(path), "files": len(names),
                "project": project, "native_aot_binary": aot_binary, "pck": archive.pack(pack_name)}
    finally:
        archive.close()


def audit_manifest(path: pathlib.Path, required: list[pathlib.Path]) -> dict:
    root = path.parent.resolve()
    entries = {}
    for line in path.read_text(encoding="utf-8-sig").splitlines():
        match = re.fullmatch(r"([a-fA-F0-9]{64})  (.+)", line)
        require(match is not None, "Invalid SHA256 manifest record")
        expected, name = match.groups()
        name = safe_name(name)
        require(name not in entries, f"Duplicate manifest member: {name}")
        file = (root / name).resolve()
        require(file.is_file() and file.is_relative_to(root), f"Missing/unsafe manifest file: {name}")
        actual = digest_file(file)
        require(actual == expected.lower(), f"SHA256 mismatch: {name}")
        entries[name] = actual
    require(entries, "SHA256 manifest is empty")
    for file in required:
        require(file.resolve().is_relative_to(root), "Artifact is not alongside its SHA256 manifest")
        relative = file.resolve().relative_to(root).as_posix()
        require(relative in entries, f"Artifact missing from manifest: {relative}")
    return {"manifest": path.name, "files": entries}


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--ipa")
    parser.add_argument("--xcode")
    parser.add_argument("--manifest")
    parser.add_argument("--pck-only")
    parser.add_argument("--report-file")
    parser.add_argument("--orientation", choices=("adaptive", "landscape"), default="adaptive",
                        help="Current portrait/landscape policy; use landscape only for historical exports")
    args = parser.parse_args()
    if args.pck_only:
        require(not (args.ipa or args.xcode or args.manifest), "PCK-only mode does not verify iOS artifacts")
        path = pathlib.Path(args.pck_only).resolve()
        with path.open("rb") as stream:
            report = {"scope": "PCK only; not an iOS/package/device verification",
                      "pck": dict(pck_inventory(stream, path.stat().st_size), sha256=digest_file(path))}
    else:
        require(bool(args.ipa and args.xcode and args.manifest), "Supply --ipa, --xcode and --manifest together")
        ipa, xcode, manifest = (pathlib.Path(value).resolve() for value in (args.ipa, args.xcode, args.manifest))
        hashes = audit_manifest(manifest, [ipa, xcode])
        ipa_report, xcode_report = audit_ipa(ipa, args.orientation), audit_xcode(xcode)
        require(ipa_report["pck"]["sha256"] == xcode_report["pck"]["sha256"], "IPA and Xcode project contain different game packs")
        report = {"scope": "Native iOS artifact structure and hashes; not actual installation/frame pacing",
                  "verified": True, "sha256_manifest": hashes, "ipa": ipa_report, "xcode": xcode_report,
                  "same_pck": True, "device_launch_tested": False}
    rendered = json.dumps(report, ensure_ascii=False, sort_keys=True, indent=2) + "\n"
    if args.report_file:
        output = pathlib.Path(args.report_file).resolve()
        with output.open("x", encoding="utf-8", newline="\n") as stream:
            stream.write(rendered)
    print(rendered)


if __name__ == "__main__":
    main()
