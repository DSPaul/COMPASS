#!/usr/bin/env python3
"""Generate nuget-sources.json for flatpak-builder from a packages.lock.json.

Reads SHA512 hashes for regular packages from the local NuGet cache (populated
by running `dotnet restore`), and fetches hashes for runtime packs directly
from the NuGet API.

Usage: python generate_nuget_sources.py
Writes: nuget-sources.json and dotnet-runtime-version.txt (next to this script)

The two files must stay in sync: nuget-sources.json vendors the runtime packs for
the resolved version, and dotnet-runtime-version.txt lets the Flatpak manifest pin
`RuntimeFrameworkVersion` to that exact version. Pinning is what keeps the offline
build working when the SDK shipped by the Flatpak extension is a different patch
than the version we vendored.
"""

import base64
import binascii
import gzip
import json
import os
import re
import urllib.request
import xml.etree.ElementTree as ET
from pathlib import Path

REPO_ROOT = Path(__file__).parent.parent.parent
LOCK_FILE = REPO_ROOT / "Source/COMPASS.Linux/packages.lock.json"
MANIFEST = Path(__file__).parent / "info.compassapp.COMPASS.yaml"
OUTPUT = Path(__file__).parent / "nuget-sources.json"
RUNTIME_VERSION_OUTPUT = Path(__file__).parent / "dotnet-runtime-version.txt"
DEST_DIR = "nuget-sources"

# Global NuGet packages cache location
NUGET_CACHE = Path(os.environ.get("NUGET_PACKAGES", Path.home() / ".nuget" / "packages"))

# Runtime packs required for a self-contained linux-x64 publish.
# These are not tracked in packages.lock.json but are fetched directly from NuGet.
# crossgen2 is required when PublishReadyToRun=true (used in info.compassapp.COMPASS.yaml).
LINUX_RUNTIME_PACKS = (
    "microsoft.netcore.app.runtime.linux-x64",
    "microsoft.netcore.app.host.linux-x64",
    "microsoft.netcore.app.crossgen2.linux-x64",
    "microsoft.aspnetcore.app.runtime.linux-x64",
)


def _fetch_json(url: str) -> dict:
    """Fetch JSON from a URL, transparently handling gzip-compressed responses."""
    with urllib.request.urlopen(url, timeout=30) as resp:
        raw = resp.read()
    try:
        return json.loads(raw)
    except (json.JSONDecodeError, UnicodeDecodeError):
        return json.loads(gzip.decompress(raw))


def _resolve_dotnet_runtime_version() -> str:
    """Read the .NET version shipped by the Flatpak SDK extension from its appdata.xml."""
    # Read the top-level scalar without introducing a YAML package dependency.
    runtime_lines = [
        line for line in MANIFEST.read_text(encoding="utf-8").splitlines()
        if line.startswith("runtime-version:")
    ]
    if len(runtime_lines) != 1:
        raise RuntimeError(f"Expected exactly one top-level runtime-version in {MANIFEST}")
    match = re.fullmatch(
        r"runtime-version:[ \t]*(['\"]?)([0-9]{2}\.[0-9]{2})\1[ \t]*(?:#.*)?",
        runtime_lines[0],
    )
    if match is None:
        raise RuntimeError(f"Expected runtime-version in YY.MM format in {MANIFEST}")
    runtime_branch = match.group(2)
    appdata_url = (
        "https://raw.githubusercontent.com/flathub/org.freedesktop.Sdk.Extension.dotnet10"
        f"/refs/heads/branch/{runtime_branch}/org.freedesktop.Sdk.Extension.dotnet10.appdata.xml"
    )
    with urllib.request.urlopen(appdata_url, timeout=30) as resp:
        raw = resp.read()
    root = ET.fromstring(raw)
    first_release = root.find("./releases/release")
    if first_release is None:
        raise RuntimeError("No release entries found in Flatpak SDK extension appdata.xml")
    return first_release.attrib["version"]


def _fetch_entry_from_nuget(name: str, version: str) -> dict | None:
    """Fetch sha512 for a package from the NuGet API and build a source entry."""
    # Step 1: get the catalogEntry URL from the registration leaf (response is gzip-compressed)
    reg_url = f"https://api.nuget.org/v3/registration5-gz-semver2/{name}/{version}.json"
    try:
        leaf = _fetch_json(reg_url)
        catalog_url = leaf["catalogEntry"]
        # Step 2: fetch the catalog entry which contains the packageHash
        catalog = _fetch_json(catalog_url)
        b64_hash = catalog["packageHash"]
        sha512 = binascii.hexlify(base64.b64decode(b64_hash)).decode("ascii")
    except Exception as exc:
        print(f"WARNING: Could not fetch hash for {name}/{version} from NuGet: {exc}")
        return None
    filename = f"{name}.{version}.nupkg"
    url = f"https://api.nuget.org/v3-flatcontainer/{name}/{version}/{filename}"
    return {
        "type": "file",
        "url": url,
        "sha512": sha512,
        "dest": DEST_DIR,
        "dest-filename": filename,
    }


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


def generate():
    with LOCK_FILE.open() as f:
        lock = json.load(f)

    dotnet_runtime_version = _resolve_dotnet_runtime_version()
    print(f"Resolved .NET runtime version: {dotnet_runtime_version}")

    # Persist the version so the Flatpak manifest can pin RuntimeFrameworkVersion
    # to exactly the version we vendor. Without this the SDK shipped by the Flatpak
    # extension would request its own (possibly different) runtime pack version and
    # the offline restore would fail with NU1102.
    RUNTIME_VERSION_OUTPUT.write_text(f"{dotnet_runtime_version}\n", encoding="utf-8")
    print(f"Written .NET runtime version to {RUNTIME_VERSION_OUTPUT}")

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

    # Fetch runtime packs for the target flatpak SDK version directly from NuGet.
    already_included = {e["dest-filename"] for e in sources}
    for pack_name in LINUX_RUNTIME_PACKS:
        filename = f"{pack_name}.{dotnet_runtime_version}.nupkg"
        if filename in already_included:
            continue
        print(f"Fetching {pack_name}/{dotnet_runtime_version} from NuGet...")
        entry = _fetch_entry_from_nuget(pack_name, dotnet_runtime_version)
        if entry is not None:
            sources.append(entry)

    sources.sort(key=lambda s: s["dest-filename"])

    with OUTPUT.open("w", encoding="utf-8", newline="\n") as f:
        json.dump(sources, f, indent=4)
        f.write("\n")

    print(f"Written {len(sources)} packages to {OUTPUT}")


if __name__ == "__main__":
    generate()
