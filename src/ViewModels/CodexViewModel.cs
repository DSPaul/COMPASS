using COMPASS.Commands;
using COMPASS.Models;
using COMPASS.Tools;
using COMPASS.ViewModels.Sources;
using COMPASS.Windows;
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

namespace COMPASS.ViewModels
{
    public class CodexViewModel : ObservableObject, IDropTarget
    {
        public CodexViewModel() { }

        #region Open Codex

        //Open Codex whereever
        public static bool OpenCodex(Codex codex)
        {
            bool success = PreferableFunction<Codex>.TryFunctions(SettingsViewModel.GetInstance().OpenCodexPriority, codex);
            if (!success) MessageBox.Show("Could not open codex, please check local path or URL");
            return success;
        }

        //Open File Offline
        private ReturningRelayCommand<Codex, bool> _openCodexLocallyCommand;
        public ReturningRelayCommand<Codex, bool> OpenCodexLocallyCommand => _openCodexLocallyCommand ??= new(OpenCodexLocally, CanOpenCodexLocally);
        public static bool OpenCodexLocally(Codex toOpen)
        {
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

                if (toOpen is null) return false;

                FileNotFoundWindow fileNotFoundWindow = new(new(toOpen))
                {
                    Owner = Application.Current.MainWindow
                };
                return (bool)fileNotFoundWindow.ShowDialog();
            }
        }
        public static bool CanOpenCodexLocally(Codex toOpen)
        {
            if (toOpen == null) return false;

            return toOpen.HasOfflineSource();
        }

        //Open File Online
        private ReturningRelayCommand<Codex, bool> _openCodexOnlineCommand;
        public ReturningRelayCommand<Codex, bool> OpenCodexOnlineCommand => _openCodexOnlineCommand ??= new(OpenCodexOnline, CanOpenCodexOnline);
        public static bool OpenCodexOnline(Codex toOpen)
        {
            if (!CanOpenCodexOnline(toOpen)) return false;
            try
            {
                Process.Start(new ProcessStartInfo(toOpen.SourceURL) { UseShellExecute = true });
                toOpen.LastOpened = DateTime.Now;
                toOpen.OpenedCount++;
                Logger.Info($"Opened {toOpen.SourceURL}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to open {toOpen.SourceURL}", ex);
                //fails if no internet, pinging 8.8.8.8 DNS instead of server because some sites like gmbinder block ping
                if (!Utils.PingURL()) Logger.Warn($"Cannot open online files when not connected to the internet", ex);
                return false;
            }

        }
        public static bool CanOpenCodexOnline(Codex toOpen)
        {
            if (toOpen == null) return false;

            return toOpen.HasOnlineSource();
        }

        //Open Multiple Files
        private ReturningRelayCommand<IList, bool> _openSelectedCodicesCommand;
        public ReturningRelayCommand<IList, bool> OpenSelectedCodicesCommand => _openSelectedCodicesCommand ??= new(l => OpenSelectedCodices(l.Cast<Codex>()));
        public static bool OpenSelectedCodices(IEnumerable<Codex> toOpen)
        {
            List<Codex> ToOpen = toOpen?.ToList();
            if (ToOpen is null) return false;

            if (ToOpen.Count == 1)
            {
                return OpenCodex(ToOpen.First());
            }

            //MessageBox "Are you Sure?"
            string sMessageBoxText = "You are about to open " + ToOpen.Count + " Files. Are you sure you wish to continue?";
            string sCaption = "Are you Sure?";

            MessageBoxButton btnMessageBox = MessageBoxButton.YesNo;
            MessageBoxImage imgMessageBox = MessageBoxImage.Warning;

            MessageBoxResult rsltMessageBox = MessageBox.Show(sMessageBoxText, sCaption, btnMessageBox, imgMessageBox);

            if (rsltMessageBox == MessageBoxResult.Yes)
            {
                foreach (Codex f in ToOpen) OpenCodex(f);
                return true;
            }
            else { return false; }
        }

        #endregion

        #region Edit Codex

        //Edit File
        private RelayCommand<Codex> _editCodexCommand;
        public RelayCommand<Codex> EditCodexCommand => _editCodexCommand ??= new(EditCodex);
        public static void EditCodex(Codex toEdit)
        {
            CodexEditWindow editWindow = new(new CodexEditViewModel(toEdit));
            editWindow.ShowDialog();
            editWindow.Topmost = true;
        }

