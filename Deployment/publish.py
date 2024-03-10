from dataclasses import replace
from datetime import date
import os
import sys
import re

def replaceVersion(path, old, new):
    with open(path, "r") as f:
        data = f.read()
    data = data.replace(old,new)
    with open(path,"w") as f:
        f.write(data)

def getOldVersion():
    #open and read AssemblyInfo
    with open(r"../src/Properties/AssemblyInfo.cs", "r+") as f:
        data = f.read()
    #find old version number
    return re.findall(r".*\[assembly: AssemblyVersion\(\"(.+)\"\)\].*", data, re.MULTILINE)[0]

def IncrementVersions():
    # Increment AssemblyInfo
    replaceVersion(r"../src/Properties/AssemblyInfo.cs", OLD_VERSION, NEW_VERSION)
    # Increment installer.iss
    replaceVersion("install.iss", OLD_VERSION, NEW_VERSION)
    # Increment versionInfo.xml
    replaceVersion(r"../versionInfo.xml", OLD_VERSION,NEW_VERSION)

def WriteChangelog():

    data = ""
    # add changelog of this release to full changelog
    if os.path.exists("changes.md"):
        with open("changes.md", "r") as f:
            data = f.read()

    # make dir to put changelog in
    dir = f"./Versions/{NEW_VERSION}"
    if not os.path.exists(dir): os.mkdir(dir)

    #create realease-notes file and write to it
    notes = dir +f"/release-notes-{NEW_VERSION}.md"
    openMode = "x" if not os.path.exists(notes) else "w"
    with open(dir +f"/release-notes-{NEW_VERSION}.md",openMode) as f:
        # append version number and date
        today = date.today().strftime("%d %B %Y")
        data = f"# COMPASS v{NEW_VERSION} ({today})\n\n" + data
        f.write(data)

    #append them to total Changelog
    with open("../Changelog.md","r+") as f:
        content = f.read()
        f.seek(14) # skip "#CHANGELOG"
        data = data.replace("# ","## ") # make titles one smaller
        f.write(data + '\n' + content[13:])
    
    #clear changes back to template
    if os.path.exists("release-notes-template.md"):
        with open("release-notes-template.md","r") as f:
            data = f.read()
        with open("changes.md","w") as f:
            f.write(data)


def Build():
    os.system(r"dotnet build ../COMPASS.sln -c Release")

def Publish():
    os.system(r"dotnet publish ../COMPASS.sln -p:PublishProfile=FolderProfile.pubxml")
    
def CreateSetupFile():
    os.system(r"InnoSetup\ISCC.exe install.iss")

# first arg: file name, second arg: version
NEW_VERSION = sys.argv[1]
OLD_VERSION = getOldVersion()

#switch to dev branch
#os.system("git checkout dev")

if NEW_VERSION != OLD_VERSION:
    IncrementVersions()
    WriteChangelog()    
Build()
Publish()
CreateSetupFile()