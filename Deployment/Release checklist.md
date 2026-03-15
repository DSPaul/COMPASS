# Creating a release checklist

## Increment Version (run locally from the Deployment folder)

```
py bumpVersion.py x.x.x
```

- [x] Updates version in `Directory.Build.props`
- [x] Updates `install.iss`
- [x] Updates `versionInfo.xml`
- [x] Prepends release entry to flatpak metainfo
- [x] Appends changelog from `changes.md` to `Changelog.md`
- [ ] Commit and push the version bump

## Create PR to master

- [ ] Open a PR from the release branch to master
- [ ] All unit and integration tests pass (triggered automatically on PR)

## Run the Package Workflow

- [ ] Trigger the `Create release` workflow on GitHub Actions with the version number and branch
- [ ] Wait for all three jobs to complete (Windows build, Flatpak build, Draft Release)

## Test & Publish

- [ ] Download the Windows installer and Flatpak from the draft release
- [ ] Install and test both
- [ ] Fill in the release description/changelog on the draft release
- [ ] Publish the release
- [ ] Merge the PR ← must be done AFTER publishing the release
