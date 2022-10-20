![COMPASS Banner](Docs/CompassWideLogo.png)

# COMPASS
The Codex Organizer to make Pen-and-paper Adventuring Super Simple or COMPASS for short is a windows application to organize and manage all your TTRPG rulebooks, inspired by [Playnite](https://github.com/JosefNemec/Playnite). 

## :scroll: Story

If you are an RPG fan like me, you probably have lots of pdf's both homebrew and official from sites such as [Drivethrough RPG](https://www.drivethrurpg.com/), [Humble Bundle Books](https://www.humblebundle.com/books),  [GM Binder](https://www.gmbinder.com/), [Homebrewery](https://homebrewery.naturalcrit.com/), [itch.io](https://itch.io/books/genre-rpg) or content from subreddits, patreon creators and kickstarters. Or perhaps you own them in digital form on sites such as [D&D Beyond](https://www.dndbeyond.com/sources#Sourcebooks) or [Roll20](https://roll20.net/compendium/dnd5e/BookIndex).  Keeping track of all these books is hard and nothing is more frustrating than franticly clicking trough folders in search of that one statblock you need in the heat of combat. COMPASS solves this problem by bringing all of your books together in one place that is easily searchable, sortable and filterable. 

## :toolbox: Features

- **Unify** your digital tabletop RPG sourcebook library so everything is in one place.
- **Import** any file or link to a URL for online sources. You can also manually add books to include your physical collection.
- Have **Redundant Access** to your books by having both an offline pdf and a link to an online version hosted on sites such as Google Drive and GM Binder for each book. COMPASS will automatically fallback to another version if the preferred one is unavailable due to a lack of internet for example.
- **Automatic metadata** from PDF's, supported sites and books with an ISBN number thanks to [Open Library](https://openlibrary.org/). 
  
- <img src="Docs/Metadata_support.png" alt="Supported sources table" width="600"/>

- **Categorize** all your books using Tags. You can add Tags for anything you like, some examples:
  - The type of content like *Adventure*, *Monster Stats* or *Setting/Lore*.
  - The edition or ruleset such as *DnD 5e* or *Pathfinder 2e*.
  - The setting such as *Forgotten realms* or *Eberron*.
  - The Genre such as *Horror*, *Fantasy* or *Sci-Fi*.
  - Whatever works for you, you can create tags for everything.
- **Organize** Tags in a folder like structure to retain the advantages of folders, without the need for file duplication.
- **Filter and Sort** all your books by *Title, Author, Publisher, Release date, Personal Rating, ect.* with support for separate sorting titles.
- **Visualize** your library with 4 different layouts to choose from: Home, List, Cards and Tiles (see screenshots).
- Have **Quick Access** to your books thanks to the Home view which lists your favorites, recently opened, most opened and recently added books for your convenience.
- **Group** your books into collections. Each collection has their own list of tags, authors, ect. and helps you keep an overview by reducing the amount of books that are on your screen at once.

## :warning: Why am I getting a Windows Defender SmartScreen Warning when installing COMPASS?

The COMPASS files are not digitally signed which results in these kinds of warnings. Signing code is expensive and not worth it with a small user base. If this project ever takes off and gains a large amount of users, I will look into this again. If you do not trust the executable, you can always build the code from source. You will need to install ghostscript seperatly if you want pdf thumbnails, as it is included in installer but not in the repo. 

## :heart: Supporting and Contributing

I created this project to solve a problem I had and as a learning opportunity. I eventually chose to release it publicly and open source because I believe others might find it useful as well. If you like this project and would like to support it, there are many ways you can contribute.

- If you find a bug, please open a github issue and report it there so it can be fixed. Please include a clear explanation the nature of the bug and if possible steps to reproduce the it.
- If you would like to contribute any code, please communicate what you plan to working on so we don't work on the same things at the same time. All pull requests should be made against the dev branch. Dev gets merged into master with every new release.
- If you have ideas for new features or improvements, you can put those in a github issue as well.
- If you would like to financially support me so I can justify spending more time on this, help with potential costs such as code signing or just show some love, you can buy me a coffee over on [ko-fi](https://ko-fi.com/pauldesmul), I would greatly appreciate it.

## :camera: Screenshots

![Home Layout](Docs/Screenshots/Home_Layout.png)
![List Layout](Docs/Screenshots/List_Layout.png)
![Card Layout](Docs/Screenshots/Card_Layout.png)
![Tile Layout](Docs/Screenshots/Tile_Layout.png)
![Codex Properties](Docs/Screenshots/Codex_Properties.png)
