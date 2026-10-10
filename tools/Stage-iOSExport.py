"""Stage a minimal isolated Mac export; never alter the user's source/signing files."""
import argparse
import json
import pathlib
import re
import shutil
import xml.etree.ElementTree as ET


def stage(source, destination, team_id="AAAAAAAAAA", bundle_id="com.beygelman.ancientnaval"):
    source, destination = pathlib.Path(source).resolve(), pathlib.Path(destination).resolve()
    if destination == source or source in destination.parents:
        raise ValueError("Stage outside the source tree to avoid recursive export pollution")
    if destination.exists():
        raise ValueError("Use a fresh staging path; existing work is never removed")
    if not re.fullmatch(r"[A-Z0-9]{10}", team_id):
        raise ValueError("Team ID must contain ten uppercase letters/digits")
    if not re.fullmatch(r"[A-Za-z0-9-]+(?:\.[A-Za-z0-9-]+)+", bundle_id):
        raise ValueError("Bundle identifier must be reverse-DNS letters/digits/hyphens")
    if not (source / "project.godot").is_file():
        raise ValueError("Source must contain project.godot")
    destination.mkdir(parents=True)
    ignored = shutil.ignore_patterns(".godot", ".git", "bin", "obj", "__MACOSX", "*.pdb")
    for name in ("src", "assets", "data", "scenes", "tests"):
        shutil.copytree(source / name, destination / name, ignore=ignored)
    for pattern in ("*.csproj", "*.sln"):
        for path in source.glob(pattern):
            shutil.copy2(path, destination / path.name)
    for name in ("project.godot", "export_presets.cfg", "icon.svg"):
        shutil.copy2(source / name, destination / name)
    for name in ("Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props"):
        if (source / name).is_file():
            shutil.copy2(source / name, destination / name)
    preset_path = destination / "export_presets.cfg"
    text = preset_path.read_text(encoding="utf-8-sig")
    # Only the iOS preset is changed, never historical Windows metadata.
    text, count = re.subn(r'(?ms)(\[preset\.9\.options\]\n.*?application/app_store_team_id=)"[^"]*"',
                         lambda m: m[1] + '"' + team_id + '"', text)
    if count != 1:
        raise ValueError("Expected the reviewed iOS preset at index 9")
    text = re.sub(r'(?ms)(\[preset\.9\.options\]\n.*?application/bundle_identifier=)"[^"]*"',
                  lambda m: m[1] + '"' + bundle_id + '"', text, count=1)
    preset_path.write_text(text, encoding="utf-8", newline="\n")
    # Windows' local offline NuGet feed is intentionally not copied to a Mac.
    nuget = ET.Element("configuration")
    sources = ET.SubElement(nuget, "packageSources")
    ET.SubElement(sources, "clear")
    ET.SubElement(sources, "add", key="nuget.org", value="https://api.nuget.org/v3/index.json")
    ET.ElementTree(nuget).write(destination / "NuGet.Config", encoding="utf-8", xml_declaration=True)
    (destination / "global.json").write_text(json.dumps({"sdk": {
        "version": "8.0.425", "rollForward": "disable", "allowPrerelease": False
    }}, indent=2) + "\n", encoding="utf-8")
    return destination


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", required=True)
    parser.add_argument("--destination", required=True)
    parser.add_argument("--team-id", default="AAAAAAAAAA")
    parser.add_argument("--bundle-id", default="com.beygelman.ancientnaval")
    args = parser.parse_args()
    print(stage(args.source, args.destination, args.team_id, args.bundle_id))
