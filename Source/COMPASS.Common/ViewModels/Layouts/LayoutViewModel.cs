using Avalonia.Input;
using Avalonia.Platform.Storage;
using COMPASS.Common.Adorners;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Operations;
using COMPASS.Common.Services;
using COMPASS.Common.ViewModels.Import;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Common.ViewModels.ModelVMs;
using COMPASS.Infra.Models.DragDrop;

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

        public DropManager FileDropManager { get; } = new DropManager()
            .AddHandler(new DropHandler<IStorageItem>(DataFormat.File, DragDropEffects.Copy)
            {
                AdornerFactory = storageItems => new FileDropAdorner(storageItems),
                OnDroppedMultipleAsync = async storageItems =>
                {
                    var paths = storageItems.Select(f => f.Path.LocalPath).ToList();

                    var folders = paths.Where(path => File.GetAttributes(path).HasFlag(FileAttributes.Directory)).ToList();
                    var files = paths.Where(path => !File.GetAttributes(path).HasFlag(FileAttributes.Directory)).ToList();

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
                            case 0:
                                return;
                            case 1 when files.First().EndsWith(Constants.SatchelExtension):
                                if (TabsViewModel.GetInstance().ActiveTab is CollectionTabVM activeTab)
                                    await activeTab.ImportSatchelAsync(files.First());
                                break;
                            default:
                                await ImportViewModel.ImportFilesAsync(files);
                                break;
                        }
                }
            });

        public void Dispose()
        {
            _tabViewModel.CollectionChanged -= OnCollectionChanged;
        }
    }
}