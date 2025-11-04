using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.DependencyInjection;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models;
using COMPASS.Common.Models.CodexProperties;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Models.Hierarchy;
using COMPASS.Common.Services;
using COMPASS.Common.Services.FileSystem;
using COMPASS.Common.Sources;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Common.ViewModels.Modals;
using COMPASS.Common.ViewModels.Modals.Edit;
using COMPASS.Common.Views.Windows;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Infra.ExtensionMethods;
using COMPASS.Infra.Tools;

namespace COMPASS.Common.Operations
{
    public class CodexOperations
    {
        public CodexOperations(CollectionHandle collectionHandle)
        {
            _collectionHandle = collectionHandle;
        }
        
        /// <summary>
        /// the handle of the collection containing the codices on which these operations works
        /// </summary>
        private readonly CollectionHandle _collectionHandle;
        
        #region Open Codex

        //Open Codex wherever
        public static async Task<bool> OpenCodex(Codex codex)
        {
            var openPriority = PreferencesService.GetInstance().Preferences.OpenCodexPriority;
            bool success = await PreferableFunction<Codex>.TryFunctionsAsync(openPriority, codex);
            if (!success)
            {
                Notification notification = new("Could not open item", "Could not open item, please check its sources");
                await ServiceResolver.Resolve<INotificationService>().ShowDialog(notification);
            }

            return success;
        }

