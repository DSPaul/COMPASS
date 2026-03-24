#!/usr/bin/env python3
"""Generate nuget-sources.json for flatpak-builder from a packages.lock.json.

Runs `dotnet publish` on the Linux project to ensure all packages (including
runtime packs such as Microsoft.NETCore.App.Runtime.linux-x64) are present in
the NuGet cache, then reads their SHA512 hashes to build the source list.

Usage: python generate_nuget_sources.py
Writes: nuget-sources.json (next to this script)
"""

import base64
import binascii
import json
import os
import subprocess
from pathlib import Path

REPO_ROOT = Path(__file__).parent.parent.parent
LINUX_CSPROJ = REPO_ROOT / "Source/COMPASS.Linux/COMPASS.Linux.csproj"
LOCK_FILE = REPO_ROOT / "Source/COMPASS.Linux/packages.lock.json"
OUTPUT = Path(__file__).parent / "nuget-sources.json"
DEST_DIR = "nuget-sources"

# Global NuGet packages cache location
NUGET_CACHE = Path(os.environ.get("NUGET_PACKAGES", Path.home() / ".nuget" / "packages"))

# Runtime pack name prefixes that are not tracked in packages.lock.json but are
# required for self-contained publishes. They are downloaded by `dotnet publish`
# and stored in the NuGet cache just like regular packages.
RUNTIME_PACK_PREFIXES = (
    "microsoft.netcore.app.runtime.",
    "microsoft.netcore.app.host.",
    "microsoft.aspnetcore.app.runtime.",
    "microsoft.windowsdesktop.app.runtime.",
    "microsoft.netcore.app.crossgen2.",
)


def _package_entry(name: str, version: str) -> dict | None:
    """Build a flatpak source entry for a NuGet package, or None if not cached."""
    sha512_file = NUGET_CACHE / name / version / f"{name}.{version}.nupkg.sha512"
    if not sha512_file.exists():
        return None
    sha512 = binascii.hexlify(base64.b64decode(sha512_file.read_text().strip())).decode("ascii")
    filename = f"{name}.{version}.nupkg"
    url = f"https://api.nuget.org/v3-flatcontainer/{name}/{version}/{filename}"
    return {
        "type": "file",
        "url": url,
        "sha512": sha512,
        "dest": DEST_DIR,
        "dest-filename": filename,
    }


def _run_publish() -> None:
    """Run dotnet publish to populate the NuGet cache with runtime packs."""
    print("Running dotnet publish to populate NuGet cache with runtime packs...")
    result = subprocess.run(
        [
            "dotnet", "publish", str(LINUX_CSPROJ),
            "--configuration", "Release",
            "--runtime", "linux-x64",
            "--self-contained", "true",
        ],
        check=False,
    )
    if result.returncode != 0:
        print("WARNING: dotnet publish failed — nuget-sources.json may be incomplete.")


def generate():
    _run_publish()

    with LOCK_FILE.open() as f:
        lock = json.load(f)

    # Collect all resolved packages from the lock file
    packages = {}
    for framework, deps in lock.get("dependencies", {}).items():
        for name, info in deps.items():
            version = info.get("resolved")
            if version:
                packages[name.lower()] = version

    sources = []
    missing = []
    for name, version in packages.items():
        entry = _package_entry(name, version)
        if entry is None:
            missing.append(f"{name}/{version}")
        else:
            sources.append(entry)

    if missing:
        print(f"WARNING: {len(missing)} packages not found in NuGet cache — run `dotnet restore` first:")
        for m in missing:
            print(f"  {m}")

    # Also include runtime packs that dotnet publish downloads but that are not
    # tracked in packages.lock.json. These live in the same NuGet cache.
    already_included = {e["dest-filename"] for e in sources}
    runtime_pack_missing = []
    if NUGET_CACHE.is_dir():
        for pkg_dir in NUGET_CACHE.iterdir():
            name = pkg_dir.name.lower()
            if not name.startswith(RUNTIME_PACK_PREFIXES):
                continue
            for version_dir in pkg_dir.iterdir():
                version = version_dir.name
                filename = f"{name}.{version}.nupkg"
                if filename in already_included:
                    continue
                entry = _package_entry(name, version)
                if entry is not None:
                    sources.append(entry)
                    already_included.add(filename)
                else:
                    runtime_pack_missing.append(f"{name}/{version}")

    if runtime_pack_missing:
        print(
            f"WARNING: {len(runtime_pack_missing)} runtime pack(s) found in cache dir but missing .sha512 file:"
        )
        for m in runtime_pack_missing:
            print(f"  {m}")

    sources.sort(key=lambda s: s["dest-filename"])

    with OUTPUT.open("w", encoding="utf-8") as f:
        json.dump(sources, f, indent=4)

    print(f"Written {len(sources)} packages to {OUTPUT}")


if __name__ == "__main__":
    generate()
