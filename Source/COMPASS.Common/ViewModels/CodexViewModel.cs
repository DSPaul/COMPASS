using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Avalonia.Controls;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Interfaces;
using COMPASS.Common.Models;
using COMPASS.Common.Models.CodexProperties;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Models.XmlDtos;
using COMPASS.Common.Services;
using COMPASS.Common.Services.FileSystem;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels.Sources;
using COMPASS.Common.Views.Windows;

namespace COMPASS.Common.ViewModels
{
    public class CodexViewModel : ViewModelBase
    {
        #region Open Codex

        //Open Codex wherever
        public static bool OpenCodex(Codex codex)
        {
            bool success = PreferableFunction<Codex>.TryFunctions(PreferencesService.GetInstance().Preferences.OpenCodexPriority, codex);
            if (!success)
            {
                Notification notification = new("Could Not open item", "Could not open item, please check local path or URL");
                App.Container.ResolveKeyed<INotificationService>(NotificationDisplayType.Windowed).Show(notification);
            }

            return success;
        }

        //Open codex Offline
        private RelayCommand<Codex>? _openCodexLocallyCommand;
        public RelayCommand<Codex> OpenCodexLocallyCommand => _openCodexLocallyCommand ??= new(codex => OpenCodexLocally(codex), CanOpenCodexLocally);
        public static bool OpenCodexLocally(Codex? toOpen)
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

