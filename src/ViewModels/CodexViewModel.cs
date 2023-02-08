using COMPASS.Models;
using COMPASS.Tools;
using COMPASS.ViewModels.Commands;
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

        //Open Codex whereever
        public static bool OpenCodex(Codex codex)
        {
            bool success = Utils.TryFunctions(MVM.SettingsVM.OpenCodexPriority, codex);
            if (!success) MessageBox.Show("Could not open codex, please check local path or URL");
            return success;
        }

        //Open File Offline
        public ReturningRelayCommand<Codex> OpenCodexLocallyCommand => new(OpenCodexLocally, CanOpenCodexLocally);
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
        public ReturningRelayCommand<Codex> OpenCodexOnlineCommand => new(OpenCodexOnline, CanOpenCodexOnline);
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
        public ReturningRelayCommand<IEnumerable> OpenSelectedCodicesCommand => new(OpenSelectedCodices);
        public static bool OpenSelectedCodices(IEnumerable toOpen)
        {
            if (toOpen == null) return false;
            List<Codex> ToOpen = toOpen.Cast<Codex>().ToList();
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

        //Edit File
        public RelayCommand<Codex> EditCodexCommand => new(EditCodex);
        public static void EditCodex(Codex toEdit)
        {
            //MVM.CurrentEditViewModel = new CodexEditViewModel(toEdit);
            CodexEditWindow editWindow = new(new CodexEditViewModel(toEdit));
            editWindow.ShowDialog();
            editWindow.Topmost = true;
        }

        public RelayCommand<Codex> FavoriteCodexCommand => new(FavoriteCodex);
        public static void FavoriteCodex(Codex toFavorite) => toFavorite.Favorite = !toFavorite.Favorite;

        //Edit Multiple files
        public RelayCommand<IEnumerable> EditCodicesCommand => new(EditCodices);
        public static void EditCodices(IEnumerable toEdit)
        {
            if (toEdit == null) return;
            List<Codex> ToEdit = toEdit.Cast<Codex>().ToList();
            FileBulkEditWindow fpw = new(new CodexBulkEditViewModel(ToEdit));
            fpw.ShowDialog();
            fpw.Topmost = true;
        }

        //Show in Explorer
        public RelayCommand<Codex> ShowInExplorerCommand => new(ShowInExplorer, CanOpenCodexLocally);
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
        public RelayCommand<object[]> MoveToCollectionCommand => new(MoveToCollection);
        public static void MoveToCollection(object[] par)
        {
            //par contains 2 parameters
            List<Codex> ToMoveList = new();
            string targetCollectionName;

            //extract Collection parameter
            if (par[0] != null) targetCollectionName = (string)par[0];
            else return;
            if (targetCollectionName == MVM.CurrentCollectionName) return;

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
                    MVM.CollectionVM.RemoveCodex(ToMove);

                    //Update the cover art metadata to new path, has to happen after delete so old one gets deleted
                    ToMove.CoverArt = newCoverArt;
                    ToMove.Thumbnail = newThumbnail;
                }
                //Save changes to TargetCollection
                TargetCollection.SaveCodices();
            }
        }

        //Delete Codex
        public RelayCommand<object> DeleteCodexCommand => new(DeleteCodex);
        public static void DeleteCodex(object o)
        {
            //works for Single codex and list
            List<Codex> ToDeleteList = new();

            // if single codex, add to list
            if (o is Codex)
            {
                ToDeleteList.Add(o as Codex);
            }
            // if already list, cast is as such
            else
            {
                IList list = o as IList;
                ToDeleteList = list.Cast<Codex>().ToList();
            }
            //Actually delete stuff
            foreach (Codex ToDelete in ToDeleteList)
            {
                MVM.CurrentCollection.DeleteCodex(ToDelete);
                MVM.CollectionVM.RemoveCodex(ToDelete);
            }
            MVM.Refresh();
        }

        public static void DataGridHandleKeyDown(object sender, KeyEventArgs e) => HandleKeyDownOnCodex(((DataGrid)sender).SelectedItems, e);
        public static void ListBoxHandleKeyDown(object sender, KeyEventArgs e) => HandleKeyDownOnCodex(((ListBox)sender).SelectedItems, e);
        public static void HandleKeyDownOnCodex(IList selectedItems, KeyEventArgs e)
        {
            int count = selectedItems.Count;
            if (count > 0)
            {
                switch (e.Key)
                {
                    case Key.Delete:
                        string message = $"You are about to delete {count} file{(count > 1 ? @"s" : @"")}. " +
                            $"This cannot be undone. " +
                            $"Are you sure you want to continue?";
                        var result = MessageBox.Show(message, "Delete", MessageBoxButton.OKCancel);
                        if (result == MessageBoxResult.OK)
                        {
                            DeleteCodex(selectedItems);
                        }
                        break;
                }
            }
        }
    }
}
