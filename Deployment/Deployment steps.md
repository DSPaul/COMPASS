# Deployment Steps

## Increment Version (done by running $: py publish.py x.x.x)

- [x] Increment AssemblyInfo Version Number
- [x] Increment installer.iss Version Number
- [x] Update versionInfo.xml
- [x] Append changelog from changes.md to Changelog.md

## Build all the things (also done when running $: py publish.py x.x.x)

- [x] Build Release
- [x] Publish
- [x] Create installer using install.iss

## Test

- [ ] Run all the unit and integration tests
- [ ] Run setup file to make sure installer works

## Deploy to github

- [ ] Create a PR
- [ ] Create new github tag based on dev
- [ ] create new github release
- [ ] Merge the PR <-- must be done AFTER release