        //Open codex Offline
        private AsyncRelayCommand<Codex>? _openCodexLocallyCommand;
        public AsyncRelayCommand<Codex> OpenCodexLocallyCommand => _openCodexLocallyCommand ??= new(OpenCodexLocally, CanOpenCodexLocally);
        public static async Task<bool> OpenCodexLocally(Codex? toOpen)
        {
            if (toOpen is null) return false;
            if (!toOpen.Sources.HasOfflineSource()) return false;
            try
            {
                Process.Start(new ProcessStartInfo(toOpen.Sources.Path) { UseShellExecute = true });
                toOpen.LastOpened = DateTime.Now;
                toOpen.OpenedCount++;
                Logger.Info($"Opened {toOpen.Sources.Path}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Warn($"Failed to open {toOpen.Sources.Path}", ex);

                var fileNotFoundVM = new FileNotFoundViewModel(toOpen);
                await new ModalWindow(fileNotFoundVM).ShowDialog(App.MainWindow);
                return fileNotFoundVM.FixedAndOpenedCodex;
            }
        }
        public static bool CanOpenCodexLocally(Codex? toOpen)
        {
            if (toOpen == null) return false;

            return toOpen.Sources.HasOfflineSource();
        }

        //Open codex Online
        private RelayCommand<Codex>? _openCodexOnlineCommand;
        public RelayCommand<Codex> OpenCodexOnlineCommand => _openCodexOnlineCommand ??= new(codex => OpenCodexOnline(codex), CanOpenCodexOnline);
        public static bool OpenCodexOnline(Codex? toOpen)
        {
            if (!CanOpenCodexOnline(toOpen)) return false;
            try
            {
                Process.Start(new ProcessStartInfo(toOpen!.Sources.SourceURL) { UseShellExecute = true });
                toOpen.LastOpened = DateTime.Now;
                toOpen.OpenedCount++;
                Logger.Info($"Opened {toOpen.Sources.SourceURL}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to open {toOpen!.Sources.SourceURL}", ex);
                //fails if no internet, pinging 8.8.8.8 DNS instead of server because some sites like gm binder block ping
                if (!IOService.PingURL()) Logger.Warn($"Cannot open this item online when not connected to the internet", ex);
                return false;
            }

        }
        public static bool CanOpenCodexOnline(Codex? toOpen)
        {
            if (toOpen is null) return false;

            return toOpen.Sources.HasOnlineSource();
        }

        //Open Multiple Files
        private AsyncRelayCommand<IList>? _openSelectedCodicesCommand;
        public AsyncRelayCommand<IList> OpenSelectedCodicesCommand => 
            _openSelectedCodicesCommand ??= new(async l => await OpenSelectedCodices(l?.Cast<Codex>().ToList()));
        public static async Task<bool> OpenSelectedCodices(IList<Codex>? toOpen)
        {
            if (!toOpen.SafeAny()) return false;

            if (toOpen.Count == 1)
            {
                return await OpenCodex(toOpen.First());
            }

            Notification notification = Notification.AreYouSureNotification;
            notification.Body = "You are about to open " + toOpen.Count + " items. Are you sure you wish to continue?";
            await ServiceResolver.Resolve<INotificationService>().ShowDialog(notification);

            if (notification.Result == NotificationAction.Confirm)
            {
                foreach (Codex f in toOpen)
                {
                    await OpenCodex(f).ConfigureAwait(false);
                }

                return true;
            }
            return false;
        }

        #endregion

        #region Edit Codex

        //Edit File
        private AsyncRelayCommand<Codex>? _editCodexCommand;
        public AsyncRelayCommand<Codex> EditCodexCommand => _editCodexCommand ??= new(EditCodex);
        public async Task EditCodex(Codex? toEdit)
        {
            if (toEdit is null) return;
            ModalWindow editWindow = new(new CodexEditViewModel(toEdit: toEdit));
            await editWindow.ShowDialog(App.MainWindow);
        }

        //Edit Multiple files
        private AsyncRelayCommand<IList>? _editCodicesCommand;
        public AsyncRelayCommand<IList> EditCodicesCommand => _editCodicesCommand ??= new(EditCodices);
        public async Task EditCodices(IList? toEdit)
        {
            List<Codex>? toEditList = toEdit?.Cast<Codex>().ToList();
            if (!toEditList.SafeAny()) return;

            if (toEditList.Count == 1)
            {
                await EditCodex(toEditList.First());
                return;
            }

            CodexBulkEditViewModel vm = new(toEdit: toEditList);
            ModalWindow window = new(vm);
            await window.ShowDialog(App.MainWindow);
        }

        #endregion

        #region Create Codex

        public static Codex CreateNewCodex(CodexCollection collection)
        {
            var codex = new Codex(collection);
            codex.Id = Utils.GetAvailableId(collection.AllCodices);
            ServiceResolver.Resolve<IThumbnailStorageService>().InitCodexImagePaths(codex);
            return codex;
        }

        #endregion

        #region Toggle Favorite 

        //Toggle Favorite
        private RelayCommand<Codex>? _favoriteCodexCommand;
        public RelayCommand<Codex> FavoriteCodexCommand => _favoriteCodexCommand ??= new(FavoriteCodex);
        public static void FavoriteCodex(Codex? toFavorite)
        {
            if (toFavorite is null) return;
            toFavorite.Favorite = !toFavorite.Favorite;
            string prefix = toFavorite.Favorite ? "Favorited" : "Unfavorited";
            Logger.Info($"{prefix} {toFavorite.Title}");
        }

        //Toggle Favorite
        private RelayCommand<IList>? _favoriteCodicesCommand;
        public RelayCommand<IList> FavoriteCodicesCommand => _favoriteCodicesCommand ??= new(FavoriteCodices);
        private static void FavoriteCodices(IList? toFavorite)
        {
            List<Codex>? toFavoriteList = toFavorite?.Cast<Codex>().ToList();
            if (!toFavoriteList.SafeAny()) return;
            if (toFavoriteList.Count == 1)
            {
                FavoriteCodex(toFavoriteList.First());
                return;
            }

            // if at least one is not favorited, set all to favorite
            // if all are already favorited, unfavorite all
            bool newVal = toFavoriteList.Any(c => !c.Favorite);
            foreach (Codex codex in toFavoriteList)
            {
                codex.Favorite = newVal;
                Logger.Info($"{(newVal ? "Favorited" : "Unfavorited")} {codex.Title}");
            }
        }

        #endregion

        //Show in Explorer
        private RelayCommand<Codex>? _showInExplorerCommand;
        public RelayCommand<Codex> ShowInExplorerCommand => _showInExplorerCommand ??= new(ShowInExplorer, CanOpenCodexLocally);
        public static void ShowInExplorer(Codex? toShow)
        {
            if (string.IsNullOrEmpty(toShow?.Sources.Path) || !File.Exists(toShow.Sources.Path)) return;
            string? folderPath = Path.GetDirectoryName(toShow.Sources.Path);
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath)) return;
            IOService.ShowInExplorer(folderPath);
        }

