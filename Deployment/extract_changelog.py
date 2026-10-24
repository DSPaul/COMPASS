#!/usr/bin/env python3
"""Extract a single release's section from Changelog.md.

Prints the body of the `## COMPASS v<version> (<date>)` heading, without the
heading itself and without the blank lines around it, so the output starts at
`### New Features`. Used by the `Create release` workflow to prefill the draft
release body, and runnable on its own:

Usage: python extract_changelog.py [version] [--output release-notes.md]

Without a version, the one in ../Directory.Build.props is used. A prerelease
version (2.0.1-beta1) falls back to the heading of its release version, which is
how unreleased builds are normally documented until the changelog is renamed on
release day.

Exits with status 1 when no matching section exists, unless --allow-missing is
passed, in which case an empty file is written and a warning is printed.
"""

from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent
CHANGELOG = REPO_ROOT / "Changelog.md"
BUILD_PROPS = REPO_ROOT / "Directory.Build.props"

SECTION_HEADING_PATTERN = re.compile(r"^##\s+COMPASS\s+v(?P<version>\S+)")


def read_version_from_build_props() -> str:
    """Read the version the release is being built from."""
    build_props = BUILD_PROPS.read_text(encoding="utf-8")
    version_match = re.search(r"<Version>(?P<version>[^<]+)</Version>", build_props)
    if version_match is None:
        raise ValueError(f"No <Version> element found in {BUILD_PROPS}")
    return normalize_version(version_match.group("version"))


def normalize_version(version: str) -> str:
    """Drop a leading 'v' and surrounding whitespace, as both occur in the wild."""
    return version.strip().removeprefix("v")


def candidate_versions(version: str) -> list[str]:
    """Versions to look for, most specific first."""
    version = normalize_version(version)
    release_version = re.split(r"[-+]", version)[0]
    if release_version == version:
        return [version]
    return [version, release_version]


def extract_section(changelog: str, version: str) -> str | None:
    """Return the body of the section documenting `version`, or None if there is none."""
    for candidate_version in candidate_versions(version):
        section = section_for_version(changelog, candidate_version)
        if section is not None:
            return section
    return None


def section_for_version(changelog: str, version: str) -> str | None:
    """Return the body of the `## COMPASS v<version>` section, or None if there is none."""
    section_lines: list[str] = []
    is_capturing = False

    for line in changelog.splitlines():
        if line.startswith("## "):
            if is_capturing:
                break
            heading_match = SECTION_HEADING_PATTERN.match(line)
            if heading_match is not None and heading_match.group("version") == version:
                is_capturing = True
            continue
        if is_capturing:
            section_lines.append(line)

    if not is_capturing:
        return None
    return "\n".join(trim_blank_edges(section_lines))


def trim_blank_edges(lines: list[str]) -> list[str]:
    """Drop leading and trailing blank lines, keeping the ones in between."""
    first_content_line = 0
    while first_content_line < len(lines) and not lines[first_content_line].strip():
        first_content_line += 1

    last_content_line = len(lines)
    while last_content_line > first_content_line and not lines[last_content_line - 1].strip():
        last_content_line -= 1

    return lines[first_content_line:last_content_line]


def main(argv: list[str] | None = None) -> int:
    argument_parser = argparse.ArgumentParser(
        description="Extract one version's section from Changelog.md.")
    argument_parser.add_argument(
        "version", nargs="?",
        help="version to extract, with or without a leading 'v' (default: <Version> of Directory.Build.props)")
    argument_parser.add_argument(
        "--changelog", type=Path, default=CHANGELOG,
        help=f"changelog to read (default: {CHANGELOG})")
    argument_parser.add_argument(
        "--output", type=Path,
        help="file to write the section to (default: stdout)")
    argument_parser.add_argument(
        "--allow-missing", action="store_true",
        help="write an empty section instead of failing when the version is not documented")
    arguments = argument_parser.parse_args(argv)

    version = arguments.version if arguments.version is not None else read_version_from_build_props()
    version = normalize_version(version)

    if not arguments.changelog.is_file():
        print(f"error: {arguments.changelog} does not exist", file=sys.stderr)
        return 2

    section = extract_section(arguments.changelog.read_text(encoding="utf-8"), version)
    if section is None:
        message = f"no '## COMPASS v{version}' section in {arguments.changelog}"
        if not arguments.allow_missing:
            print(f"error: {message}", file=sys.stderr)
            return 1
        print(f"warning: {message}, the release body will be empty", file=sys.stderr)
        section = ""
    elif section == "":
        print(f"warning: the '## COMPASS v{version}' section in {arguments.changelog} has no entries",
              file=sys.stderr)

    release_notes = f"{section}\n" if section else ""
    if arguments.output is not None:
        arguments.output.write_text(release_notes, encoding="utf-8")
    else:
        sys.stdout.write(release_notes)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
