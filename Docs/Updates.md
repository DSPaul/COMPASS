# Automatic updates

## Overview

COMPASS checks the `DSPAUL/COMPASS` GitHub releases for versions newer than the running one. On Windows it can download and install them automatically; on Linux it just opens the release page.

Checking runs on startup and then every 6 hours, and can be disabled or extended to pre-releases in Settings.

## Flow

```
UpdateManager (startup + every 6h)
   │
   ├─ IUpdateService.CheckForUpdates(includePrerelease)
   │      └─ GitHubApiClient.GetReleasesAsync → version compare → List<Update>
   │
   ├─ none found? ──► delete stale assets in <appdata>/updates
   │
   └─ found ──► OnUpdateFound (title-bar Update button)
               └─ IUpdateService.OnUpdatesFound(updates)
                      ├─ pre-download installer + verify SHA256
                      ├─ modal "Update Available" dialog with changelog
                      └─ on confirm ──► IUpdateService.HandleUpdate(latest)
```

## Components

**`UpdateManager`** (`Services/StateManagers/UpdateManager.cs`) — orchestrates the check loop (immediately on startup, then via a 6-hour `PeriodicTimer`). Raises `OnUpdateFound` when new updates exist, clears `<appdata>/updates` when none do, and hosts the SHA256 verification (`IsChecksumCorrect`). `ExplicitCheckUpdates()` is the manual "Check for Updates" trigger; it resets `NotifiedUpdates` so the dialog can show again, and notifies if no updates were found.

**`IUpdateService`** (`Infra/Interfaces/Services/IUpdateService.cs`) with shared base `UpdateServiceBase` (`Services/UpdateServiceBase.cs`) — platform-agnostic logic:
- `CheckForUpdates(includePrerelease)` — fetches releases, parses tags as semantic versions, keeps only those newer than the running version.
- `OnUpdatesFound(updates)` — pre-downloads the installer, shows the update dialog unless the version was already notified (`NotifiedUpdates`), then calls `HandleUpdate` on confirmation.
- `HandleUpdate(update)` — abstract, platform-specific.

Shared helpers download assets with SHA256 checksum verification (checksum from the release asset's `digest` field, retried up to 3 times).

**Platform implementations**, registered as `IUpdateService` in each platform's DI module:
- Windows (`Services/UpdateService.cs`) — downloads `COMPASS_Setup_{version}.exe`, launches it with `/SILENT`, shuts down the app.
- Linux (`Services/UpdateService.cs`) — `HandleUpdate` opens the release page in the browser; no auto-install yet.

## Preferences

`UpdatePreferences` (`Models/Preferences/UpdatePreferences.cs`), stored in the main `Preferences` JSON:

| Property | Default | Purpose |
|---|---|---|
| `CheckForUpdates` | `true` | Enable startup/periodic checking |
| `IncludePrerelease` | `false` | Offer pre-release versions |
| `NotifiedUpdates` | empty | Versions already shown, so the dialog isn't nagged |

## Release requirements

For the Windows updater to work, a release needs a version-parsable tag (e.g. `v2.1.0`) and an asset named `COMPASS_Setup_{version}.exe` whose `digest` is the file's SHA256.

See `Source/COMPASS.Common/App.axaml.cs` (`HandleVersionChanges`) for startup handling of version changes.