        //Move Codex to other CodexCollection
        private AsyncRelayCommand<IList<object>>? _moveToCollectionCommand;
        public AsyncRelayCommand<IList<object>> MoveToCollectionCommand => _moveToCollectionCommand ??= new(MoveToCollection);
        public async Task MoveToCollection(IList<object>? par)
        {
            if (par == null) return;

            if (par.Count != 2)
            {
                Logger.Debug($"Move to collection contained {par.Count} parameters, should be 2");
                return;
            }

            //par contains 2 parameters
            string targetCollectionIdentifier = (string)par[0];
            List<Codex> toMoveList = par[1] switch
            {
                Codex codex => [codex],
                IList list => list.Cast<Codex>().ToList(),
                _ => []
            };

            await MoveToCollection(targetCollectionIdentifier, toMoveList);
        }

        /// <summary>
        /// Moves all codices from the toMoveList to the targetCollection
        /// </summary>
        /// <param name="targetCollectionIdentifier"></param>
        /// <param name="toMoveList"></param>
        private async Task MoveToCollection(string targetCollectionIdentifier, List<Codex> toMoveList)
        {
            if (!toMoveList.Any())
            {
                return;
            }

            //To move should all belong to the same collection
            CodexCollection sourceCollection = toMoveList[0].Collection;
            Debug.Assert(toMoveList.All(codex => codex.Collection == sourceCollection));

            //Check if target Collection is valid
            if (targetCollectionIdentifier == sourceCollection.Name)
            {
                Logger.Warn($"Target Collection {targetCollectionIdentifier} is invalid");
                return;
            }

            //"Are you Sure?"
            var windowedNotificationService = ServiceResolver.Resolve<INotificationService>();
            var thumbnailStorageService = ServiceResolver.Resolve<IThumbnailStorageService>();
            var userFilesStorageService = ServiceResolver.Resolve<IUserFilesStorageService>();

            string messageSingle = $"Moving  {toMoveList[0].Title} to {targetCollectionIdentifier} will remove all tags from the item, are you sure you wish to continue?";
            string messageMultiple = $"Moving these {toMoveList.Count} items to {targetCollectionIdentifier} will remove all tags from these items, are you sure you wish to continue?";

            Notification areYouSureNotification = Notification.AreYouSureNotification;
            areYouSureNotification.Body = toMoveList.Count == 1 ? messageSingle : messageMultiple;
            await windowedNotificationService.ShowDialog(areYouSureNotification);

            if (areYouSureNotification.Result == NotificationAction.Confirm)
            {
                using CollectionHandle? targetCollectionHandle = CollectionManager.LoadCollection(targetCollectionIdentifier);
                if (targetCollectionHandle == null)
                {
                    Notification errorNotification = new("Target collection could not be loaded.", $"Could not move items to {targetCollectionIdentifier}",
                        Severity.Error);
                    await windowedNotificationService.ShowDialog(errorNotification);
                    return;
                }
                
                CodexCollection targetCollection = targetCollectionHandle.CollectionVM.Collection;
                
                //Copy the codices to the target collection
                foreach (Codex toMove in toMoveList)
                {
                    Codex movedCodex = new(targetCollection);
                    movedCodex.CopyFrom(toMove);

                    movedCodex.Tags.Clear();
                    movedCodex.Id = Utils.GetAvailableId(targetCollection.AllCodices);

                    //Add Codex to target CodexCollection
                    targetCollection.AllCodices.Add(movedCodex);

                    thumbnailStorageService.MoveCodexDataToCollection(movedCodex, targetCollection);
                    userFilesStorageService.MoveCodexDataToCollection(movedCodex, targetCollection, toMove.Collection);

                    Logger.Info($"Moved {movedCodex.Title} from {sourceCollection.Name} to {targetCollection.Name}");
                }

                //After they are all copied, delete them
                await DeleteCodices(toMoveList, false);

                //Save source and target collections
                _collectionHandle.SaveCodices();
                targetCollectionHandle.SaveCodices();
            }
        }

