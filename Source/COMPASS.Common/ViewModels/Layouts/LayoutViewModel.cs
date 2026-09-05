using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using COMPASS.Common.Adorners;
using COMPASS.Common.DependencyInjection;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Operations;
using COMPASS.Common.ViewModels.Import;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Common.ViewModels.ModelVMs;
using COMPASS.Infra.Avalonia.DragDrop;
using COMPASS.Infra.Avalonia.ExtensionMethods;
using COMPASS.Infra.Models;
using COMPASS.Infra.Tools;

namespace COMPASS.Common.ViewModels.Layouts
{
    public abstract class LayoutViewModel : ViewModelBase, IDisposable
    {
        public LayoutViewModel(CodexInfoViewModelFactory codexInfoVmFactory, CollectionTabVM tabVM)
        {
            _tabViewModel = tabVM;
            CodexInfoVM = codexInfoVmFactory.Create();
            tabVM.CollectionChanging += OnCollectionChanging;
            tabVM.CollectionChanged += OnCollectionChanged;
        }

        protected CollectionTabVM _tabViewModel;

        public RangeObservableCollection<CodexViewModel> FilteredCodexVms => _tabViewModel.FiltersVM.FilteredCodices;


        #region Properties

        public abstract CodexLayout LayoutType { get; }
        
        public CodexInfoViewModel CodexInfoVM { get; }

        public FiltersViewModel FiltersVM => _tabViewModel.FiltersVM;

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


        protected virtual void OnCollectionChanging(object? sender, EventArgs? eventArgs)
        {
            
        }

        protected virtual void OnCollectionChanged(object? sender, EventArgs? eventArgs)
        {
            Dispatcher.UIThread.PostIfNeeded(() => OnPropertyChanged(nameof(FilteredCodexVms)));
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
                        var importFilesVmFactory = ServiceResolver.Resolve<ImportFilesViewModelFactory>();
                        using ImportFilesViewModel folderImportVM = importFilesVmFactory.Create(autoImport: false);
                        folderImportVM.RecursiveDirectories = folders;
                        folderImportVM.Files = files;
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
                                await ServiceResolver.Resolve<CodexCollectionOperations>().ImportFilesAsync(files);
                                break;
                        }
                }
            });

        public void Dispose()
        {
            _tabViewModel.CollectionChanging -= OnCollectionChanging;
            _tabViewModel.CollectionChanged -= OnCollectionChanged;
        }
    }

    [Factory]
    public class LayoutViewModelFactory(CodexInfoViewModelFactory codexInfoVmFactory, IPreferencesService preferencesService)
    {
        public LayoutViewModel Create(CollectionTabVM tabVm, CodexLayout? layout = null)
        {
            layout ??= preferencesService.Preferences.UIState.StartupLayout;
            preferencesService.Preferences.UIState.StartupLayout = (CodexLayout)layout;
            return layout switch
            {
                CodexLayout.Home => new HomeLayoutViewModel(preferencesService.Preferences.HomeLayoutPreferences, codexInfoVmFactory, tabVm),
                CodexLayout.List => new ListLayoutViewModel(preferencesService.Preferences.ListLayoutPreferences, codexInfoVmFactory, tabVm),
                CodexLayout.Card => new CardLayoutViewModel(preferencesService.Preferences.CardLayoutPreferences, codexInfoVmFactory, tabVm),
                CodexLayout.Tile => new TileLayoutViewModel(preferencesService.Preferences.TileLayoutPreferences, codexInfoVmFactory, tabVm),
                _ => throw new NotImplementedException(layout.ToString())
            };
        }
    }
}