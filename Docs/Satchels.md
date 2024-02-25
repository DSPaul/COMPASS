# Import & export your collection: Satchels

## Overview

.satchel is a file format designed to share data between compass users.
Like many other existing file formats, .satchel is not a binary file but a renamed zip archive.

This means that you can inspect its content outside of COMPASS by renaming it back to .zip and opening it like any other archive. All the files inside are either .xml or .json files which are both human readable and editable.

## Contents

As single .satchel file corresponds to a single collection and can contain any of the following:

- A `SatchelInfo.json` file that contains the version of COMPASS that was used to create the satchel file, as well as the last compatible version for each component. More about compatibility in the next section.
- A `CodexInfo.xml` file containing all the items in the collections with their metadata such as their title, author, ect.
- A `CollectionInfo.xml` file containing info and preferences about the collection such as which filetypes to exclude from import and folder-tag links.
- A `Tags.xml` file containing all the tags for that collection.
- A `CoverArt` folder containing full res versions of the cover art of the items in `CodexInfo.xml`.
- A `Thumbnails` folder containing low res versions of the cover art of the items in `CodexInfo.xml`.
- A `Files` folder that contains files that were referenced by the `path` field of the items.

## Compatibility

Satchel files will always be forward compatible with newer versions of COMPASS, meaning they can be opened in any version of COMPASS that is equal to or greater than the version that was used to create it.

Backwards compatibility however poses challenges, as .satchel files created in newer versions may have changes in their data structure that older versions cannot interpret. Attempting to import these newer files anyway could result in data that has seen changes to its structure, getting lost in the import. It can also result in crashes and even possible file corruption of your existing collections.

Because of these dangers, satchels always include the minimum required version of COMPASS to properly open and interpret each of its 3 xml files (`CodexInfo.xml`, `CollectionInfo.xml` and `Tags.xml`). If any of these xml files are present and marked incompatible, COMPASS will refuse to import the satchel.

If you wish to import such an incompatible .satchel file, you will have to update your install to match the target version. If however you do not want to update, you can always manually modify the necessary files to match the old format by referencing the included changelog, and then change the `SatchelInfo.json` file manually to match your installed version.

## Format changes

### Version 1.7.0

Introduction of the format.
