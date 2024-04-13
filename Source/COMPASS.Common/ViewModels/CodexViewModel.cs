using Autofac;
using COMPASS.Common.Commands;
using COMPASS.Common.Models;
using COMPASS.Common.Services;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels.Sources;
using COMPASS.Common.Views.Windows;
using GongSolutions.Wpf.DragDrop;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace COMPASS.Common.ViewModels
{
    public class CodexViewModel : ViewModelBase, IDropTarget
    {
        #region Open Codex

        //Open Codex wherever
        public static bool OpenCodex(Codex codex)
        {
            bool success = PreferableFunction<Codex>.TryFunctions(SettingsViewModel.GetInstance().OpenCodexPriority, codex);
            if (!success)
            {
                messageDialog.Show("Could not open item, please check local path or URL", "Could not open item");
            }

            return success;
        }

        //Open codex Offline
        private ReturningRelayCommand<Codex, bool>? _openCodexLocallyCommand;
        public ReturningRelayCommand<Codex, bool> OpenCodexLocallyCommand => _openCodexLocallyCommand ??= new(OpenCodexLocally, CanOpenCodexLocally);
        public static bool OpenCodexLocally(Codex? toOpen)
        {
            if (toOpen is null) return false;
            if (!toOpen.HasOfflineSource()) return false;
            try
            {
                Process.Start(new ProcessStartInfo(toOpen.Path) { UseShellExecute = true });
                toOpen.LastOpened = DateTime.Now;
                toOpen.OpenedCount++;
                Logger.Info($"Opened {toOpen.Path}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Warn($"Failed to open {toOpen.Path}", ex);

                FileNotFoundWindow fileNotFoundWindow = new(new(toOpen))
                {
                    Owner = Application.Current.MainWindow
                };
                return fileNotFoundWindow.ShowDialog() ?? false;
            }
        }
        public static bool CanOpenCodexLocally(Codex? toOpen)
        {
            if (toOpen == null) return false;

            return toOpen.HasOfflineSource();
        }

        //Open codex Online
        private ReturningRelayCommand<Codex, bool>? _openCodexOnlineCommand;
        public ReturningRelayCommand<Codex, bool> OpenCodexOnlineCommand => _openCodexOnlineCommand ??= new(OpenCodexOnline, CanOpenCodexOnline);
        public static bool OpenCodexOnline(Codex? toOpen)
        {
            if (!CanOpenCodexOnline(toOpen)) return false;
            try
            {
                Process.Start(new ProcessStartInfo(toOpen!.SourceURL) { UseShellExecute = true });
                toOpen.LastOpened = DateTime.Now;
                toOpen.OpenedCount++;
                Logger.Info($"Opened {toOpen.SourceURL}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to open {toOpen!.SourceURL}", ex);
                //fails if no internet, pinging 8.8.8.8 DNS instead of server because some sites like gm binder block ping
                if (!IOService.PingURL()) Logger.Warn($"Cannot open this item online when not connected to the internet", ex);
                return false;
            }

        }
        public static bool CanOpenCodexOnline(Codex? toOpen)
        {
            if (toOpen is null) return false;

            return toOpen.HasOnlineSource();
        }

        //Open Multiple Files
        private ReturningRelayCommand<IList, bool>? _openSelectedCodicesCommand;
        public ReturningRelayCommand<IList, bool> OpenSelectedCodicesCommand => _openSelectedCodicesCommand ??= new(l => OpenSelectedCodices(l?.Cast<Codex>().ToList()));
        public static bool OpenSelectedCodices(IList<Codex>? toOpen)
        {
            if (toOpen is null) return false;

            if (toOpen.Count == 1)
            {
                return OpenCodex(toOpen.First());
            }

            //MessageBox "Are you Sure?"
            string messageBoxText = "You are about to open " + toOpen.Count + " items. Are you sure you wish to continue?";
            const string caption = "Are you Sure?";

            const MessageBoxButton btnMessageBox = MessageBoxButton.YesNo;
            const MessageBoxImage imgMessageBox = MessageBoxImage.Warning;

            MessageBoxResult rsltMessageBox = messageDialog.Show(messageBoxText, caption, btnMessageBox, imgMessageBox);

            if (rsltMessageBox == MessageBoxResult.Yes)
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
        private RelayCommand<Codex>? _editCodexCommand;
        public RelayCommand<Codex> EditCodexCommand => _editCodexCommand ??= new(EditCodex);
        public static void EditCodex(Codex? toEdit)
        {
            if (toEdit is null) return;
            CodexEditWindow editWindow = new(new CodexEditViewModel(toEdit));
            editWindow.ShowDialog();
            editWindow.Topmost = true;
        }

        //Edit Multiple files
        private RelayCommand<IList>? _editCodicesCommand;
        public RelayCommand<IList> EditCodicesCommand => _editCodicesCommand ??= new(EditCodices);
        public static void EditCodices(IList? toEdit)
        {
            List<Codex>? toEditList = toEdit?.Cast<Codex>().ToList();
            if (toEditList is null) return;

            if (toEditList.Count == 1)
            {
                EditCodex(toEditList.First());
                return;
            }

            CodexBulkEditWindow window = new(new CodexBulkEditViewModel(toEditList));
            window.ShowDialog();
            window.Topmost = true;
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
            if (String.IsNullOrEmpty(toShow?.Path) || !File.Exists(toShow.Path)) return;
            string? folderPath = Path.GetDirectoryName(toShow.Path);
            if (String.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath)) return;
            IOService.ShowInExplorer(folderPath);
        }

        //Move Codex to other CodexCollection
        private RelayCommand<object[]>? _moveToCollectionCommand;
        public RelayCommand<object[]> MoveToCollectionCommand => _moveToCollectionCommand ??= new(MoveToCollection);
        public void MoveToCollection(object[]? par)
        {
            if (par == null) return;

            //par contains 2 parameters
            CodexCollection targetCollection = new((string)par[0]);
            List<Codex> toMoveList = new();

            //extract Codex parameter
            if (par[1] is Codex codex)
            {
                toMoveList.Add(codex);
            }
            else
            {
                if (par[1] as IList is { } list) toMoveList = list.Cast<Codex>().ToList();
            }

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

            //MessageBox "Are you Sure?"
            string messageSingle = $"Moving  {toMoveList[0].Title} to {targetCollection.DirectoryName} will remove all tags from the item, are you sure you wish to continue?";
            string messageMultiple = $"Moving these {toMoveList.Count} items to {targetCollection.DirectoryName} will remove all tags from these items, are you sure you wish to continue?";

            string sCaption = "Are you Sure?";
            string sMessageBoxText = toMoveList.Count == 1 ? messageSingle : messageMultiple;

            MessageBoxButton btnMessageBox = MessageBoxButton.YesNo;
            MessageBoxImage imgMessageBox = MessageBoxImage.Warning;

            MessageBoxResult rsltMessageBox = messageDialog.Show(sMessageBoxText, sCaption, btnMessageBox, imgMessageBox);

            if (rsltMessageBox == MessageBoxResult.Yes)
            {
                bool success = targetCollection.LoadCodices();
                if (!success)
                {
                    messageDialog.Show($"Could not move items to {targetCollection.DirectoryName}", "Target collection could not be loaded.", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                foreach (Codex toMove in toMoveList)
                {
                    toMove.Tags.Clear();
                    toMove.ID = Utils.GetAvailableID(targetCollection.AllCodices);

                    //Add Codex to target CodexCollection
                    targetCollection.AllCodices.Add(toMove);

                    //Move cover art to right folder with new ID
                    Codex tempCodex = new(toMove);
                    tempCodex.SetImagePaths(targetCollection);

                    if (Path.Exists(toMove.CoverArt))
                        File.Copy(toMove.CoverArt, tempCodex.CoverArt);
                    if (Path.Exists(toMove.CoverArt))
                        File.Copy(toMove.Thumbnail, tempCodex.Thumbnail);

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
            MainViewModel.CollectionVM.CurrentCollection.DeleteCodices(toDelete?.Cast<Codex>().ToList() ?? new());
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
            MainViewModel.CollectionVM.CurrentCollection.BanishCodices(toBanish?.Cast<Codex>().ToList() ?? new());
            DeleteCodices(toBanish);
        }

        private ReturningRelayCommand<Codex, Task>? _getMetaDataCommand;
        public ReturningRelayCommand<Codex, Task> GetMetaDataCommand => _getMetaDataCommand ??= new(StartGetMetaDataProcess);


        private ReturningRelayCommand<IList, Task>? _getMetaDataBulkCommand;
        public ReturningRelayCommand<IList, Task> GetMetaDataBulkCommand => _getMetaDataBulkCommand ??= new(
            async codices =>
            {
                try
                {
                    await StartGetMetaDataProcess(codices?.Cast<Codex>().ToList() ?? new());
                }
                catch (OperationCanceledException ex)
                {
                    Logger.Warn("Renewing metadata has been cancelled", ex);
                    await Task.Run(() => ProgressViewModel.GetInstance().ConfirmCancellation());
                }
            }
        );

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
                MaxDegreeOfParallelism = Environment.ProcessorCount / 2
            };

            ChooseMetaDataViewModel chooseMetaDataVM = new();

            await Parallel.ForEachAsync(codices, parallelOptions, async (codex, _) => await GetMetaData(codex, chooseMetaDataVM));

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
            Dictionary<MetaDataSource, Codex> metaDataFromSource = new();

            //Make Codex with only sources which can be filled with new data
            Codex metaDatalessCodex = new()
            {
                Path = codex.Path,
                SourceURL = codex.SourceURL,
                ISBN = codex.ISBN
            };

            //First try to get sources from other sources
            //Pdf can contain ISBN number
            PdfSourceViewModel pdfSourceVM = new();
            if (pdfSourceVM.IsValidSource(codex) && String.IsNullOrEmpty(codex.ISBN))
            {
                var pdfData = await pdfSourceVM.GetMetaData(metaDatalessCodex);
                codex.ISBN = pdfData.ISBN;
                metaDatalessCodex.ISBN = pdfData.ISBN;

                //already store this so pdf doesn't need to be opened twice
                metaDataFromSource.Add(MetaDataSource.PDF, pdfData);
            }

            // Now use bits and pieces of the Codices in MetaDataFromSource to set the actual metadata based on preferences
            var properties = SettingsViewModel.GetInstance().MetaDataPreferences;

            //Codex with metadata that will be shown to the user, and asked if they want to use it
            Codex toAsk = new();
            bool shouldAsk = false;

            //Iterate over all the properties and set them
            foreach (var prop in properties)
            {

                if (prop.OverwriteMode == MetaDataOverwriteMode.Never) continue;
                if (prop.OverwriteMode == MetaDataOverwriteMode.IfEmpty && !prop.IsEmpty(codex)) continue;
                if (prop.Name == nameof(Codex.CoverArt)) continue; //Covers are done separately

                //propHolder will hold the property from the top preferred source
                Codex propHolder = new();

                //iterate over the sources in reverse because overwriting causes the last ones to remain
                foreach (var source in prop.SourcePriority.AsEnumerable().Reverse())
                {
                    ProgressViewModel.GlobalCancellationTokenSource.Token.ThrowIfCancellationRequested();

                    // Check if there is metadata from this source to use
                    if (!metaDataFromSource.ContainsKey(source))
                    {
                        SourceViewModel? sourceVM = SourceViewModel.GetSourceVM(source);
                        if (sourceVM is null) continue;
                        if (!sourceVM.IsValidSource(codex)) continue;
                        var metaDataHolder = await sourceVM.GetMetaData(metaDatalessCodex);
                        metaDataFromSource.Add(source, metaDataHolder);
                    }
                    // Set the prop Data from this source in propHolder
                    // if the new value is not null/default/empty
                    if (!prop.IsEmpty(metaDataFromSource[source]))
                    {
                        prop.SetProp(propHolder, metaDataFromSource[source]);
                    }
                }

                //if no value was found for this prop, do nothing
                if (prop.IsEmpty(propHolder)) continue;

                if (prop.OverwriteMode == MetaDataOverwriteMode.Always || prop.IsEmpty(codex))
                {
                    prop.SetProp(codex, propHolder);
                }
                else if (prop.OverwriteMode == MetaDataOverwriteMode.Ask)
                {
                    bool isDifferent = prop.Name == nameof(Codex.Tags) ?
                    // in case of tags, check if source adds tags that aren't there yet
                    ((IList<Tag>)prop.GetProp(propHolder)!).Except((IList<Tag>)prop.GetProp(codex)!).Any()
                    //check if ToString() representations are different, doesn't work for tags
                    : prop.GetProp(codex)?.ToString() != prop.GetProp(propHolder)?.ToString();
                    if (isDifferent)
                    {
                        prop.SetProp(toAsk, propHolder);
                        shouldAsk = true; //set shouldAsk to true when we found at lease one none empty prop that should be asked
                    }
                }
            }

            if (shouldAsk)
            {
                chooseMetaDataVM.AddCodexPair(codex, toAsk);
            }

            ProgressViewModel.GetInstance().IncrementCounter();
        }

        private ReturningRelayCommand<Codex, Task>? _getCoverCommand;
        public ReturningRelayCommand<Codex, Task> GetCoverCommand => _getCoverCommand ??=
            new(async codex =>
            {
                if (codex is null) return;
                await CoverService.GetCover(new List<Codex>() { codex });
            });

        private ReturningRelayCommand<IList, Task>? _getCoverBulkCommand;
        public ReturningRelayCommand<IList, Task> GetCoverBulkCommand => _getCoverBulkCommand ??=
            new(async codices => await CoverService.GetCover(codices?.Cast<Codex>().ToList() ?? new()));

        public static void DataGridHandleKeyDown(object sender, KeyEventArgs e)
            => HandleKeyDownOnCodex(((DataGrid)sender).SelectedItems, e);
        public static void ListBoxHandleKeyDown(object sender, KeyEventArgs e)
            => HandleKeyDownOnCodex(((ListBox)sender).SelectedItems, e);
        public static void HandleKeyDownOnCodex(IList selectedItems, KeyEventArgs e)
        {
            List<Codex> codices = selectedItems.Cast<Codex>().ToList();
            int count = selectedItems.Count;
            if (count > 0)
            {
                switch (e.Key)
                {
                    case Key.Delete:
                        if (Keyboard.Modifiers == ModifierKeys.Alt)
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
                        if (Keyboard.Modifiers == ModifierKeys.Control)
                        {
                            //CTRL + E
                            EditCodices(codices);
                            e.Handled = true;
                        }
                        break;
                    case Key.F:
                        if (Keyboard.Modifiers == ModifierKeys.Control)
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
        void IDropTarget.DragOver(IDropInfo dropInfo)
        {
            if ((dropInfo.Data is TreeViewNode node && !node.Tag.IsGroup)
                || dropInfo.Data is Tag { IsGroup: false })
            {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
                dropInfo.Effects = DragDropEffects.Copy;
            }
        }

        void IDropTarget.Drop(IDropInfo dropInfo)
        {
            Codex targetCodex = (Codex)dropInfo.TargetItem;
            if (targetCodex is null) return;

            Tag? toAdd = dropInfo.Data switch
            {
                TreeViewNode node => node.Tag,
                Tag tag => tag,
                _ => null
            };

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