        //Edit Multiple files
        private RelayCommand<IList> _editCodicesCommand;
        public RelayCommand<IList> EditCodicesCommand => _editCodicesCommand ??= new(EditCodices);
        public static void EditCodices(IList toEdit)
        {
            List<Codex> ToEdit = toEdit?.Cast<Codex>().ToList();
            if (ToEdit is null) return;

            if (ToEdit.Count == 1)
            {
                EditCodex(ToEdit.First());
                return;
            }

            CodexBulkEditWindow window = new(new CodexBulkEditViewModel(ToEdit));
            window.ShowDialog();
            window.Topmost = true;
        }

        #endregion

        #region Toggle Favorite 

        //Toggle Favorite
        private RelayCommand<Codex> _favoriteCodexCommand;
        public RelayCommand<Codex> FavoriteCodexCommand => _favoriteCodexCommand ??= new(FavoriteCodex);
        public static void FavoriteCodex(Codex toFavorite)
        {
            toFavorite.Favorite = !toFavorite.Favorite;
            string prefix = toFavorite.Favorite ? "Favorited" : "Unfavorited";
            Logger.Info($"{prefix} {toFavorite.Title}");
        }

        //Toggle Favorite
        private RelayCommand<IList> _favoriteCodicesCommand;
        public RelayCommand<IList> FavoriteCodicesCommand => _favoriteCodicesCommand ??= new(FavoriteCodices);
        public static void FavoriteCodices(IList toFavorite)
        {
            List<Codex> ToFavorite = toFavorite?.Cast<Codex>().ToList();
            if (ToFavorite.Count == 1)
            {
                FavoriteCodex(ToFavorite.First());
                return;
            }

            // if at least one is not favorited, set all to favorite
            // if all are already favorited, unfavorite all
            bool newVal = ToFavorite.Any(c => !c.Favorite);
            foreach (Codex codex in toFavorite)
            {
                codex.Favorite = newVal;
                Logger.Info($"{(newVal ? "Favorited" : "Unfavorited")} {codex.Title}");
            }
        }

        #endregion

        //Show in Explorer
        private RelayCommand<Codex> _showInExplorerCommand;
        public RelayCommand<Codex> ShowInExplorerCommand => _showInExplorerCommand ??= new(ShowInExplorer, CanOpenCodexLocally);
        public static void ShowInExplorer(Codex toShow)
        {
            string folderPath = Path.GetDirectoryName(toShow.Path);
            Utils.ShowInExplorer(folderPath);
        }

