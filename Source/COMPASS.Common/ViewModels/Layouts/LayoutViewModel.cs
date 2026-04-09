using Avalonia.Input;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Operations;
using COMPASS.Common.Services;
using COMPASS.Common.ViewModels.Import;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Common.ViewModels.ModelVMs;

namespace COMPASS.Common.ViewModels.Layouts
{
    public abstract class LayoutViewModel : ViewModelBase, IDisposable
    {
        public LayoutViewModel(CollectionTabVM tabVM)
        {
            _tabViewModel = tabVM;
            CodexInfoVM = new();
            FiltersVM = _tabViewModel.FiltersVM;
            tabVM.CollectionChanged += OnCollectionChanged;
        }

        private CollectionTabVM _tabViewModel;

        // Should put this function separate Factory class for proper factory pattern,
        // but I don't see the point, seems a lot of boilerplate without real advantages
        public static LayoutViewModel GetLayout(CollectionTabVM tabVM, CodexLayout? layout = null)
        {
            layout ??= PreferencesService.GetInstance().Preferences.UIState.StartupLayout;
            PreferencesService.GetInstance().Preferences.UIState.StartupLayout = (CodexLayout)layout;
            return layout switch
            {
                CodexLayout.Home => new HomeLayoutViewModel(tabVM),
                CodexLayout.List => new ListLayoutViewModel(tabVM),
                CodexLayout.Card => new CardLayoutViewModel(tabVM),
                CodexLayout.Tile => new TileLayoutViewModel(tabVM),
                _ => throw new NotImplementedException(layout.ToString())
            };
        }

        #region Properties

        public abstract CodexLayout LayoutType { get; }
        
        public CodexInfoViewModel CodexInfoVM { get; }

        public FiltersViewModel FiltersVM
        {
            get;
            set => SetProperty(ref field, value);
        }

        public CodexOperations? CodexCommands => TabsViewModel.GetInstance().ActiveTab?.CodexCommands;

        public CodexViewModel? SelectedCodex
        {
            get;
            set
            {
                if (SetProperty(ref field, value))
                {
                    CodexInfoVM.DisplayedCodex = field;
                }
            }
        }

        public IList<CodexViewModel>? SelectedCodices
        {
            get;
            set => SetProperty(ref field, value);
        }
        
        #endregion

        protected virtual void OnCollectionChanged(object? sender, EventArgs? eventArgs)
        {
            FiltersVM = _tabViewModel.FiltersVM;
        }

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
                    .Select(f => f.Path.LocalPath)
                    .ToList();

                if (paths is null) return;

                var folders = paths.Where(path => File.GetAttributes(path).HasFlag(FileAttributes.Directory)).ToList();
                var files = paths.Where(path => !File.GetAttributes(path).HasFlag(FileAttributes.Directory)).ToList();

                //check for folder import
                if (folders.Count != 0)
                {
                    using ImportFilesViewModel folderImportVM = new(autoImport: false)
                    {
                        RecursiveDirectories = folders,
                        Files = files
                    };
                    await folderImportVM.Import();
                }
                else
                    switch (files.Count)
                    {
                        //If no files or folders, to nothing
                        case 0:
                            return;
                        //Check if it's a satchel file, do import if so
                        case 1 when files.First().EndsWith(Constants.SatchelExtension):
                            if (TabsViewModel.GetInstance().ActiveTab is CollectionTabVM activeTab)
                            {
                                await activeTab.ImportSatchelAsync(files.First());
                            }

                            break;
                        //If none of the above, just import the files
                        default:
                            await ImportViewModel.ImportFilesAsync(files);
                            break;
                    }
            }
        }

        public void Dispose()
        {
            _tabViewModel.CollectionChanged -= OnCollectionChanged;
        }
    }
}