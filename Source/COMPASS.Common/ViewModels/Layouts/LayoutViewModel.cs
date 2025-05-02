using Avalonia.Input;
using COMPASS.Common.Models;
using COMPASS.Common.Services;
using COMPASS.Common.ViewModels.Import;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Operations;
using COMPASS.Common.ViewModels.Modals.Import;
using Material.Icons;

namespace COMPASS.Common.ViewModels.Layouts
{
    public abstract class LayoutViewModel : ViewModelBase
    {
        public LayoutViewModel()
        {
            CodexInfoVM = new CodexInfoViewModel();
        }

        // Should put this function separate Factory class for proper factory pattern,
        // but I don't see the point, seems a lot of boilerplate without real advantages
        public static LayoutViewModel GetLayout(CodexLayout? layout = null)
        {
            layout ??= PreferencesService.GetInstance().Preferences.UIState.StartupLayout;
            PreferencesService.GetInstance().Preferences.UIState.StartupLayout = (CodexLayout)layout;
            return layout switch
            {
                CodexLayout.Home => new HomeLayoutViewModel(),
                CodexLayout.List => new ListLayoutViewModel(),
                CodexLayout.Card => new CardLayoutViewModel(),
                CodexLayout.Tile => new TileLayoutViewModel(),
                _ => throw new NotImplementedException(layout.ToString())
            };
        }

        //TODO check if this is still needed
        //public void UpdateDoVirtualization() => OnPropertyChanged(nameof(DoVirtualization));

        #region Properties
        
        public abstract CodexLayout LayoutType { get; }
        public abstract string Name { get; }
        public abstract MaterialIconKind Icon { get; }
        
        public string LongName => $"{Name} Layout";

        //TODO: commands should be in a viewmodel rather than operations
        public CodexOperations CodexCommands { get; init; } = new();
        public CodexInfoViewModel CodexInfoVM { get; }
        
        private Codex? _selectedCodex;
        public Codex? SelectedCodex
        {
            get => _selectedCodex;
            set
            {
                if (SetProperty(ref _selectedCodex, value))
                {
                    CodexInfoVM.DisplayedCodex = _selectedCodex;
                }
            }
        }
        
        private IList<Codex>? _selectedCodices;
        public IList<Codex>? SelectedCodices
        {
            get => _selectedCodices;
            set => SetProperty(ref _selectedCodices, value);
        }

        //TODO check if this is still needed, remove abstract for now so derived classes can skip it
        //public abstract bool DoVirtualization { get; }
        public bool DoVirtualization { get; }
        
            
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
                    ImportFilesViewModel folderImportVM = new(autoImport: false)
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
