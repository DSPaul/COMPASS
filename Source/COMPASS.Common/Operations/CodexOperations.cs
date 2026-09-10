using Avalonia.Input;
using Avalonia.Threading;
using Autofac.Features.Indexed;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models;
using COMPASS.Common.Models.CodexProperties;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Models.Preferences;
using COMPASS.Common.Services;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.Sources;
using COMPASS.Common.ViewModels;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Common.ViewModels.Modals;
using COMPASS.Common.ViewModels.Modals.Edit;
using COMPASS.Common.ViewModels.ModelVMs;
using COMPASS.Infra.ExtensionMethods;
using COMPASS.Infra.Interfaces.Services;
using COMPASS.Infra.Models;
using COMPASS.Infra.Models.Enums;
using COMPASS.Infra.Tools;
using System.Collections;
using System.Diagnostics;

namespace COMPASS.Common.Operations
{
    /// Domain operations for <see cref="Codex"/>
    public class CodexOperations(
        ILogger logger,
        IPreferencesService preferencesService,
        INotificationService notificationService,
        ICoverStorageService coverStorageService,
        IUserFilesStorageService userFilesStorageService,
        IIOService ioService,
        Lazy<CodexEditViewModelFactory> codexEditViewModelFactory,
        Lazy<FileNotFoundViewModelFactory> fileNotFoundViewModelFactory,
        ChooseMetaDataViewModelFactory chooseMetaDataViewModelFactory,
        Lazy<CollectionManager> collectionManager,
        CoverService coverService,
        IIndex<MetaDataSourceType, MetaDataSource> metaDataSources)
    {
        #region Open Codex

        //Open Codex wherever
        public async Task<bool> OpenCodex(Codex codex)
        {
            //Preferences only stores the order (IDs); bind to instance methods at execution time
            var openFunctionsById = new Dictionary<int, PreferableFunction<Codex>>
            {
                [Preferences.ONLINE_SOURCE_PRIORITY_ID] = new("Online source", OpenCodexOnline, Preferences.ONLINE_SOURCE_PRIORITY_ID),
                [Preferences.LOCAL_SOURCE_PRIORITY_ID] = new("Local File", OpenCodexLocally, Preferences.LOCAL_SOURCE_PRIORITY_ID),
            };
            var orderedFunctions = preferencesService.Preferences.OpenCodexPriority
                .Select(pf => pf.Id)
                .Where(openFunctionsById.ContainsKey)
                .Select(id => openFunctionsById[id]);

            bool success = await PreferableFunction<Codex>.TryFunctionsAsync(orderedFunctions, codex);
            if (!success)
            {
                Notification notification = new("Could not open item", "Could not open item, please check its sources");
                await notificationService.ShowDialog(notification);
            }

            return success;
        }
        
        public bool CanOpenCodex(Codex codex) => CanOpenCodexLocally(codex) || CanOpenCodexOnline(codex);

        //Open codex Offline
        public async Task<bool> OpenCodexLocally(Codex? toOpen)
        {
            if (toOpen is null) return false;
            if (!toOpen.Sources.HasOfflineSource()) return false;
            try
            {
                Process.Start(new ProcessStartInfo(toOpen.Sources.Path) { UseShellExecute = true });
                toOpen.LastOpened = DateTime.Now;
                toOpen.OpenedCount++;
                logger.Info($"Opened {toOpen.Sources.Path}");
                return true;
            }
            catch (Exception ex)
            {
                logger.Warn($"Failed to open {toOpen.Sources.Path}", ex);

                var fileNotFoundVM = fileNotFoundViewModelFactory.Value.Create(toOpen);
                await WindowManager.OpenModal(fileNotFoundVM);
                return fileNotFoundVM.FixedAndOpenedCodex;
            }
        }
        public bool CanOpenCodexLocally(Codex? toOpen)
        {
            if (toOpen == null) return false;

            return toOpen.Sources.HasOfflineSource();
        }

        //Open codex Online
        public bool OpenCodexOnline(Codex? toOpen)
        {
            if (!CanOpenCodexOnline(toOpen)) return false;
            try
            {
                //TODO detect if source is even reachable before opening the brower
                Process.Start(new ProcessStartInfo(toOpen!.Sources.SourceURL) { UseShellExecute = true });
                toOpen.LastOpened = DateTime.Now;
                toOpen.OpenedCount++;
                logger.Info($"Opened {toOpen.Sources.SourceURL}");
                return true;
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to open {toOpen!.Sources.SourceURL}", ex);
                return false;
            }
        }
        public bool CanOpenCodexOnline(Codex? toOpen)
        {
            if (toOpen is null) return false;

            return toOpen.Sources.HasOnlineSource();
        }

        //Open Multiple Files
        public AsyncRelayCommand<IList> OpenSelectedCodicesCommand => 
            field ??= new(async l => await OpenSelectedCodices(l?.Cast<CodexViewModel>().Select(vm => vm.GetModel()).ToList()));
        public async Task<bool> OpenSelectedCodices(IList<Codex>? toOpen)
        {
            if (!toOpen.SafeAny()) return false;

            if (toOpen.Count == 1)
            {
                return await OpenCodex(toOpen.First());
            }

            Notification notification = Notification.AreYouSureNotification;
            notification.Body = "You are about to open " + toOpen.Count + " items. Are you sure you wish to continue?";
            await notificationService.ShowDialog(notification);

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

        public async Task EditCodex(Codex? toEdit)
        {
            if (toEdit is null) return;
            var codexEditVm = codexEditViewModelFactory.Value.Create(sourceCodex: toEdit);
            await WindowManager.OpenModal(codexEditVm);
        }

        //Edit Multiple files
        public AsyncRelayCommand<IList> EditCodicesCommand => field ??= new(EditCodices);
        public async Task EditCodices(IList? toEdit)
        {
            List<Codex>? toEditList = toEdit?
                .OfType<CodexViewModel>().Select(vm => vm.GetModel()) //could be codexVms
                .Concat(toEdit.OfType<Codex>())                       //could just be items
                .ToList();

            if (!toEditList.SafeAny()) return;

            if (toEditList.Count == 1)
            {
                await EditCodex(toEditList.First());
                return;
            }

            CodexBulkEditViewModel vm = new(toEdit: toEditList);
            await WindowManager.OpenModal(vm);
        }

        #endregion

        #region Toggle Favorite 

        //Toggle Favorite
        public void FavoriteCodex(Codex? toFavorite)
        {
            if (toFavorite is null) return;
            toFavorite.Favorite = !toFavorite.Favorite;
            string prefix = toFavorite.Favorite ? "Favorited" : "Unfavorited";
            logger.Info($"{prefix} {toFavorite.Title}");
        }

        //Toggle Favorite
        public RelayCommand<IList> FavoriteCodicesCommand => field ??= new(FavoriteCodices);
        private void FavoriteCodices(IList? toFavorite)
        {
            List<Codex>? toFavoriteList = GatherCodices(toFavorite);
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
                logger.Info($"{(newVal ? "Favorited" : "Unfavorited")} {codex.Title}");
            }
        }

        #endregion

        //Show in Explorer
        public void ShowInExplorer(Codex? toShow)
        {
            string? filePath = toShow?.Sources.Path;
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;
            
            ioService.ShowInExplorer(filePath);
        }

        //Move Codex to other CodexCollection
        public AsyncRelayCommand<IList<object>> MoveToCollectionCommand => field ??= new(MoveToCollection);
        public async Task MoveToCollection(IList<object>? par)
        {
            if (par == null) return;

            if (par.Count != 2)
            {
                logger.Debug($"Move to collection contained {par.Count} parameters, should be 2");
                return;
            }

            //par contains 2 parameters
            string targetCollectionIdentifier = (string)par[0];
            List<Codex>? toMoveList = GatherCodices(par[1] as IList);

            if(!toMoveList.SafeAny()) return;

            await MoveToCollection(targetCollectionIdentifier, toMoveList);
        }

        /// <summary>
        /// Moves all items from the toMoveList to the targetCollection
        /// </summary>
        /// <param name="targetCollectionIdentifier"></param>
        /// <param name="toMoveList"></param>
        public async Task MoveToCollection(string targetCollectionIdentifier, List<Codex> toMoveList)
        {
            if (!toMoveList.Any())
            {
                return;
            }

            //To move should all belong to the same collection
            CodexCollection sourceCollection = toMoveList[0].Collection;
            Debug.Assert(toMoveList.All(codex => codex.Collection == sourceCollection));

            using var sourceCollectionHandle = sourceCollection.Load();
            if (sourceCollectionHandle == null)
            {
                logger.Warn($"Failed to move items from {sourceCollection.Name} because they could not be loaded");
                return;
            }

            //Check if target Collection is valid
            if (targetCollectionIdentifier == sourceCollection.Name)
            {
                logger.Warn($"Target collection {targetCollectionIdentifier} cannot be the same as source collection");
                return;
            }

            //"Are you Sure?"
            string messageSingle = $"Moving  {toMoveList[0].Title} to {targetCollectionIdentifier} will remove all tags from the item, are you sure you wish to continue?";
            string messageMultiple = $"Moving these {toMoveList.Count} items to {targetCollectionIdentifier} will remove all tags from these items, are you sure you wish to continue?";

            Notification areYouSureNotification = Notification.AreYouSureNotification;
            areYouSureNotification.Body = toMoveList.Count == 1 ? messageSingle : messageMultiple;
            await notificationService.ShowDialog(areYouSureNotification);

            if (areYouSureNotification.Result == NotificationAction.Confirm)
            {
                using CollectionHandle? targetCollectionHandle = collectionManager.Value.LoadCollection(targetCollectionIdentifier);
                if (targetCollectionHandle == null)
                {
                    Notification errorNotification = new("Target collection could not be loaded.", $"Could not move items to {targetCollectionIdentifier}",
                        Severity.Error);
                    await notificationService.ShowDialog(errorNotification);
                    return;
                }
                
                CodexCollection targetCollection = targetCollectionHandle.CollectionVM.Collection;
                
                //Copy the items to the target collection
                foreach (Codex toMove in toMoveList)
                {
                    Codex movedCodex = new(targetCollection);
                    movedCodex.CopyFrom(toMove);

                    movedCodex.Tags.Clear();
                    movedCodex.Id = Utils.GetAvailableId(targetCollection.AllCodices);

                    //Add Codex to target CodexCollection
                    targetCollection.AllCodices.Add(movedCodex);

                    coverStorageService.MoveCodexDataToCollection(movedCodex, targetCollection);
                    userFilesStorageService.MoveCodexDataToCollection(movedCodex, targetCollection, toMove.Collection);

                    logger.Info($"Moved {movedCodex.Title} from {sourceCollection.Name} to {targetCollection.Name}");
                }

                //After they are all copied, delete them
                await DeleteCodices(toMoveList, false);

                //Save source and target collections
                sourceCollectionHandle.SaveCodices();
                targetCollectionHandle.SaveCodices();
            }
        }

        //Delete Codex
        public async Task DeleteCodex(Codex? toDelete)
        {
            if (toDelete == null) return;
            await DeleteCodices([toDelete], true);
        }

        //Delete Codices
        public AsyncRelayCommand<IList> DeleteCodicesCommand => field ??= new(async (items) =>
        {
            List<Codex>? codicesToDelete = GatherCodices(items);

            if (!codicesToDelete.SafeAny()) return;

            await DeleteCodices(codicesToDelete, true);
        });
        
        public async Task DeleteCodices(IList<Codex> codicesToDelete, bool askForConfirmation)
        {
            if (!codicesToDelete.Any()) return;
            
            Notification deleteWarnNotification = Notification.AreYouSureNotification;
            if (askForConfirmation)
            {
                deleteWarnNotification.Body = $"You are about to remove {codicesToDelete.Count} item{(codicesToDelete.Count > 1 ? @"s" : @"")}. " +
                                              $"This cannot be undone. " +
                                              $"Are you sure you want to continue?";
                await notificationService.ShowDialog(deleteWarnNotification);
            }

            if (askForConfirmation && deleteWarnNotification.Result != NotificationAction.Confirm)
            {
                return;
            }

            var codicesByCollections = codicesToDelete.GroupBy(codex => codex.Collection);
            foreach (var group in codicesByCollections)
            {
                CodexCollection collection = group.Key;
                using var collectionHandle = collectionManager.Value.LoadCollection(collection.Name);

                if (collectionHandle == null)
                {
                    logger.Warn($"Failed to delete items from {collection.Name} because the collection could not be loaded");
                    var failedNotification = new Notification("Failed to delete items", $"Failed to delete items from {collection.Name} because the collection could not be loaded", Severity.Error);
                    await notificationService.ShowDialog(failedNotification);
                    return;
                }
                
                //Delete codex from all lists
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    using var scope = TabsViewModel.GetInstance().ActiveTab?.FiltersVM?.DelayUpdateEvents();
                    foreach (Codex codexToDelete in group)
                    {
                        collection.AllCodices.Remove(codexToDelete);
                        coverStorageService.OnCodexDeleted(codexToDelete);
                        logger.Info($"Removed {codexToDelete.Title} from {collection.Name}");
                    }

                    collectionHandle.SaveCodices();
                });
            }
        }

        //Banish Codex
        public async Task BanishCodex(Codex? codex)
        {
            if (codex == null) return;
            await BanishCodices(new List<Codex>() { codex });
        }

        //Banish Codices
        public AsyncRelayCommand<IList> BanishCodicesCommand => field ??= new(BanishCodices);
        private async Task BanishCodices(IList? toBanish)
        {
            var codicesToBanish = GatherCodices(toBanish);
            if (!codicesToBanish.SafeAny()) return;

            var codicesByCollections = codicesToBanish.GroupBy(codex => codex.Collection);
            foreach (var codicesForCollection in codicesByCollections)
            {
                CodexCollection collection = codicesForCollection.Key;
                collection.BanishCodices(codicesForCollection.ToList());
            }
            
            await DeleteCodices(codicesToBanish, true);
        }

        //Get Metadata
        public async Task StartGetMetaDataProcess(Codex? codex)
        {
            try
            {
                if (codex == null) { return; }
                await StartGetMetaDataProcess(new List<Codex>() { codex });
            }
            catch (OperationCanceledException ex)
            {
                logger.Warn("Renewing metadata has been cancelled", ex);
                await Task.Run(() => ProgressViewModel.GetInstance().ConfirmCancellation());
            }
        }
        public async Task StartGetMetaDataProcess(IList<Codex> codices)
        {            
            if (!codices.Any()) return;

            var codicesGroupedByCollection = codices.GroupBy(codex => codex.Collection).ToList();
            
            //If spread over multiple collection, to them one collection at a time
            if (codicesGroupedByCollection.Count > 1)
            {
                foreach (var group in codicesGroupedByCollection)
                {
                    await StartGetMetaDataProcess(group.ToList());
                }
                return;
            }
            
            using CollectionHandle? localCollectionHandle = codices.First().Collection.Load();
            if (localCollectionHandle == null)
            {
                logger.Warn("Could not get metadata for items as they could not be loaded");
                return;;
            }
            
            var progressVM = ProgressViewModel.GetInstance();
            progressVM.ResetCounter();
            progressVM.Text = "Getting MetaData";
            progressVM.TotalAmount = codices.Count;

            ParallelOptions parallelOptions = new()
            {
                MaxDegreeOfParallelism = Math.Max(Environment.ProcessorCount / 2, 1)
            };

            ChooseMetaDataViewModel chooseMetaDataVM = chooseMetaDataViewModelFactory.Create();

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
                await WindowManager.OpenModal(chooseMetaDataVM);
            }
            
            //Save at the end
            localCollectionHandle.SaveCodices();
        }
        private async Task GetMetaData(Codex codex, ChooseMetaDataViewModel chooseMetaDataVM)
        {
            SourceMetaData existingMetaData = new(codex);

            // Lazy load metadata from all the sources, use dict to store
            Dictionary<MetaDataSourceType, SourceMetaData> metaDataFromSource = new();

            //First try to get sources from other sources
            //Pdf can contain ISBN number
            if (metaDataSources.TryGetValue(MetaDataSourceType.PDF, out MetaDataSource? pdfSource)
                && pdfSource.IsValidSource(codex.Sources)
                && string.IsNullOrEmpty(codex.Sources.ISBN))
            {
                SourceMetaData pdfData = await pdfSource.GetMetaData(codex.Sources, codex.Collection.AllTags);

                //already store this so pdf doesn't need to be opened twice
                metaDataFromSource.Add(MetaDataSourceType.PDF, pdfData);
            }

            //metadata that will be shown to the user, and asked if they want to use it
            SourceMetaData toAsk = new();
            bool shouldAsk = false;

                //Iterate over all the properties and set them
                foreach (var prop in preferencesService.Preferences.ImportableCodexProperties)
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
                        if (!metaDataSources.TryGetValue(sourceType, out MetaDataSource? source)) continue;
                        if (!source.IsValidSource(codex.Sources)) continue;
                        metadata = await source.GetMetaData(codex.Sources, codex.Collection.AllTags);
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

        public AsyncRelayCommand<IList> GetMetaDataBulkCommand => field ??= new(GetMetaDataBulk);

        private async Task GetMetaDataBulk(IList? items)
        {
            try
            {
                var codices = GatherCodices(items);
                if(!codices.SafeAny()) return;
                await StartGetMetaDataProcess(codices);
            }
            catch (OperationCanceledException ex)
            {
                logger.Warn("Renewing metadata has been cancelled", ex);
                await Task.Run(() => ProgressViewModel.GetInstance().ConfirmCancellation());
            }
        }
        
        //Get Cover
        public async Task GetCover(Codex? codex)
        {
            if (codex is null) return;
            await coverService.GetAndApplyCover([codex]);
        }

        public AsyncRelayCommand<IList> GetCoverBulkCommand => field ??= new(GetCoverBulk);
        private async Task GetCoverBulk(IList? items) =>
            await coverService.GetAndApplyCover(GatherCodices(items) ?? []);
        
        public async void HandleKeyDownOnCodex(IList? selectedItems, KeyEventArgs e)
        {
            List<Codex>? codices = GatherCodices(selectedItems);

            if (!codices.SafeAny()) return;

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

        private static List<Codex>? GatherCodices(IList? items) => items?
                .OfType<CodexViewModel>().Select(vm => vm.GetModel()) //could be codexVms
                .Concat(items.OfType<Codex>())                       //could just be items
                .ToList();
    }
}
