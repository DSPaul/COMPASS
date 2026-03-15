from datetime import date
import os
import sys
import re

sys.path.insert(0, os.path.join(os.path.dirname(__file__), "flatpak"))
from generate_nuget_sources import generate as generate_nuget_sources

def replaceInFile(path, old, new):
    with open(path, "r") as f:
        data = f.read()
    data = data.replace(old, new)
    with open(path, "w") as f:
        f.write(data)

def replaceRegexInFile(path, pattern, replacement):
    with open(path, "r") as f:
        data = f.read()
    data = re.sub(pattern, replacement, data)
    with open(path, "w") as f:
        f.write(data)

def getOldVersion():
    with open(r"../Directory.Build.props", "r") as f:
        data = f.read()
    return re.search(r"<Version>(.+)</Version>", data).group(1)

def toAssemblyVersion(version):
    # Strip prerelease suffix and ensure 4-part format: x.y.z.0
    base = re.split(r"[-+]", version)[0]
    parts = base.split(".")
    while len(parts) < 4:
        parts.append("0")
    return ".".join(parts[:4])

def IncrementVersions():
    assembly_version = toAssemblyVersion(NEW_VERSION)

    # Update Directory.Build.props
    replaceRegexInFile(r"../Directory.Build.props",
        r"<Version>.+</Version>", f"<Version>{NEW_VERSION}</Version>")
    replaceRegexInFile(r"../Directory.Build.props",
        r"<AssemblyVersion>.+</AssemblyVersion>", f"<AssemblyVersion>{assembly_version}</AssemblyVersion>")
    replaceRegexInFile(r"../Directory.Build.props",
        r"<FileVersion>.+</FileVersion>", f"<FileVersion>{assembly_version}</FileVersion>")

    # Update Inno Setup installer script
    replaceRegexInFile("install.iss",
        r'#define MyAppVersion ".+"', f'#define MyAppVersion "{NEW_VERSION}"')

    # Update versionInfo.xml
    replaceRegexInFile(r"../versionInfo.xml",
        r"<version>.+</version>", f"<version>{NEW_VERSION}</version>")
    replaceRegexInFile(r"../versionInfo.xml",
        r"<url>.+</url>",
        f"<url>https://github.com/DSPAUL/COMPASS/releases/download/v{NEW_VERSION}/COMPASS_Setup_{NEW_VERSION}.exe</url>")

    # Prepend new release entry to flatpak metainfo
    today = date.today().strftime("%Y-%m-%d")
    release_type = "development" if re.search(r"[-+]", NEW_VERSION) else "stable"
    new_release = (
        f'    <release version="{NEW_VERSION}" date="{today}" type="{release_type}">\n'
        f'      <description>\n'
        f'        <p>See release notes on GitHub.</p>\n'
        f'      </description>\n'
        f'    </release>\n'
    )
    replaceInFile("flatpak/info.compassapp.COMPASS.metainfo.xml",
        "<releases>\n", f"<releases>\n{new_release}")

def WriteChangelog():
    data = ""
    if os.path.exists("changes.md"):
        with open("changes.md", "r") as f:
            data = f.read()

    # Make dir to put changelog in
    dir = f"./Versions/{NEW_VERSION}"
    if not os.path.exists(dir):
        os.mkdir(dir)

    # Create release-notes file and write to it
    notes = f"{dir}/release-notes-{NEW_VERSION}.md"
    open_mode = "x" if not os.path.exists(notes) else "w"
    with open(notes, open_mode) as f:
        today = date.today().strftime("%d %B %Y")
        data = f"# COMPASS v{NEW_VERSION} ({today})\n\n" + data
        f.write(data)

    # Append to total Changelog
    with open("../Changelog.md", "r+") as f:
        content = f.read()
        f.seek(14)  # skip "# CHANGELOG\n\n"
        data = data.replace("# ", "## ")  # make titles one level smaller
        f.write(data + '\n' + content[13:])

    # Clear changes back to template
    if os.path.exists("release-notes-template.md"):
        with open("release-notes-template.md", "r") as f:
            template = f.read()
        with open("changes.md", "w") as f:
            f.write(template)


# first arg: new version number
NEW_VERSION = sys.argv[1]
OLD_VERSION = getOldVersion()

if NEW_VERSION != OLD_VERSION:
    IncrementVersions()
    WriteChangelog()
    generate_nuget_sources()