        //Delete Codex
        private AsyncRelayCommand<Codex>? _deleteCodexCommand;
        public AsyncRelayCommand<Codex> DeleteCodexCommand => _deleteCodexCommand ??= new(DeleteCodex);
        public async Task DeleteCodex(Codex? toDelete)
        {
            if (toDelete == null) return;
            await DeleteCodices([toDelete], true);
        }

        //Delete Codices
        private AsyncRelayCommand<IList>? _deleteCodicesCommand;
        public AsyncRelayCommand<IList> DeleteCodicesCommand => _deleteCodicesCommand ??= new(async (codices) =>
        {
            var codicesToDelete = codices?.Cast<Codex>().ToList() ?? [];
            await DeleteCodices(codicesToDelete, true);
        });
        public async Task DeleteCodices(IList<Codex> codicesToDelete, bool askForConfirmation)
        {
            if (!codicesToDelete.Any()) return;
            
            var thumbnailStorageService = ServiceResolver.Resolve<IThumbnailStorageService>();

            Notification deleteWarnNotification = Notification.AreYouSureNotification;
            if (askForConfirmation)
            {
                deleteWarnNotification.Body = $"You are about to remove {codicesToDelete.Count} item{(codicesToDelete.Count > 1 ? @"s" : @"")}. " +
                                              $"This cannot be undone. " +
                                              $"Are you sure you want to continue?";
                var windowedNotificationService = ServiceResolver.Resolve<INotificationService>();
                await windowedNotificationService.ShowDialog(deleteWarnNotification);
            }

            if (askForConfirmation && deleteWarnNotification.Result != NotificationAction.Confirm)
            {
                return;
            }

            var codicesByCollections = codicesToDelete.GroupBy(codex => codex.Collection);
            foreach (var group in codicesByCollections)
            {
                CodexCollection collection = group.Key;
                using var collectionHandle = CollectionManager.LoadCollection(collection.Name);

                if (collectionHandle == null)
                {
                    //TODO deal with having to delete codices from a collection that cannot be loaded
                    //Shouldn't happen
                    return;
                }
                
                foreach (Codex codexToDelete in group)
                {
                    //Delete codex from all lists
                    collection.AllCodices.Remove(codexToDelete);
                    thumbnailStorageService.OnCodexDeleted(codexToDelete);

                    Logger.Info($"Removed {codexToDelete.Title} from {collection.Name}");
                    codexToDelete.Dispose();
                }
                
                collectionHandle.SaveCodices();
            }
        }

        //Banish Codex
        private AsyncRelayCommand<Codex>? _banishCodexCommand;
        public AsyncRelayCommand<Codex> BanishCodexCommand => _banishCodexCommand ??= new(async (codex) =>
        {
            if (codex == null) return;
            await BanishCodices(new List<Codex>() { codex });
        });

        //Banish Codices
        private AsyncRelayCommand<IList>? _banishCodicesCommand;
        public AsyncRelayCommand<IList> BanishCodicesCommand => _banishCodicesCommand ??= new(BanishCodices);
        private async Task BanishCodices(IList? toBanish)
        {
            var codicesToBanish = toBanish?.Cast<Codex>().ToList() ?? [];
            if (!codicesToBanish.SafeAny()) return;

            var codicesByCollections = codicesToBanish.GroupBy(codex => codex.Collection);
            foreach (var codicesForCollection in codicesByCollections)
            {
                CodexCollection collection = codicesForCollection.Key;
                collection.BanishCodices(codicesForCollection.ToList());
            }
            
            await DeleteCodices(codicesToBanish, true);
        }

