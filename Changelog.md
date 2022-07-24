# CHANGELOG
## COMPASS v0.6.0 (24 July 2022)

### Fixes

- Fix a bug that caused all user data to be deleted when trying to delete a collection with an empty name.
- Added exit button when importing, it used to be impossible to close the window if import failed.
- Fixed COMPASS crashing when preferences.xml didn't exist yet.
- The minimum rating filter now resets when all filters are reset.
- Calendar background is now dark so you can read the white text.

### Enhancements

- Most contextmenus have icons now.
- Certain textboxes now have focus by default.
- You can press enter to confirm instead of clicking in more places.
- You can now favorite books from the contextmenu.
- Added tooltips to lots of buttons.
- There are now a welcome screen with simple instructions when opening an empty selection.
- Add "About" Sections in settings.

### New Features

- Added ISBN as an import option, all metadata and the cover are fetched from <https://openlibrary.org>.
- Added options to create and restore a backup of user data.

### Other Changes

- Changed icons to edit and delete collections.

## COMPASS v0.5.0 (16 July 2022)

### Fixes

- Fix Import button disappearing when importing from URL
- Fix cover download failing sometimes
- Fix COMPASS sometimes crashing when downloading cover
- Restored sorting in Tile Layout
- Fixed "Move to Collection" not working in certain layouts

### Enhancements

- Downloading covers from homebrewery and gmbinder no longer requires COMPASS to be run as administrator
- All Layout options (cover size and title visibility) now persist in Home Layout
- Cover size now persists in Tile Layout

### New Features

- Added support for multiple authors
- Added separate sorting titles

### Other Changes

- Removed redundant sorting options in Home Layout

## COMPASS v0.4.0 (29 June 2022)

### Fixes

- Fixed Adding tags not working
- Fix some inconsistent UI elements

### Enhancements

- various UI improvements

### New Features

- Added new Home layout
- Added option to favorite a book
- Track when and how often books are opened

## COMPASS v0.3.1 (16 June 2022)

### Fixes

- Fixed crash on startup when update is found

## COMPASS v0.3.0 (16 June 2022)

### Fixes

- Fixed crash when trying to open release notes in settings
- COMPASS now installs to "/Program Files/" by default instead of "/Program Files (x86)/"
- Fixed crash when launching COMPASS for the very first time
- User preferences are now retained across updates

### Enhancements

- Added option to delete user data on uninstall
- The icon of the used view (list, cards, tiles) is now highlighted
- Clicking "browse" in the sources section of files now opens in the location of the file
- Search now tolerates typos and accepts abbreviations

### New Features

- Logging has been added to facilitate reporting issues
- Added "Show in Explorer" option to file context menu
- Added a way to update the file path references in all files in a collection when you rename a folder

### Other Changes

- renamed "Changelog" tab in settings to "What's New"

## COMPASS v0.2.0 (14 June 2022)

### Fixes

- Importing a homebrewery file would sometimes fail
- Metadata fields of homebrewery were cut short if they contained a comma

### New Features

- Google drive import has been added, supported metadata is:
  - Cover
  - Title
- Automatic update checking and installing

## COMPASS v0.1.0 (10 June 2022)

- Release of 0.1.0
