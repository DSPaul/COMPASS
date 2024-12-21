
using Avalonia.Input;
using COMPASS.Common.Models;
using COMPASS.Common.Services;
using COMPASS.Common.ViewModels.Import;
using System;
using System.IO;
using System.Linq;

namespace COMPASS.Common.ViewModels.Layouts
{
    public abstract class LayoutViewModel : ViewModelBase
    {
        public enum Layout
        {
            List,
            Card,
            Tile,
            Home
        }

        // Should put this function separate Factory class for proper factory pattern,
        // but I don't see the point, seems a lot of boilerplate without real advantages
        public static LayoutViewModel GetLayout(Layout? layout = null)
        {
            layout ??= PreferencesService.GetInstance().Preferences.UIState.StartupLayout;
            PreferencesService.GetInstance().Preferences.UIState.StartupLayout = (Layout)layout;
            return layout switch
            {
                Layout.Home => new HomeLayoutViewModel(),
                Layout.List => new ListLayoutViewModel(),
                Layout.Card => new CardLayoutViewModel(),
                Layout.Tile => new TileLayoutViewModel(),
                _ => throw new NotImplementedException(layout.ToString())
            };
        }

        //TODO check if this is still needed
        //public void UpdateDoVirtualization() => OnPropertyChanged(nameof(DoVirtualization));

        #region Properties

        public CodexViewModel CodexVM { get; init; } = new();

        //Selected File
        private Codex? _selectedCodex;
        public Codex? SelectedCodex
        {
            get => _selectedCodex;
            set => SetProperty(ref _selectedCodex, value);
        }

        //TODO check if this is still needed, remove abstract for now so derived classes can skip it
        //public abstract bool DoVirtualization { get; }
        public bool DoVirtualization { get; }

        //Set Type of view
        public Layout LayoutType { get; init; }
        #endregion

        public void OnDragOver(object? sender, DragEventArgs e)
        {
            if (e.Data is DataObject)
            {
                //TODO: handle UI changes on hover manually
                //e.DropTargetAdorner = DropTargetAdorners.Highlight;
                e.DragEffects = DragDropEffects.Copy;
                e.Handled = true;
            }
        }

        public async void OnDrop(object? sender, DragEventArgs e)
        {
            if (e.Data is DataObject data)
            {
                var paths = data
                    .GetFiles()?
                    .Select(f => f.Path.AbsolutePath)
                    .ToList();

                if (paths is null) return;

                var folders = paths.Where(path => File.GetAttributes(path).HasFlag(FileAttributes.Directory)).ToList();
                var files = paths.Where(path => !File.GetAttributes(path).HasFlag(FileAttributes.Directory)).ToList();

                //check for folder import
                if (folders.Count != 0)
                {
                    ImportFolderViewModel folderImportVM = new(manuallyTriggered: true)
                    {
                        RecursiveDirectories = folders,
                        Files = files
                    };
                    await folderImportVM.Import();
                }
                else switch (files.Count)
                {
                    //If no files or folders, to nothing
                    case 0:
                        return;
                    //Check if it's a cmpss file, do import if so
                    case 1 when files.First().EndsWith(Constants.SatchelExtension):
                        await MainViewModel.CollectionVM.ImportSatchelAsync(files.First());
                        break;
                    //If none of the above, just import the files
                    default:
                        await ImportViewModel.ImportFilesAsync(files);
                        break;
                }
            }
        }
    }
}
