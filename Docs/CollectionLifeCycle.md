# Collection Lifecycle & the Handle System

## Problem

A `CodexCollection` stores all its data (tags, codices, collection info) in XML files on disk. Loading is expensive and, more importantly, **saving a collection that was never loaded would overwrite existing data with empty lists**, effectively wiping the user's library. COMPASS needs a way to:

1. Know whether a collection is currently loaded before allowing saves.
2. Support **multiple simultaneous consumers** of the same collection (e.g. two tabs showing the same collection, or an import wizard and a tab).
3. Automatically unload and clean up when the last consumer is done.

## Solution: `CollectionHandle`

The handle system uses a **reference-counted ownership** pattern. Nobody interacts with loading/saving/unloading directly on `CodexCollectionVM` — instead, they acquire a `CollectionHandle` and use that.

### Key types

| Type | File | Role |
|---|---|---|
| `CodexCollection` | `Models/CodexCollection.cs` | The data model. Holds `AllTags`, `AllCodices`, `Info`. Also has `LoadedTags`/`LoadedCodices`/`LoadedInfo` guard flags used by the repository layer to prevent saving unloaded sections. |
| `CodexCollectionVM` | `ViewModels/Main/CodexCollectionVM.cs` | The manager for a single collection. Owns the `Owners` list (active handles), the repository reference, and the Load/Unload/Save methods. |
| `CollectionHandle` | `ViewModels/Main/CollectionHandle.cs` | A disposable token representing one consumer's claim on a loaded collection. Calling `Dispose()` releases the claim. |
| `CollectionManager` | `Services/StateManagers/CollectionManager.cs` | Static registry of all known `CodexCollectionVM` instances. Entry point for discovering, creating, and loading collections. |

## Lifecycle

```
CollectionManager
  DiscoverCollections() → creates CodexCollectionVM per folder
                │
         collectionVm.Load()
                │
                ▼
CodexCollectionVM
  Load()
   ├─ If already loaded (Owners not empty):
   │   → Create handle, add to Owners, return it
   │     (skip repo.Load — data is already in memory)
   │
   └─ If not loaded (Owners empty):
       → Call repo.Load(collection)
       → If success: create handle, add to Owners, return it
       → If failure: notify user, return null

  Save(handle) / SaveCodices(handle)
   → Only proceeds if handle is in Owners list
   → Prevents saving with a stale/expired handle

  Unload(handle)
   → Removes handle from Owners
   → If Owners is now empty:
       → repo.Unload(collection)
       → Dispose all CodexViewModels and TagViewModels
       → Clear AllCodexVms and AllTagVms
                │
        handle.Dispose()
                │
                ▼
CollectionHandle
  Implements IDisposable
  Dispose() → calls collectionVm.Unload(this)
  Save()    → calls collectionVm.Save(this)
  Disposed flag prevents double-unload
```

## Who acquires handles and when

| Consumer | How it gets a handle | When it releases |
|---|---|---|
| **`CollectionTabVM`** (a tab) | Calls `collectionVm.Load()` in its constructor. | `Dispose()` when the tab is closed, or when switching to a different collection via `ChangeToCollection()` (disposes old handle, acquires new one). |
| **`CollectionManager.GetOrCreateInitialCollectionVM()`** | Tries to load the startup collection. Falls back through available collections. Creates a default if none exist. | Returns the handle to whichever `CollectionTabVM` requested it. |
| **`ImportCollectionViewModel`** | Loads the satchel-extracted collection to access its codices and tags during the import wizard. | `Dispose()` when the wizard closes. |
| **`BrokenFileRefsToolViewModel`** | Loads each collection to scan for broken paths. | Disposes the handle after scanning. |
| **`SettingsViewModel`** | Loads a collection temporarily for settings changes. | Disposes after applying changes. |
| **`CollectionManager.Save()` extension** | Acquires a short-lived handle via `using var handle = LoadCollection(name)` just to call `Save()`. | Disposed at end of `using` block. |

## Guard flags on `CodexCollection`

The model itself carries three boolean flags:

```
LoadedTags    — set to true by the repository after successfully deserialising Tags.xml
LoadedCodices — set to true by the repository after successfully deserialising CodexInfo.xml
LoadedInfo    — set to true by the repository after successfully deserialising CollectionInfo.xml
```

The XML repository checks these before writing. If `LoadedCodices` is `false`, the repository will **not** write `CodexInfo.xml`, preventing an empty list from overwriting the real data on disk. These flags act as a second safety net below the handle system.

## Common patterns

### Opening a collection in a tab

```csharp
// TabsViewModel creates a new tab, which loads the collection
var tab = new CollectionTabVM(collectionVm);
// Internally: _collectionHandle = collectionVm.Load()
```

### Short-lived access (e.g. save from outside a tab)

```csharp
// CollectionManager extension method
using var handle = LoadCollection(collection.Name);
handle?.Save();
// handle.Dispose() runs at end of using → Unload if last owner
```

## Deleting a collection

`CodexCollectionVM.DeleteCollection()` first checks `Owners.Count > 0` — a collection that is still in use by a tab or import wizard **cannot be deleted**. The caller must ensure all handles are disposed first.