        //Move Codex to other CodexCollection
        private RelayCommand<object[]> _moveToCollectionCommand;
        public RelayCommand<object[]> MoveToCollectionCommand => _moveToCollectionCommand ??= new(MoveToCollection);
        public static void MoveToCollection(object[] par)
        {
            //par contains 2 parameters
            CodexCollection targetCollection = new((string)par[0]);
            List<Codex> ToMoveList = new();

            //Check if target Collection is valid
            if (targetCollection is null || targetCollection.DirectoryName == MainViewModel.CollectionVM.CurrentCollection.DirectoryName)
            {
                Logger.Warn($"Target Collection {targetCollection.DirectoryName} is invalid", new ArgumentException());
                return;
            }

            //extract Codex parameter
            if (par[1] is Codex codex)
            {
                ToMoveList.Add(codex);
            }
            else
            {
                IList list = par[1] as IList;
                ToMoveList = list.Cast<Codex>().ToList();
            }

            //MessageBox "Are you Sure?"
            string MessageSingle = $"Moving  {ToMoveList[0].Title} to {targetCollection.DirectoryName} will remove all tags from the Codex, are you sure you wish to continue?";
            string MessageMultiple = $"Moving these {ToMoveList.Count} files to {targetCollection.DirectoryName} will remove all tags from the Codices, are you sure you wish to continue?";

            string sCaption = "Are you Sure?";
            string sMessageBoxText = ToMoveList.Count == 1 ? MessageSingle : MessageMultiple;

            MessageBoxButton btnMessageBox = MessageBoxButton.YesNo;
            MessageBoxImage imgMessageBox = MessageBoxImage.Warning;

            MessageBoxResult rsltMessageBox = MessageBox.Show(sMessageBoxText, sCaption, btnMessageBox, imgMessageBox);

            if (rsltMessageBox == MessageBoxResult.Yes)
            {
                bool succes = targetCollection.LoadCodices();
                if (!succes)
                {
                    MessageBox.Show($"Could not move books to {targetCollection.DirectoryName}", "Target collection could not be loaded.", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                foreach (Codex ToMove in ToMoveList)
                {
                    ToMove.Tags.Clear();
                    // Give file new ID and move it to other folder
                    ToMove.ID = Utils.GetAvailableID(targetCollection.AllCodices);

                    //Add Codex to target CodexCollection
                    targetCollection.AllCodices.Add(ToMove);

                    //Move cover art to right folder with new ID
                    Codex TempCodex = new(ToMove);
                    TempCodex.SetImagePaths(targetCollection);

                    if (Path.Exists(ToMove.CoverArt))
                        File.Copy(ToMove.CoverArt, TempCodex.CoverArt);
                    if (Path.Exists(ToMove.CoverArt))
                        File.Copy(ToMove.Thumbnail, TempCodex.Thumbnail);

                    //Delete file in original folder
                    MainViewModel.CollectionVM.CurrentCollection.DeleteCodex(ToMove);
                    MainViewModel.CollectionVM.FilterVM.RemoveCodex(ToMove);

                    //Update the cover art metadata to new path, has to happen after delete so old one gets deleted
                    ToMove.Copy(TempCodex);

                    Logger.Info($"Moved {ToMove.Title} from {MainViewModel.CollectionVM.CurrentCollection.DirectoryName} to {targetCollection.DirectoryName}");
                }
                //Save changes to TargetCollection
                targetCollection.SaveCodices();
            }
        }

        //Delete Codex
        private RelayCommand<Codex> _deleteCodexCommand;
        public RelayCommand<Codex> DeleteCodexCommand => _deleteCodexCommand ??= new(DeleteCodex);
        public static void DeleteCodex(Codex toDelete) => DeleteCodices(new List<Codex>() { toDelete });

        //Delete Codices
        private RelayCommand<IList> _deleteCodicesCommand;
        public RelayCommand<IList> DeleteCodicesCommand => _deleteCodicesCommand ??= new(DeleteCodices);
        public static void DeleteCodices(IList toDelete)
        {
            MainViewModel.CollectionVM.CurrentCollection.DeleteCodices(toDelete);
            MainViewModel.CollectionVM.FilterVM.ReFilter();
        }

        //Banish Codex
        private RelayCommand<Codex> _banishCodexCommand;
        public RelayCommand<Codex> BanishCodexCommand => _banishCodexCommand ??= new((codex) => BanishCodices(new List<Codex>() { codex }));

        //Banish Codices
        private RelayCommand<IList> _banishCodicesCommand;
        public RelayCommand<IList> BanishCodicesCommand => _banishCodicesCommand ??= new(BanishCodices);
        public static void BanishCodices(IList toBanish)
        {
            MainViewModel.CollectionVM.CurrentCollection.BanishCodices(toBanish);
            DeleteCodices(toBanish);
        }

        private ReturningRelayCommand<Codex, Task> _getMetaDataCommand;
        public ReturningRelayCommand<Codex, Task> GetMetaDataCommand => _getMetaDataCommand ??= new(StartGetMetaDataProcess);


        private ReturningRelayCommand<IList, Task> _getMetaDataBulkCommand;
        public ReturningRelayCommand<IList, Task> GetMetaDataBulkCommand => _getMetaDataBulkCommand ??= new(StartGetMetaDataProcessWithCast);
        private static async Task StartGetMetaDataProcessWithCast(IList codices) => await StartGetMetaDataProcess(codices.Cast<Codex>().ToList());

        public static async Task StartGetMetaDataProcess(Codex codex) => await StartGetMetaDataProcess(new List<Codex>() { codex });
        public static async Task StartGetMetaDataProcess(IList<Codex> codices)
        {
            var ProgressVM = ProgressViewModel.GetInstance();
            ProgressVM.ResetCounter();
            ProgressVM.Text = "Getting MetaData";
            ProgressVM.TotalAmount = codices.Count;

            ParallelOptions parallelOptions = new()
            {
                MaxDegreeOfParallelism = 8
            };

            ChooseMetaDataViewModel ChooseMetaDataVM = new();

            await Parallel.ForEachAsync(codices, parallelOptions, async (codex, token) => await GetMetaData(codex, ChooseMetaDataVM));

            if (ChooseMetaDataVM.CodicesWithChoices.Any())
            {
                ChooseMetaDataWindow window = new(ChooseMetaDataVM);
                window.Show();
            }

            MainViewModel.CollectionVM.FilterVM.PopulateMetaDataCollections();
            MainViewModel.CollectionVM.CurrentCollection.Save();
            MainViewModel.CollectionVM.FilterVM.ReFilter();
        }

        private static async Task GetMetaData(Codex codex, ChooseMetaDataViewModel chooseMetaDataVM)
        {
            // Lazy load metadata from all the sources, use dict to store
            Dictionary<MetaDataSource, Codex> MetaDataFromSource = new();

            //Make Codex with only sources which can be filled with new data
            Codex MetaDatalessCodex = new()
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
                var pdfData = await pdfSourceVM.GetMetaData(MetaDatalessCodex);
                codex.ISBN = pdfData.ISBN;
                MetaDatalessCodex.ISBN = pdfData.ISBN;

                //already store this so pdf doesn't need to be opened twice
                MetaDataFromSource.Add(MetaDataSource.PDF, pdfData);
            };

            // Now use bits and pieces of the Codices in MetaDataFromSource to set the actual metadata based on preferences
            var properties = SettingsViewModel.GetInstance().MetaDataPreferences;

            //Codex with metadata that will be shown to the user, and asked if they want to use it
            Codex ToAsk = new();
            bool shouldAsk = false;

            //Iterate over all the properties and set them
            foreach (var prop in properties)
            {
                if (prop.OverwriteMode == MetaDataOverwriteMode.Never) continue;
                if (prop.OverwriteMode == MetaDataOverwriteMode.IfEmpty && !prop.IsEmpty(codex)) continue;
                if (prop.Label == "Cover Art") continue; //Covers is done seperately

                //propHolder will hold the property from the top prefered source
                Codex propHolder = new();

                //iterate over the sources in reverse because overwriting causes the last ones to remain
                foreach (var source in prop.SourcePriority.AsEnumerable().Reverse())
                {
                    // Check if there is metadata from this source to use
                    if (!MetaDataFromSource.Keys.Contains(source))
                    {
                        SourceViewModel SourceVM = SourceViewModel.GetSourceVM(source);
                        if (SourceVM is null) continue;
                        if (!SourceVM.IsValidSource(codex)) continue;
                        var metaDataHolder = await SourceVM.GetMetaData(MetaDatalessCodex);
                        MetaDataFromSource.Add(source, metaDataHolder);
                    };
                    // Set the prop Data from this source in propHolder
                    // if the new value is not null/default/empty
                    if (!prop.IsEmpty(MetaDataFromSource[source]))
                    {
                        prop.SetProp(propHolder, MetaDataFromSource[source]);
                    }
                }

                if (!prop.IsEmpty(propHolder))
                {

                    if (prop.OverwriteMode == MetaDataOverwriteMode.Always || prop.IsEmpty(codex))
                    {
                        prop.SetProp(codex, propHolder);
                    }
                    else if (prop.OverwriteMode == MetaDataOverwriteMode.Ask)
                    {
                        bool isDifferent = false;
                        //check if ToString() representations are different, doesn't work for tags
                        isDifferent = isDifferent || (prop.Label != "Tags" && prop.GetProp(codex)?.ToString() != prop.GetProp(propHolder)?.ToString());
                        // use For tags, check if source adds tags that aren't there yet
                        isDifferent = isDifferent || (prop.Label == "Tags" && ((IList<Tag>)prop.GetProp(propHolder)).Except((IList<Tag>)prop.GetProp(codex)).Any());
                        if (isDifferent)
                        {
                            prop.SetProp(ToAsk, propHolder);
                            shouldAsk = true; //set shouldAsk to true when we found at lease one none empty prop that should be asked
                        }
                    }
                }
            }

            if (shouldAsk)
            {
                chooseMetaDataVM.AddCodexPair(codex, ToAsk);
            }

            ProgressViewModel.GetInstance().IncrementCounter();
        }

        private ReturningRelayCommand<Codex, Task> _getCoverCommand;
        public ReturningRelayCommand<Codex, Task> GetCoverCommand => _getCoverCommand ??=
            new(async codex => await CoverFetcher.GetCover(new List<Codex>() { codex }));

        private ReturningRelayCommand<IList, Task> _getCoverBulkCommand;
        public ReturningRelayCommand<IList, Task> GetCoverBulkCommand => _getCoverBulkCommand ??=
            new(async codices => await CoverFetcher.GetCover(codices.Cast<Codex>().ToList()));

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
                || (dropInfo.Data is Tag tag && !tag.IsGroup))
            {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
                dropInfo.Effects = DragDropEffects.Copy;
            }
        }

        void IDropTarget.Drop(IDropInfo dropInfo)
        {
            Codex TargetCodex = (Codex)dropInfo.TargetItem;
            if (TargetCodex is not null)
            {
                Tag toAdd = dropInfo.Data switch
                {
                    TreeViewNode node => node.Tag,
                    Tag tag => tag,
                    _ => null
                };

                if (!TargetCodex.Tags.Contains(toAdd))
                {
                    TargetCodex.Tags.Add(toAdd);
                    MainViewModel.CollectionVM.FilterVM.ReFilter();
                }
            }
        }
        #endregion
    }
}
