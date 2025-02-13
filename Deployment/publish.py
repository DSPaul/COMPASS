import os

def Build():
    os.system(r"dotnet build ../COMPASS.sln -c Release")

def Publish():
    os.system(r"dotnet publish ../COMPASS.sln -p:PublishProfile=FolderProfile.pubxml")
    
def CreateSetupFile():
    os.system(r""" ""C:\Program Files (x86)\Inno Setup 6\ISCC.exe" install.iss" """)
 
Build()
Publish()
CreateSetupFile()