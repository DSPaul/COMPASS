# CHANGELOG

## COMPASS v1.7.0 (25 February 2024)

### New Features

- IMPORT & EXPORT are here in the form of `Satchels`! In this new update you can:
  - Export collections to a new .satchel file to share with your friends. Exports can include:
    - the tags from a collection
    - the items in the collection (such as books and maps) including their metadata, cover art and even the files referenced by those items such as pdf's.
    - your settings for that collection such as which file types to exclude from import
  - Import those .satchel files to merge all of its content into one of your existing collections or add it as brand new collection.
  - Both during import and export, you can pick and choose which tags, books and settings you want to keep using a user friendly wizard. You can access this new feature by using the new "Import" and "Export" options near the collections at the top.
    - If you only want to import only the tags, you can go to "Import Tags" > "From a .satchel file" in the "Tags" Tab.
    - If you only want to import the items without any tags, you can select "From a .satchel file" in the "Add Books" Tab.
  - You can find more info about the new .satchel file format [here](https://github.com/DSPaul/COMPASS/tree/master/Docs/Satchels.md).
- You can now merge collections. This is especially handy when used in combination with import. If you would like to browse a collection you wish to import before merging it into your existing collections, you can simply import it as a new collection and merge it later.
- Added a new context menu to the tags tab which can be accessed by right clicking anywhere or clicking the vertical triple dots, with the following options
  - Sort tags A->Z. [#31](https://github.com/DSPaul/COMPASS/issues/31)
  - Export Tags.
- Importing from a folder now allows you to exclude certain subfolders. [#58](https://github.com/DSPaul/COMPASS/issues/58)

### Improvements

- Tags created using "quick new tag" are now automatically added to the item being edited.
- COMPASS now saves more often to prevent data loss.
- Info and warning tooltips now appear instantly.
- Added an explainer when zero-padding is applied to the sorting title.
- When auto import tries to import a new file type, it will now always ask what to do with it.
- Increased stability.
- When manually resolving a broken file reference, COMPASS will analyze how the file path changed and attempt to fix other broken links by applying the same change.

### Fixes

- Fixed tags being cleared when quick creating a tag.
- Fix total item count not updating.
- Cards in Card Layout can now resize thanks to an update to the [VirtualizingWrapPanel](https://github.com/sbaeumlisberger/VirtualizingWrapPanel) package. (thanks [@sbaeumlisberger](https://github.com/sbaeumlisberger)) This fixes [#62](https://github.com/DSPaul/COMPASS/issues/62)
- Fix metadata source priority preferences not persisting.

## COMPASS v1.6.0 (6 November 2023)

### Features

- You can now import Tags from other collections [#51](https://github.com/DSPaul/COMPASS/issues/51). This new way of adding tags to a collection can be found in the slightly redesigned Tags Tab, next to creating new tags and tag groups. 

### Improvements

- The "Update Found" window now displays the COMPASS logo rather than the AutoUpdater.NET logo.
- Flipped the webcam preview when scanning IBAN barcodes so it mirrors your movements making it much easier to align.
- Titles that contain numbers will now sort as expected ("book-1","book-2","book-10" instead of "book-1","book-10","book-2").
- Improved changelog display when update is found.
- Make the UI to add a folder to check for auto import more intuitive.
- Various improvements to author selection, including [#46](https://github.com/DSPaul/COMPASS/issues/46)
- Renamed "Delete" To "Remove" to clarify that items are removed from COMPASS and no user files are deleted in the process.
- Mark buttons that open additional dialogs with "..." in correspondence with industry conventions.

### Fixes

- Fixed a crash when finishing choosing metadata.
- Fixed an issue where rows without tags could not be selected.
- Folder-Tag are now deleted when the associated tag is deleted.
- Fixed wrong tooltip text on remove Folder-Tag link button.
- Fixed unable to scroll through authors.
- Fixed missing warning icon in some dialogs.
- Fixed webdriver initialisation failing and related crash. [#60](https://github.com/DSPaul/COMPASS/issues/60)
- Fixed selecting an item causing a scroll jump. [#61](https://github.com/DSPaul/COMPASS/issues/61)
- Various smaller fixes.

## COMPASS v1.5.1 (20 August 2023)

### Fixes

- Fixed a crash when finishing the "choose metadata wizard"
- Fixed a crash during search when there are items with empty titles

## COMPASS v1.5.0 (20 May 2023)

### New Features

- Added a priority system for metadata sources so you can choose which metadata values should be used when multiple sources can provide data for a certain field. This also applies to the thumbnail and cover of books. You can configure the priority in the new `Metadata` tab in the settings. [#52](https://github.com/DSPaul/COMPASS/issues/52)
- Added the option to renew metadata for items in your collections. This way you can get metadata for files already in your collections making use of all the new features, metadata fields and sources that may not have existed when the file was initially imported. The new system will search for metadata in all of the item's sources (a local file, a URL and ISBN number) and combine them using the previously added priority order and overwrite the old value with the new one according to your preferences which you can also configure in the new `Metadata` tab in the settings.
- Added "Has ISBN" filter.
- Added "Website" filter.
- Added "Show in Open Library" button next to the ISBN number.
- Added a new entry in the context menu (right click) to renew the cover, to complement the one to renew metadata. [#54](https://github.com/DSPaul/COMPASS/issues/54)
- Add "Date Added" & "ISBN" column to List Layout. [#55](https://github.com/DSPaul/COMPASS/issues/55)
- Added the options to cancel background tasks. 
  
### Improvements

- Added virtualization for large collections which massively improves load times at the cost of some small stutters while scrolling. Because of this trade-off, the collection's size at which virtualization is enabled can be configured in the `Preferences` tab of the settings. [#22](https://github.com/DSPaul/COMPASS/issues/22)
- Various improvements to the `Card Layout` including faster rendering, more responsive resizing and clearer icons.
There is a know issue where the bottom of a card might get cut off if it has many tags, this will get fixed when the [Virtualizing WrapPanel](https://github.com/sbaeumlisberger/VirtualizingWrapPanel) dependecy updates to 2.0.
- The progressbar at the bottom is now used for more things, giving better insight into what COMPASS is doing in the background.
- The import procedure has been split up in three steps: adding all the files, getting metadata for those files, and finally getting the cover for them. This is in constrast to previous versions where these three steps would be done for one file, before moving on to the next one. The new method significantly speeds up import for large imports and makes is less likely to lose your data if the import is interupted.
- The "Fetch Cover" button in the edit window has been renamed to "renew" and will now look for covers in the order specified by the new priority system.

### Fixes

- UI elements that can become scrollable will no longer prevent scrolling when they are not scrollable yet themselves.
- COMPASS no longer freezes when reading in files during import.
- Fixed COMPASS using empty covers form openlibrary.org.
- Fixed broken ISBN detection in pdf's in some cases.
- Files with capitalized file extensions will now be handled properly.
- Fix progress counter having the wrong value.
- Various other small fixes.

### Other changes

- Moved Folder - Tag linking from `import` tab to the new `metadata` tab.

## COMPASS v1.4.2 (23 April 2023)

### Fixes

 - Fix another possible crash
 - Fix fuzzy matching being too fuzzy
## COMPASS v1.4.1 (23 April 2023)

### Fixes

 - Fix crash during import

## COMPASS v1.4.0 (23 April 2023)

### New Features
  
- You can now link folders to tags! During import, this will add any tags to files that have the linked folders in their filepath. Tags and folders with the same name will be treated as linked by default, but this can be disabled in the settings.
This new feaures and related options can be found in the `import` tab of the settings. [#25](https://github.com/DSPaul/COMPASS/issues/25)

### Improvements

- You can now choose folders to automatically import, using a folder selection dialog.
- All lists in the `sources` tab are now sorted alphabetically.
- Clicking the version number in the bottom right will take you to the "about" section of the settings.
- Added some info icons next to some features which show a better explanation of the feature on hover.  
- Added "Report a Bug" button to settings dropdown which will bring you to the linktree with all the social channels where you can do so, including a new google form for completely anonymous and account free bug reporting.
  
### Fixes

- Fixed the "Show in Explorer" button in "folders to auto import" not working.
- Fixed inconsitent tooltip styling.
- Fixed a crash when trying to open mutliple books at once.

### Other Changes

- Renamed the `sources` tab of the settings to `import`.
- Added the subreddit and website to the `about` section.

## COMPASS v1.3.0 (01 April 2023)

### New Features

- Whenever you import a folder, COMPASS will now check that folder for new files on each startup and automatically import any new files it finds there. Your file type preferences will also be applied to the automatic imports. This features can be disabled during the folder import process. [#26](https://github.com/DSPaul/COMPASS/issues/26)
- A new "Delete Forever" option was added which prevents files from automatically being imported again, which is intended to complement the new automatic import feature.
- You can manage both the folders to check for new files and the list of banished (deleted forever) files in the new `sources` tab of the settings.
- Added a new ribbon along the bottom with extra info such as:
  - How many files are present in a collection and how many of them match all the filters. [#39](https://github.com/DSPaul/COMPASS/issues/39)
  - Progress indication when tasks are running in the background.
  
### Improvements

- Importing is now multithreaded which massively reduces import times.
- `What's new` now shows the full changelog and got a visual overhaul.

### Fixes

- Fixed multiple of bugs related to missing "/" in paths
- Fixed covers sometimes having extra whitespace around it
- Fixed logger no longer logging
- Fixed empty popup when importing 0 files
- Fixed logo disappearing when opening the "about" tab of the settings

## COMPASS v1.2.0 (21 March 2023)

### New Features

- The file path to your user data can now be changed from `%appdata%` to any other location. This enables cloud syncing your data by moving it to a cloud synced folder like a google drive, dropbox or onedrive directory on your pc or even to a network attached storage solution. User data includes your collections, tags, metadata and thumbnails. Note that moving your data to a slower drive might increase load times. 

### Fixes

- fix dark cursors in some places
- fix file paths to covers and thumbnails being broken and related crashes

## COMPASS v1.1.4 (17 March 2023)

### Fixes

- fix crash when trying to add a newly created tag to a book
## COMPASS v1.1.3 (12 March 2023)

### Fixes

- Fixes tags getting deleted when importing a new file
## COMPASS v1.1.2 (11 March 2023)

### Improvements

- Improved the bulk edit UI/UX to make it more intuitive. [#33](https://github.com/DSPaul/COMPASS/issues/33)
- Improved functionality of the scrollbar with arrow buttons and snap scrolling by clicking on the desired scroll location.
- Limited the width of tags section in edit windows. [#41](https://github.com/DSPaul/COMPASS/issues/41)
- The tags of a book are now displayed in the same order as your tree of tags, top to bottom. [#37](https://github.com/DSPaul/COMPASS/issues/37)
- You can now select multiple folders when adding books from folder.
- Import files and folders by dragginng and dropping them onto a layout. [#29](https://github.com/DSPaul/COMPASS/issues/29)
- Change the cover by dragging and dropping an image onto the cover in the edit window.
- Added option to auto hide the info panel if nothing is selected. On by default. Found in Layouts Tab.
- Clicking the selected tab on the left will hide collapse it.
- Holding `Alt` while selecting filters will send them to the **Excluded** filters section.
- Press `F5` to reload the current collection.
  
### Fixes

- fixed a bug where Tags could have the same ID
- fixed a bug where the "new tag" and "new group" dropdowns were linked
- fixed broken look of horizontal scrollbar
- fixed "Failed to import metadata" on pdf's whith less than 5 pages
## COMPASS v1.1.1 (04 March 2023)

### Fixes

- Fix all tags expanding whenever a tag gets edited, added or deleted [#32](https://github.com/DSPaul/COMPASS/issues/32)
- Fix a crash when trying to create multiple tags back to back [#34](https://github.com/DSPaul/COMPASS/issues/34)

## COMPASS v1.1.0 (04 March 2023)

### New Features

- Added new Info panel on the right side, can be toggled with **Ctrl+I** or using the new  button in the top right or through the new "show info" option in the context menu. The Authors, publisher and tags are also clickable to quickly add the corresponding filter.
- Added a new color option to tags: "Same as parent". Group tags now also have a color to work in tandem with this feature, which is shown with a little stripe in front. [#27](https://github.com/DSPaul/COMPASS/issues/27)
- You can now right click a tag to create a new subtag which has its parent's color by default. [#27](https://github.com/DSPaul/COMPASS/issues/27)
- Pdf's that contain ISBN numbers in the first 5 pages will now automatically detect it and use it to fetch metadata. [#21](https://github.com/DSPaul/COMPASS/issues/21)
- Added new options to deal with broken file references [#23](https://github.com/DSPaul/COMPASS/issues/23)
  - In the *Manage Data* section of the settings:
    - Added message telling you how many files have a broken reference.
    - Clicking "show" will add a new filter that shows which files have a broken reference.
    - Added a button to remove all the broken references from the books.
    - Added a button to delete all books that have broken references.
  - When trying to open a file with a broken file reference:
    - Added option to open a file selection dialog to find the file and immediately  open it.
    - Added option to remove the broken file reference.
    - Added option to delete the book.

### Enhancements

- Added a button to quickly create a Tag while editing a book.
- Renamed the default collection as follows: "Default" -> "Default Collection" to communicate the collections feature better.
- The update path references tool now only updates the filepath if the old file path pointed to a file that no longer exists in that location and the new filepath points to a file that does exists, rather than just blindly renaming paths.
- Increased thickness of resizing border.
- All tags are now expanded by default instead of only groups.
- The tag name textbox is now automatically focussed on loading.

### Fixes

- You cannot create tags with empty names anymore.
- Only show the warning for no internet connection once instead of every 10 seconds.
- If a crash occurs during import, it no longer loses all progress. [#19](https://github.com/DSPaul/COMPASS/issues/19)
- Fixed crash on startup if data file is corrupted, it will now try to open another collection instead or create a new one if there aren't any others. [#20](https://github.com/DSPaul/COMPASS/issues/20)
- Fixed save data corrupting due to illegal characters in imported data. [#20](https://github.com/DSPaul/COMPASS/issues/20)
- Fixed "Check for Updates" button not working.
- Removed unnecessary reloads.
  
### Other Changes

- I made a discord! This way people can more easily get help and give feedback without the need for a github account, you can join with the following link: <https://discord.gg/HawGMJgS9Y>.
- There is also a [subreddit](https://www.reddit.com/r/compassapp) for people who prefer that.

## COMPASS v1.0.0 (25 February 2023)

### New Features / Enhancements
  
- New advanced tab was added to the *Edit Codex* window with COMPASS specific metadata like how often you opened a book and how long ago
- You can now filter by favorite and filetype (file extension), all online sources are currently considered a "webpage" filetype
- Major UI/UX redesign, most user actions have been moved to the into tabs on the left side of the screen.
- The dock on the left can now be collapsed
- You can now inlcude/exclude filters and tags by flipping a toggle before selecting the filter in addition to drag&drop
- You can now disabling URL validation on homebrewery import to support from self hosted instances of homebrewery
- The *Delete* key now works in all layouts to remove books from COMPASS, a warning before deleting was also added
- ISBN number was added to the sources tab of a codex
- Keyboard Shortcuts were added to interact with books
  - **Enter**  to open
  - **Ctrl+E** to Edit
  - **Ctrl+F** to Favorite
  - **Delete** to Delete
  - **Ctrl+S** to search
- Added more customisation to the Tile layout, you can now have the option to display the title, author, publisher or user rating underneath the cover.
- You can now Drag and Drop tags from Tags tab directly onto books to tag them.
- Added the option to open Edit Window after import so you can review and complete the auto imported metadata
- Added Activity log that saves errors and warning that used to just disappear so you have time to read them

### Fixes

- Fixed not being able to scroll on home view
- Fixed Window moving behind other windows when adding books completes
- Fixed multiple bugs related to sorting
- Fixed text not wrapping in multiple places
- Fixed the Delete Key not properly deleting codices in List View
- Fixed crash when moving files without cover art
- Fixed various other small bugs
- Added restrictions to collection names

### Other Changes

- Update to .NET 7.0

## COMPASS v0.9.1 (14 January 2023)

### HotFix

- Fix Drag and drop on filters not working

## COMPASS v0.9.0 (11 January 2023)

### Fixes

- Fixed crash when trying to fetch a book cover from an online source.
- Multiple small fixes related to selecting tags.
- Fixed Rating bar being invisible.

### New Features

- Tags and metadata filters can now be both inclusive or exclusive, so you can choose whether books with a certain tag or metadata field should be included or excluded from the fitered list. To use the new exclusive filters, jus drag and drop them to the new clearly labeled filter area at the top.
  
### Enhancements

- Added loading animation in some places.
- Added the option to drag tags to the filters area at the top.

### Other Changes

- After reviewing the ghostscript license, I have decided to include it in the repo so it no longer needs to be installed to build from source.

## COMPASS v0.8.0 (04 December 2022)

### Fixes

- Fixed the no-internet indicator being too small.
- Fixed crashes when importing corrupted files.
- Fixed multiple bugs related to the contextmenu on the home layout.
- Fixed "check for updates button" not doing anything if you chose to remind later.

### New Features

- Added importing entire directories at once, including subdirectories. You can choose which filetypes to include in the import.

### Enhancements

- Overhauled the contextmenu when multiple books are selected, including clearer text and icons.
## COMPASS v0.7.0 (19 October 2022)

### Fixes

- Changelog popup after updates should not be blank anymore thanks to AutoUpdater.NET Update.
- Fixed deleted files still being shown.

### New Features
  
- You can now scan barcodes using a webcam to import books based on their ISBN number.

### Enhancements

- All file types can now be imported instead of only pdf's. Only pdf's and images will have thumbnails.

## COMPASS v0.6.2 (06 August 2022)

### Fixes

- Fixed Welcome screen not disappearing when a codex is added
- Pasting authors now works as expected (thanks to BlackPearl library update)
- Fixed reordering Tags causing duplicates
- Fixed Lists on home screen not updating on newly added codices

## COMPASS v0.6.1 (24 July 2022)

### Fixes

- Fix Crash when update is found
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