        private AsyncRelayCommand<Codex>? _getMetaDataCommand;
        public AsyncRelayCommand<Codex> GetMetaDataCommand => _getMetaDataCommand ??= new(StartGetMetaDataProcess);
        public async Task StartGetMetaDataProcess(Codex? codex)
        {
            try
            {
                if (codex == null) { return; }
                await StartGetMetaDataProcess(new List<Codex>() { codex });
            }
            catch (OperationCanceledException ex)
            {
                Logger.Warn("Renewing metadata has been cancelled", ex);
                await Task.Run(() => ProgressViewModel.GetInstance().ConfirmCancellation());
            }
        }
        public async Task StartGetMetaDataProcess(IList<Codex> codices)
        {            
            if (!codices.Any()) return;

            //Because this is async and could continue when the tab or collection has already been closed
            //This method should get its own handle
            using CollectionHandle localCollectionHandle = CollectionManager.LoadCollection(_collectionHandle.CollectionVM.Identifier)!;
            
            var progressVM = ProgressViewModel.GetInstance();
            progressVM.ResetCounter();
            progressVM.Text = "Getting MetaData";
            progressVM.TotalAmount = codices.Count;

            ParallelOptions parallelOptions = new()
            {
                MaxDegreeOfParallelism = Math.Max(Environment.ProcessorCount / 2, 1)
            };

            ChooseMetaDataViewModel chooseMetaDataVM = new();

            try
            {
                await Parallel.ForEachAsync(codices, parallelOptions, async (codex, _) => await GetMetaData(codex, chooseMetaDataVM));
            }
            catch (OperationCanceledException)
            {
                ProgressViewModel.GetInstance().ConfirmCancellation();
            }

            if (chooseMetaDataVM.MetaDataProposals.Any())
            {
                ModalWindow window = new(chooseMetaDataVM);
                await window.ShowDialog(App.MainWindow);
            }
            
            //Save at the end
            localCollectionHandle.SaveCodices();
        }
        private static async Task GetMetaData(Codex codex, ChooseMetaDataViewModel chooseMetaDataVM)
        {
            SourceMetaData existingMetaData = new(codex);

            // Lazy load metadata from all the sources, use dict to store
            Dictionary<MetaDataSourceType, SourceMetaData> metaDataFromSource = new();

            //First try to get sources from other sources
            //Pdf can contain ISBN number
            PdfMetaDataSource pdfSource = new(codex.Collection);
            if (pdfSource.IsValidSource(codex.Sources) && string.IsNullOrEmpty(codex.Sources.ISBN))
            {
                SourceMetaData pdfData = await pdfSource.GetMetaData(codex.Sources);

                //already store this so pdf doesn't need to be opened twice
                metaDataFromSource.Add(MetaDataSourceType.PDF, pdfData);
            }

            //metadata that will be shown to the user, and asked if they want to use it
            SourceMetaData toAsk = new();
            bool shouldAsk = false;

            //Iterate over all the properties and set them
            foreach (var prop in PreferencesService.GetInstance().Preferences.ImportableCodexProperties)
            {
                if (prop.OverwriteMode == MetaDataOverwriteMode.Never) continue;
                if (prop is CoverProperty) continue; //Covers are done separately

                //preferredMetadata will hold the metadata from the top preferred source
                SourceMetaData preferredMetadata = new();

                //iterate over the sources in reverse because overwriting causes the last ones to remain
                foreach (var sourceType in prop.SourcePriority.AsEnumerable().Reverse())
                {
                    ProgressViewModel.GlobalCancellationTokenSource.Token.ThrowIfCancellationRequested();

                    // Check if there is metadata from this source to use
                    if (!metaDataFromSource.TryGetValue(sourceType, out SourceMetaData? metadata))
                    {
                        MetaDataSource? source = MetaDataSource.GetSource(sourceType, codex.Collection);
                        if (source is null) continue;
                        if (!source.IsValidSource(codex.Sources)) continue;
                        metadata = await source.GetMetaData(codex.Sources);
                        metaDataFromSource.Add(sourceType, metadata);
                    }

                    //If there is, make it the new preferred
                    if (!prop.IsEmpty(metadata))
                    {
                        prop.Copy(metadata, preferredMetadata);
                    }
                }

                //if no (new) value was found for this prop, do nothing
                if (prop.IsEmpty(preferredMetadata) || !prop.HasNewValue(preferredMetadata, codex)) continue;
                
                if ((prop.OverwriteMode == MetaDataOverwriteMode.IfEmpty && prop.IsEmpty(existingMetaData)) ||
                    prop.OverwriteMode == MetaDataOverwriteMode.Always)
                {
                    prop.Apply(preferredMetadata, codex);
                }
                else if ((prop.OverwriteMode == MetaDataOverwriteMode.IfEmpty && !prop.IsEmpty(existingMetaData)) ||
                         prop.OverwriteMode == MetaDataOverwriteMode.Ask )
                {
                    prop.Copy(preferredMetadata, toAsk);
                    shouldAsk = true; //set shouldAsk to true when we found at lease one none empty prop that should be asked
                }
            }

            if (shouldAsk)
            {
                chooseMetaDataVM.AddMetaDataProposal(codex, toAsk);
            }

            ProgressViewModel.GetInstance().IncrementCounter();
        }

