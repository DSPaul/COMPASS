using COMPASS.Commands;
using COMPASS.Models;
using COMPASS.Tools;
using COMPASS.Windows;
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
    public class CodexViewModel : ViewModelBase
    {
        public CodexViewModel() { }

        #region Open Codex

        //Open Codex whereever
        public static bool OpenCodex(Codex codex)
        {
            bool success = Utils.TryFunctions(MVM.SettingsVM.OpenCodexPriority, codex);
            if (!success) MessageBox.Show("Could not open codex, please check local path or URL");
            return success;
        }

        //Open File Offline
        private ReturningRelayCommand<Codex, bool> _openCodexLocallyCommand;
        public ReturningRelayCommand<Codex, bool> OpenCodexLocallyCommand => _openCodexLocallyCommand ??= new(OpenCodexLocally, CanOpenCodexLocally);
        public static bool OpenCodexLocally(Codex toOpen)
        {
            if (String.IsNullOrEmpty(toOpen.Path)) return false;
            try
            {
                Process.Start(new ProcessStartInfo(toOpen.Path) { UseShellExecute = true });
                toOpen.LastOpened = DateTime.Now;
                toOpen.OpenedCount++;
                return true;
            }
            catch (Exception ex)
            {
                Logger.log.Error(ex.InnerException);

                if (toOpen == null) return false;

                //Check if folder exists, if not ask users to rename
                var dir = Path.GetDirectoryName(toOpen.Path);
                if (!Directory.Exists(dir))
                {
                    string message = $"{toOpen.Path} could not be found. \n" +
                    $"If you renamed a folder, go to \n" +
                    $"Settings -> General -> Fix Renamed Folder\n" +
                    $"to update all references to the old folder name.";
                    MessageBox.Show(message, "Path could not be found", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                return false;
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
            //fails if no internet, pinging 8.8.8.8 DNS instead of server because some sites like gmbinder block ping
            if (!Utils.PingURL()) return false;

            try
            {
                Process.Start(new ProcessStartInfo(toOpen.SourceURL) { UseShellExecute = true });
                toOpen.LastOpened = DateTime.Now;
                toOpen.OpenedCount++;
                return true;
            }
            catch (Exception ex)
            {
                Logger.log.Error(ex.InnerException);
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
        public static void FavoriteCodex(Codex toFavorite) => toFavorite.Favorite = !toFavorite.Favorite;

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
            foreach (Codex c in toFavorite)
            {
                c.Favorite = newVal;
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
            string targetCollectionName = (string)par[0];
            List<Codex> ToMoveList = new();

            //Check if target Collection is valid
            if (targetCollectionName is null || targetCollectionName == MVM.CurrentCollectionName) return;

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
            string MessageSingle = "Moving " + ToMoveList[0].Title + " to " + targetCollectionName + " will remove all tags from the Codex, are you sure you wish to continue?";
            string MessageMultiple = "Moving these " + ToMoveList.Count + " files to " + targetCollectionName + " will remove all tags from the Codices, are you sure you wish to continue?";

            string sCaption = "Are you Sure?";
            string sMessageBoxText = ToMoveList.Count == 1 ? MessageSingle : MessageMultiple;

            MessageBoxButton btnMessageBox = MessageBoxButton.YesNo;
            MessageBoxImage imgMessageBox = MessageBoxImage.Warning;

            MessageBoxResult rsltMessageBox = MessageBox.Show(sMessageBoxText, sCaption, btnMessageBox, imgMessageBox);

            if (rsltMessageBox == MessageBoxResult.Yes)
            {
                CodexCollection TargetCollection = new(targetCollectionName);
                foreach (Codex ToMove in ToMoveList)
                {
                    ToMove.Tags.Clear();
                    // Give file new ID and move it to other folder
                    ToMove.ID = Utils.GetAvailableID(TargetCollection.AllCodices);

                    //Add Codex to target CodexCollection
                    TargetCollection.AllCodices.Add(ToMove);

                    //Update Authors,Publisher, ect. list in target collection
                    TargetCollection.PopulateMetaDataCollections();

                    //Move cover art to right folder with new ID
                    string newCoverArt = CodexCollection.CollectionsPath + targetCollectionName + @"\CoverArt\" + ToMove.ID + ".png";
                    string newThumbnail = CodexCollection.CollectionsPath + targetCollectionName + @"\Thumbnails\" + ToMove.ID + ".png";
                    File.Copy(ToMove.CoverArt, newCoverArt);
                    File.Copy(ToMove.Thumbnail, newThumbnail);

                    //Delete file in original folder
                    MVM.CurrentCollection.DeleteCodex(ToMove);
                    MVM.FilterVM.RemoveCodex(ToMove);

                    //Update the cover art metadata to new path, has to happen after delete so old one gets deleted
                    ToMove.CoverArt = newCoverArt;
                    ToMove.Thumbnail = newThumbnail;
                }
                //Save changes to TargetCollection
                TargetCollection.SaveCodices();
            }
        }

        //Delete Codex
        private RelayCommand<Codex> _deleteCodexCommand;
        public RelayCommand<Codex> DeleteCodexCommand => _deleteCodexCommand ??= new(DeleteCodex);
        public static void DeleteCodex(Codex toDelete) => DeleteCodices(new List<Codex>() { toDelete });

        //Delete Codices
        private RelayCommand<IList> _deleteCodicesCommand;
        public RelayCommand<IList> DeleteCodicesCommand => _deleteCodicesCommand ??= new(DeleteCodices);
        public static void DeleteCodices(IList toDel)
        {
            List<Codex> toDeleteList = toDel?.Cast<Codex>().ToList();
            int count = toDeleteList.Count;
            string message = $"You are about to delete {count} file{(count > 1 ? @"s" : @"")}. " +
                           $"This cannot be undone. " +
                           $"Are you sure you want to continue?";
            var result = MessageBox.Show(message, "Delete", MessageBoxButton.OKCancel);
            if (result == MessageBoxResult.OK)
            {
                foreach (Codex ToDelete in toDeleteList)
                {
                    MVM.CurrentCollection.DeleteCodex(ToDelete);
                }
                MVM.Refresh();
            }
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
    }
}
