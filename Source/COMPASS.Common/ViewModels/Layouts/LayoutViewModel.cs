using COMPASS.Common.Models;
using COMPASS.Common.ViewModels.Import;
using GongSolutions.Wpf.DragDrop;
using System;
using System.IO;
using System.Linq;
using System.Windows;

namespace COMPASS.Common.ViewModels.Layouts
{
    public abstract class LayoutViewModel : ViewModelBase, IDropTarget
    {
        public enum Layout
        {
            List,
            Card,
            Tile,
            Home
        }

        // Should put this function separate Factory class for proper factory pattern
        // but I don't see the point, seems a lot of boilerplate without real advantages
        public static LayoutViewModel GetLayout(Layout? layout = null)
        {
            layout ??= (Layout)Properties.Settings.Default.PreferedLayout;
            Properties.Settings.Default.PreferedLayout = (int)layout;
            LayoutViewModel? newLayout = layout switch
            {
                Layout.Home => new HomeLayoutViewModel(),
                Layout.List => new ListLayoutViewModel(),
                Layout.Card => new CardLayoutViewModel(),
                Layout.Tile => new TileLayoutViewModel(),
                _ => null
            };
            if (newLayout == null) throw new NotImplementedException(layout.ToString());
            return newLayout;
        }

        public void UpdateDoVirtualization() => RaisePropertyChanged(nameof(DoVirtualization));

        #region Properties

        public CodexViewModel CodexVM { get; init; } = new();

        //Selected File
        private Codex? _selectedCodex;
        public Codex? SelectedCodex
        {
            get => _selectedCodex;
            set => SetProperty(ref _selectedCodex, value);
        }

        public abstract bool DoVirtualization { get; }

        //Set Type of view
        public Layout LayoutType { get; init; }
        #endregion

        public void DragOver(IDropInfo dropInfo)
        {
            if (dropInfo.Data is DataObject)
            {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
                dropInfo.Effects = DragDropEffects.Copy;
            }
            else
            {
                dropInfo.NotHandled = true;
            }
        }

        public async void Drop(IDropInfo dropInfo)
        {
            if (dropInfo.Data is DataObject data)
            {
                var paths = data.GetFileDropList();

                var folders = paths.Cast<string>().Where(path => File.GetAttributes(path).HasFlag(FileAttributes.Directory)).ToList();
                var files = paths.Cast<string>().Where(path => !File.GetAttributes(path).HasFlag(FileAttributes.Directory)).ToList();

                //check for folder import
                if (folders.Any())
                {
                    ImportFolderViewModel folderImportVM = new(manuallyTriggered: true)
                    {
                        RecursiveDirectories = folders,
                        Files = files
                    };
                    await folderImportVM.Import();
                }
                //If no files or folders, to nothing
                else if (!files.Any())
                {
                    return;
                }
                //Check if its a cmpss file, do import if so
                else if (files.Count == 1 && files.First().EndsWith(Constants.SatchelExtension))
                {
                    await MainViewModel.CollectionVM.ImportSatchelAsync(files.First());
                }
                //If none of the above, just import the files
                else
                {
                    await ImportViewModel.ImportFilesAsync(files);
                }
            }
        }
    }
}