        private AsyncRelayCommand<IList>? _getMetaDataBulkCommand;
        public AsyncRelayCommand<IList> GetMetaDataBulkCommand => _getMetaDataBulkCommand ??= new(GetMetaDataBulk);

        private async Task GetMetaDataBulk(IList? codices)
        {
            try
            {
                await StartGetMetaDataProcess(codices?.Cast<Codex>().ToList() ?? []);
            }
            catch (OperationCanceledException ex)
            {
                Logger.Warn("Renewing metadata has been cancelled", ex);
                await Task.Run(() => ProgressViewModel.GetInstance().ConfirmCancellation());
            }
        }

        private AsyncRelayCommand<Codex>? _getCoverCommand;
        public AsyncRelayCommand<Codex> GetCoverCommand => _getCoverCommand ??= new(GetCover);
        private static async Task GetCover(Codex? codex)
        {
            if (codex is null) return;
            await CoverService.GetAndApplyCover([codex]);
        }

        private AsyncRelayCommand<IList>? _getCoverBulkCommand;
        public AsyncRelayCommand<IList> GetCoverBulkCommand => _getCoverBulkCommand ??= new(GetCoverBulk);
        private static async Task GetCoverBulk(IList? codices) =>
            await CoverService.GetAndApplyCover(codices?.Cast<Codex>().ToList() ?? []);

        //TODO remove this, HandleKeyDownOnCodex should be called directly
        // public static void DataGridHandleKeyDown(object? sender, KeyEventArgs e)
        //     => HandleKeyDownOnCodex((sender as DataGrid)?.SelectedItems, e);
        public async void HandleKeyDownOnCodex(IList? selectedItems, KeyEventArgs e)
        {
            if (selectedItems is null) return;

            List<Codex> codices = selectedItems.Cast<Codex>().ToList();
            int count = selectedItems.Count;

            if (count > 0)
            {
                switch (e.Key)
                {
                    case Key.Delete:
                        if (e.KeyModifiers == KeyModifiers.Alt)
                        {
                            //Alt + Delete
                            await BanishCodices(codices);
                        }
                        else
                        {
                            //Delete
                            await DeleteCodices(codices, true);
                        }
                        e.Handled = true;
                        break;
                    case Key.Enter:
                        await OpenSelectedCodices(codices);
                        e.Handled = true;
                        break;
                    case Key.E:
                        if (e.KeyModifiers == KeyModifiers.Control)
                        {
                            //CTRL + E
                            await EditCodices(codices);
                            e.Handled = true;
                        }
                        break;
                    case Key.F:
                        if (e.KeyModifiers == KeyModifiers.Control)
                        {
                            //CTRL + F
                            FavoriteCodices(codices);
                            e.Handled = true;
                        }
                        break;
                }
            }
        }

        #region Drag & Drop

        //Handle drag&drop of Tags on Codices to add them

        public void OnDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetValue<TreeNode<Tag>>() is { Item.IsGroup: false } ||
                e.Data.GetValue<Tag>() is { IsGroup: false })
            {
                //TODO Handle adorner manually
                //dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
                e.DragEffects = DragDropEffects.Copy;
            }
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            //Codex targetCodex = (Codex)dropInfo.TargetItem;
            //TODO check if sender is actually the codex or a control or viewmodel of some sort
            if (sender is not Codex targetCodex) return;

            Tag? toAdd = null;

            if (e.Data.Contains(nameof(TreeNode<Tag>)))
            {
                TreeNode<Tag>? node = e.Data.Get(nameof(TreeNode<Tag>)) as TreeNode<Tag>;
                toAdd = node?.Item;
            }
            else if (e.Data.Contains(nameof(Tag)))
            {
                toAdd = e.Data.Get(nameof(Tag)) as Tag;
            }

            if (toAdd is null) return;

            if (!targetCodex.Tags.Contains(toAdd))
            {
                targetCodex.Tags.Add(toAdd);
            }
        }
        #endregion
    }
}
