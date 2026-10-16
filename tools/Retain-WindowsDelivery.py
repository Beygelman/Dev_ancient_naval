"""Retain the already-verified CI player and its matching source; never rebuild it."""
from __future__ import annotations

import argparse
import hashlib
import importlib.util
import io
import json
from pathlib import Path
import struct
import subprocess
import zipfile

VERSION = 'v020.9x1'
PLAYER_HASH = '762d2f697cfcd9a6695ffa34ef04a3c9af41b60731883f6571063a6c7fecc82f'


def retain(repo: Path, artifact: Path) -> None:
    release = repo / 'releases' / VERSION
    player_name = f'Ancient_Naval_{VERSION}_Windows.zip'
    raw = (artifact / player_name).read_bytes()
    if hashlib.sha256(raw).hexdigest() != PLAYER_HASH:
        raise ValueError('The downloaded player differs from the verified CI artifact')
    if PLAYER_HASH not in (artifact / 'SHA256.txt').read_text(encoding='utf-8-sig'):
        raise ValueError('Original artifact manifest mismatch')
    with zipfile.ZipFile(io.BytesIO(raw)) as player:
        if player.testzip() is not None:
            raise ValueError('Player CRC failure')
        names = player.namelist()
        if len(names) != len(set(names)) or len(names) != 190:
            raise ValueError('Player inventory differs')
        for name in names:
            parts = Path(name).parts
            if name.startswith(('/', '\\')) or '\\' in name or ':' in name or '..' in parts:
                raise ValueError(f'Unsafe archive path: {name}')
            if {'docs', 'tests', 'tools', '.git', 'releases'}.intersection(parts) or name.endswith('.pdb'):
                raise ValueError(f'Development content in player: {name}')
        for required in ('Ancient Naval.exe', 'Ancient Naval.pck',
                         'data_Dev_ancient_naval_windows_x86_64/Dev_ancient_naval.dll',
                         'data_Dev_ancient_naval_windows_x86_64/coreclr.dll'):
            if required not in names:
                raise ValueError(f'Missing runtime file: {required}')
        pck = io.BytesIO(player.read('Ancient Naval.pck'))
        magic, fmt, major, minor, patch, flags = struct.unpack('<6I', pck.read(24))
        base, = struct.unpack('<Q', pck.read(8))
        if magic != 0x43504447 or (major, minor, patch) != (4, 7, 2) or fmt not in (2, 3, 4) or flags & 5:
            raise ValueError('Unexpected PCK engine or format')
        if fmt >= 3:
            pck.seek(struct.unpack('<Q', pck.read(8))[0])
        else:
            pck.read(64)
        count, = struct.unpack('<I', pck.read(4))
        resources = []
        for _ in range(count):
            length, = struct.unpack('<I', pck.read(4))
            name = pck.read(length).rstrip(b'\0').decode().removeprefix('res://')
            offset, size = struct.unpack('<QQ', pck.read(16))
            pck.read(16)
            entry_flags, = struct.unpack('<I', pck.read(4))
            if entry_flags or base + offset + size > len(pck.getbuffer()):
                raise ValueError(f'Invalid PCK resource: {name}')
            resources.append(name)
        expected_resources = (release / 'PCK-INVENTORY.txt').read_text().splitlines()
        if sorted(resources) != expected_resources or len(resources) != len(set(resources)):
            raise ValueError('PCK inventory differs from the reviewed delivery')
    destination = release / player_name
    if destination.exists() and destination.read_bytes() != raw:
        raise FileExistsError('Never overwrite a different player archive')
    destination.write_bytes(raw)

    inventory = json.loads((release / 'SOURCE-INVENTORY.json').read_text())
    entries = inventory['files']
    # Fetch selected immutable source blobs in one request, rather than making
    # one lazy network fetch for every file in a partial CI checkout.
    subprocess.run(['git', '-C', str(repo), 'fetch', '--depth=1', '--no-tags',
                    '--no-write-fetch-head', '--filter=blob:none', 'origin',
                    inventory['source_commit']], check=True)
    subprocess.run(['git', '-C', str(repo), '-c', 'fetch.negotiationAlgorithm=noop',
                    'fetch', '--no-tags', '--no-write-fetch-head', '--recurse-submodules=no',
                    '--filter=blob:none', 'origin', '--stdin'], check=True,
                   input=('\n'.join(sorted({e['git_blob'] for e in entries})) + '\n').encode())
    spec = importlib.util.spec_from_file_location('source_pack', repo / 'tools/Package-iOSSource.py')
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    checked_entries = [dict(e) for e in entries]
    contents = module.blobs(repo, checked_entries)
    if checked_entries != entries:
        raise ValueError('Source blob SHA256 differs from the reviewed inventory')
    source = release / f'Ancient_Naval_{VERSION}_Source.zip'
    temporary = source.with_suffix('.tmp.zip')
    with zipfile.ZipFile(temporary, 'x', compression=zipfile.ZIP_DEFLATED, compresslevel=9) as packed:
        for entry in entries:
            info = zipfile.ZipInfo('dev-ancient-naval/' + entry['path'], (2026, 10, 16, 0, 0, 0))
            info.compress_type = zipfile.ZIP_DEFLATED
            info.external_attr = int(entry['git_mode'], 8) << 16
            packed.writestr(info, contents[entry['path']], compresslevel=9)
        for name, content in (
            ('.gdignore', ''),
            ('SOURCE-INVENTORY.json', json.dumps(inventory, ensure_ascii=False, indent=2) + '\n'),
        ):
            info = zipfile.ZipInfo('dev-ancient-naval/release-evidence/' + name, (2026, 10, 16, 0, 0, 0))
            info.compress_type = zipfile.ZIP_DEFLATED
            packed.writestr(info, content, compresslevel=9)
    with zipfile.ZipFile(temporary) as packed:
        if packed.testzip() is not None:
            raise ValueError('Source CRC failure')
        for entry in entries:
            if hashlib.sha256(packed.read('dev-ancient-naval/' + entry['path'])).hexdigest() != entry['sha256']:
                raise ValueError(f'Source archive mismatch: {entry["path"]}')
    if source.exists():
        if source.read_bytes() != temporary.read_bytes():
            raise FileExistsError('Never overwrite a different source archive')
        temporary.unlink()
    else:
        temporary.rename(source)
    files = sorted(p for p in release.iterdir() if p.is_file()
                   and p.name not in ('.gitattributes', f'SHA256-{VERSION}.txt'))
    local = [hashlib.sha256(p.read_bytes()).hexdigest() + '  ' + p.name for p in files]
    (release / f'SHA256-{VERSION}.txt').write_text('\n'.join(local) + '\n')
    (repo / 'releases' / f'SHA256-{VERSION}.txt').write_text(
        '\n'.join(line.replace('  ', f'  {VERSION}/', 1) for line in local) + '\n')
    print(f'PASS: retained byte-identical Windows player, {count} PCK resources, '
          f'{len(entries)} matching source files and SHA256 manifests')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--repo', default='.')
    parser.add_argument('--artifact-dir', required=True)
    args = parser.parse_args()
    retain(Path(args.repo).resolve(), Path(args.artifact_dir).resolve())
