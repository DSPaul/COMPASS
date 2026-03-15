#!/usr/bin/env python3
"""Generate nuget-sources.json for flatpak-builder from a packages.lock.json.

Reads SHA512 hashes from the global NuGet package cache, which is populated by
running `dotnet restore` on the Linux project beforehand.

Usage: python generate_nuget_sources.py
Writes: nuget-sources.json (next to this script)
"""

import base64
import binascii
import json
import os
from pathlib import Path

LOCK_FILE = Path(__file__).parent.parent.parent / "Source/COMPASS.Linux/packages.lock.json"
OUTPUT = Path(__file__).parent / "nuget-sources.json"
DEST_DIR = "nuget-sources"

# Global NuGet packages cache location
NUGET_CACHE = Path(os.environ.get("NUGET_PACKAGES", Path.home() / ".nuget" / "packages"))


def generate():
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
        sha512_file = NUGET_CACHE / name / version / f"{name}.{version}.nupkg.sha512"
        if not sha512_file.exists():
            missing.append(f"{name}/{version}")
            continue

        sha512 = binascii.hexlify(base64.b64decode(sha512_file.read_text().strip())).decode("ascii")
        filename = f"{name}.{version}.nupkg"
        url = f"https://api.nuget.org/v3-flatcontainer/{name}/{version}/{filename}"

        sources.append({
            "type": "file",
            "url": url,
            "sha512": sha512,
            "dest": DEST_DIR,
            "dest-filename": filename,
        })

    if missing:
        print(f"WARNING: {len(missing)} packages not found in NuGet cache — run `dotnet restore` first:")
        for m in missing:
            print(f"  {m}")

    sources.sort(key=lambda s: s["dest-filename"])

    with OUTPUT.open("w", encoding="utf-8") as f:
        json.dump(sources, f, indent=4)

    print(f"Written {len(sources)} packages to {OUTPUT}")


if __name__ == "__main__":
    generate()
