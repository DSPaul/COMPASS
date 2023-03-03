using COMPASS.Commands;
using COMPASS.Models;
using COMPASS.Tools;
using COMPASS.Windows;
using GongSolutions.Wpf.DragDrop;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        private ReturningRelayCommand<IEnumerable<Codex>, bool> _openSelectedCodicesCommand;
        public ReturningRelayCommand<IEnumerable<Codex>, bool> OpenSelectedCodicesCommand => _openSelectedCodicesCommand ??= new(OpenSelectedCodices);
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
            ProcessStartInfo startInfo = new()
            {
                Arguments = folderPath,
                FileName = "explorer.exe"
            };
            Process.Start(startInfo);
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
                    string newCoverArt = CodexCollection.CollectionsPath + targetCollection.DirectoryName + @"\CoverArt\" + ToMove.ID + ".png";
                    string newThumbnail = CodexCollection.CollectionsPath + targetCollection.DirectoryName + @"\Thumbnails\" + ToMove.ID + ".png";
                    if (Path.Exists(ToMove.CoverArt))
                        File.Copy(ToMove.CoverArt, newCoverArt);
                    if (Path.Exists(ToMove.CoverArt))
                        File.Copy(ToMove.Thumbnail, newThumbnail);

                    //Delete file in original folder
                    MainViewModel.CollectionVM.CurrentCollection.DeleteCodex(ToMove);
                    MainViewModel.CollectionVM.FilterVM.RemoveCodex(ToMove);

                    //Update the cover art metadata to new path, has to happen after delete so old one gets deleted
                    ToMove.CoverArt = newCoverArt;
                    ToMove.Thumbnail = newThumbnail;

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
                        DeleteCodices(codices);
                        e.Handled = true;
                        break;
                    case Key.Enter:
                        OpenSelectedCodices(codices);
                        e.Handled = true;
                        break;
                    case Key.E:
                        //CTRL + E
                        if (Keyboard.Modifiers == ModifierKeys.Control)
                        {
                            EditCodices(codices);
                            e.Handled = true;
                        }
                        break;
                    case Key.F:
                        //CTRL + F
                        if (Keyboard.Modifiers == ModifierKeys.Control)
                        {
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