                FileNotFoundWindow fileNotFoundWindow = new(new(toOpen));
                return fileNotFoundWindow.ShowDialog(App.MainWindow).IsCompletedSuccessfully; //TODO, this should be async
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
        private RelayCommand<IList>? _openSelectedCodicesCommand;
        public RelayCommand<IList> OpenSelectedCodicesCommand => _openSelectedCodicesCommand ??= new(l => OpenSelectedCodices(l?.Cast<Codex>().ToList()));
        public static bool OpenSelectedCodices(IList<Codex>? toOpen)
        {
            if (toOpen is null) return false;

            if (toOpen.Count == 1)
            {
                return OpenCodex(toOpen.First());
            }

            Notification notification = Notification.AreYouSureNotification;
            notification.Body = "You are about to open " + toOpen.Count + " items. Are you sure you wish to continue?";
            App.Container.ResolveKeyed<INotificationService>(NotificationDisplayType.Windowed).Show(notification);

            if (notification.Result == NotificationAction.Confirm)
            {
                foreach (Codex f in toOpen)
                {
                    OpenCodex(f);
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
        public static async Task EditCodex(Codex? toEdit)
        {
            if (toEdit is null) return;
            CodexEditWindow editWindow = new(new CodexEditViewModel(toEdit))
            {
                Topmost = true
            };
            await editWindow.ShowDialog(App.MainWindow);
        }

        //Edit Multiple files
        private AsyncRelayCommand<IList>? _editCodicesCommand;
        public AsyncRelayCommand<IList> EditCodicesCommand => _editCodicesCommand ??= new(EditCodices);
        public static async Task EditCodices(IList? toEdit)
        {
            List<Codex>? toEditList = toEdit?.Cast<Codex>().ToList();
            if (toEditList is null) return;

            if (toEditList.Count == 1)
            {
                await EditCodex(toEditList.First());
                return;
            }

            CodexBulkEditWindow window = new(new CodexBulkEditViewModel(toEditList))
            {
                Topmost = true
            };
            await window.ShowDialog(App.MainWindow);
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
            if (toFavoriteList is null) return;
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
            if (String.IsNullOrEmpty(toShow?.Sources.Path) || !File.Exists(toShow.Sources.Path)) return;
            string? folderPath = Path.GetDirectoryName(toShow.Sources.Path);
            if (String.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath)) return;
            IOService.ShowInExplorer(folderPath);
        }

        //Move Codex to other CodexCollection
        private RelayCommand<IList<object>>? _moveToCollectionCommand;
        public RelayCommand<IList<object>> MoveToCollectionCommand => _moveToCollectionCommand ??= new(MoveToCollection);
        public void MoveToCollection(IList<object>? par)
        {
            if (par == null) return;

            //par contains 2 parameters
            CodexCollection targetCollection = new((string)par[0]);
            List<Codex> toMoveList = par[1] switch
            {
                Codex codex => [codex],
                IList list => list.Cast<Codex>().ToList(),
                _ => []
            };

            MoveToCollection(targetCollection, toMoveList);
        }

        /// <summary>
        /// Moves all codices from the toMoveList to the targetCollection
        /// </summary>
        /// <param name="targetCollection"></param>
        /// <param name="toMoveList"></param>
        public static void MoveToCollection(CodexCollection targetCollection, List<Codex> toMoveList)
        {
            //Check if target Collection is valid
            if (targetCollection.DirectoryName == MainViewModel.CollectionVM.CurrentCollection.DirectoryName)
            {
                Logger.Warn($"Target Collection {targetCollection.DirectoryName} is invalid");
                return;
            }

            //"Are you Sure?"

            var windowedNotificationService = App.Container.ResolveKeyed<INotificationService>(NotificationDisplayType.Windowed);

            string messageSingle = $"Moving  {toMoveList[0].Title} to {targetCollection.DirectoryName} will remove all tags from the item, are you sure you wish to continue?";
            string messageMultiple = $"Moving these {toMoveList.Count} items to {targetCollection.DirectoryName} will remove all tags from these items, are you sure you wish to continue?";

            Notification areYouSureNotification = Notification.AreYouSureNotification;
            areYouSureNotification.Body = toMoveList.Count == 1 ? messageSingle : messageMultiple;
            windowedNotificationService.Show(areYouSureNotification);

            if (areYouSureNotification.Result == NotificationAction.Confirm)
            {
                bool success = targetCollection.LoadCodices();
                if (!success)
                {
                    Notification errorNotification = new("Target collection could not be loaded.", $"Could not move items to {targetCollection.DirectoryName}", Severity.Error);
                    windowedNotificationService.Show(errorNotification);
                    return;
                }

                //Make sure the directories exist before copying cover art into it
                targetCollection.CreateDirectories();
                foreach (Codex toMove in toMoveList)
                {
                    toMove.Tags.Clear();
                    toMove.ID = Utils.GetAvailableID(targetCollection.AllCodices);

                    //Add Codex to target CodexCollection
                    targetCollection.AllCodices.Add(toMove);

                    //Move cover art to right folder with new ID
                    Codex tempCodex = new(toMove);
                    tempCodex.SetImagePaths(targetCollection);

                    if (Path.Exists(toMove.CoverArtPath))
                        File.Copy(toMove.CoverArtPath, tempCodex.CoverArtPath, true);
                    if (Path.Exists(toMove.CoverArtPath))
                        File.Copy(toMove.ThumbnailPath, tempCodex.ThumbnailPath, true);

                    //Delete codex in original collection
                    MainViewModel.CollectionVM.CurrentCollection.DeleteCodex(toMove);
                    MainViewModel.CollectionVM.FilterVM.RemoveCodex(toMove);

                    //Update the cover art metadata to new path, has to happen after delete so old one gets deleted
                    toMove.Copy(tempCodex);

                    Logger.Info($"Moved {toMove.Title} from {MainViewModel.CollectionVM.CurrentCollection.DirectoryName} to {targetCollection.DirectoryName}");
                }

                //Save changes to both collections
                MainViewModel.CollectionVM.CurrentCollection.SaveCodices();
                targetCollection.SaveCodices();
            }
        }

        //Delete Codex
        private RelayCommand<Codex>? _deleteCodexCommand;
        public RelayCommand<Codex> DeleteCodexCommand => _deleteCodexCommand ??= new(DeleteCodex);
        public static void DeleteCodex(Codex? toDelete)
        {
            if (toDelete == null) return;
            DeleteCodices(new List<Codex>() { toDelete });
        }

        //Delete Codices
        private RelayCommand<IList>? _deleteCodicesCommand;
        public RelayCommand<IList> DeleteCodicesCommand => _deleteCodicesCommand ??= new(DeleteCodices);
        public static void DeleteCodices(IList? toDelete)
        {
            MainViewModel.CollectionVM.CurrentCollection.DeleteCodices(toDelete?.Cast<Codex>().ToList() ?? []);
            MainViewModel.CollectionVM.FilterVM.ReFilter();
        }

        //Banish Codex
        private RelayCommand<Codex>? _banishCodexCommand;
        public RelayCommand<Codex> BanishCodexCommand => _banishCodexCommand ??= new((codex) =>
        {
            if (codex == null) return;
            BanishCodices(new List<Codex>() { codex });
        });

        //Banish Codices
        private RelayCommand<IList>? _banishCodicesCommand;
        public RelayCommand<IList> BanishCodicesCommand => _banishCodicesCommand ??= new(BanishCodices);
        public static void BanishCodices(IList? toBanish)
        {
            MainViewModel.CollectionVM.CurrentCollection.BanishCodices(toBanish?.Cast<Codex>().ToList() ?? []);
            DeleteCodices(toBanish);
        }

        private AsyncRelayCommand<Codex>? _getMetaDataCommand;
        public AsyncRelayCommand<Codex> GetMetaDataCommand => _getMetaDataCommand ??= new(StartGetMetaDataProcess);
        public static async Task StartGetMetaDataProcess(Codex? codex)
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
        public static async Task StartGetMetaDataProcess(IList<Codex> codices)
        {
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

            if (chooseMetaDataVM.CodicesWithChoices.Any())
            {
                ChooseMetaDataWindow window = new(chooseMetaDataVM);
                window.Show();
            }

            MainViewModel.CollectionVM.FilterVM.PopulateMetaDataCollections();
            MainViewModel.CollectionVM.CurrentCollection.Save();
            MainViewModel.CollectionVM.FilterVM.ReFilter();
        }
        private static async Task GetMetaData(Codex codex, ChooseMetaDataViewModel chooseMetaDataVM)
        {
            // Lazy load metadata from all the sources, use dict to store
            Dictionary<MetaDataSource, CodexDto> metaDataFromSource = new();

            //First try to get sources from other sources
            //Pdf can contain ISBN number
            PdfSourceViewModel pdfSourceVM = new();
            if (pdfSourceVM.IsValidSource(codex.Sources) && String.IsNullOrEmpty(codex.Sources.ISBN))
            {
                CodexDto pdfData = await pdfSourceVM.GetMetaData(codex.Sources);

                //already store this so pdf doesn't need to be opened twice
                metaDataFromSource.Add(MetaDataSource.PDF, pdfData);
            }


            // Now use bits and pieces of the Codices in MetaDataFromSource to set the actual metadata based on preferences
            //Codex with metadata that will be shown to the user, and asked if they want to use it
            CodexDto toAsk = new();
            bool shouldAsk = false;

            //Iterate over all the properties and set them
            foreach (var prop in PreferencesService.GetInstance().Preferences.CodexProperties)
            {

                if (prop.OverwriteMode == MetaDataOverwriteMode.Never) continue;
                if (prop.OverwriteMode == MetaDataOverwriteMode.IfEmpty && !prop.IsEmpty(codex)) continue;
                if (prop is CoverArtProperty) continue; //Covers are done separately

                //preferredMetadata will hold the metadata from the top preferred source
                CodexDto preferredMetadata = new();

                //iterate over the sources in reverse because overwriting causes the last ones to remain
                foreach (var source in prop.SourcePriority.AsEnumerable().Reverse())
                {
                    ProgressViewModel.GlobalCancellationTokenSource.Token.ThrowIfCancellationRequested();

                    // Check if there is metadata from this source to use
                    if (!metaDataFromSource.TryGetValue(source, out CodexDto? metadata))
                    {
                        SourceViewModel? sourceVM = SourceViewModel.GetSourceVM(source);
                        if (sourceVM is null) continue;
                        if (!sourceVM.IsValidSource(codex.Sources)) continue;
                        metadata = await sourceVM.GetMetaData(codex.Sources);
                        metaDataFromSource.Add(source, metadata);
                    }
                    // Set the prop Data from this source in propHolder
                    // if the new value is not null/default/empty
                    if (!prop.IsEmpty(metadata))
                    {
                        prop.SetProp(preferredMetadata, metadata);
                    }
                }

                //if no value was found for this prop, do nothing
                if (prop.IsEmpty(preferredMetadata)) continue;

                if (prop.OverwriteMode == MetaDataOverwriteMode.Always || prop.IsEmpty(codex))
                {
                    prop.SetProp(codex, preferredMetadata.ToModel(MainViewModel.CollectionVM.CurrentCollection.AllTags));
                }
                else if (prop.OverwriteMode == MetaDataOverwriteMode.Ask && prop.HasNewValue(preferredMetadata, codex))
                {
                    prop.SetProp(toAsk, preferredMetadata);
                    shouldAsk = true; //set shouldAsk to true when we found at lease one none empty prop that should be asked
                }
            }

            if (shouldAsk)
            {
                var allTags = MainViewModel.CollectionVM.CurrentCollection.AllTags;
                chooseMetaDataVM.AddCodexPair(codex, toAsk.ToModel(allTags));
            }

            ProgressViewModel.GetInstance().IncrementCounter();
        }

        private AsyncRelayCommand<IList>? _getMetaDataBulkCommand;
        public AsyncRelayCommand<IList> GetMetaDataBulkCommand => _getMetaDataBulkCommand ??= new(GetMetaDataBulk);

        private static async Task GetMetaDataBulk(IList? codices)
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
            await CoverService.GetCover([codex]);
        }

        private AsyncRelayCommand<IList>? _getCoverBulkCommand;
        public AsyncRelayCommand<IList> GetCoverBulkCommand => _getCoverBulkCommand ??= new(GetCoverBulk);
        private static async Task GetCoverBulk(IList? codices) =>
            await CoverService.GetCover(codices?.Cast<Codex>().ToList() ?? []);

        public static void DataGridHandleKeyDown(object? sender, KeyEventArgs e)
            => HandleKeyDownOnCodex((sender as DataGrid)?.SelectedItems, e);
        public static void ListBoxHandleKeyDown(object? sender, KeyEventArgs e)
            => HandleKeyDownOnCodex((sender as ListBox)?.SelectedItems, e);
        public static async void HandleKeyDownOnCodex(IList? selectedItems, KeyEventArgs e)
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
                            BanishCodices(codices);
                        }
                        else
                        {
                            //Delete
                            DeleteCodices(codices);
                        }
                        e.Handled = true;
                        break;
                    case Key.Enter:
                        OpenSelectedCodices(codices);
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
            if (e.Data.GetValue<TreeViewNode>() is { Tag.IsGroup: false } ||
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

            if (e.Data.Contains(nameof(TreeViewNode)))
            {
                TreeViewNode? tvn = e.Data.Get(nameof(TreeViewNode)) as TreeViewNode;
                toAdd = tvn?.Tag;
            }
            else if (e.Data.Contains(nameof(Tag)))
            {
                toAdd = e.Data.Get(nameof(Tag)) as Tag;
            }

            if (toAdd is null) return;

            if (!targetCodex.Tags.Contains(toAdd))
            {
                targetCodex.Tags.Add(toAdd);
                MainViewModel.CollectionVM.FilterVM.ReFilter();
            }
        }
        #endregion
    }
}
