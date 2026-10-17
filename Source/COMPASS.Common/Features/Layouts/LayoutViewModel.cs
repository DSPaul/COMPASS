using Autofac.Features.Indexed;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using COMPASS.Common.Features.CodexCollections;
using COMPASS.Common.Features.Codices;
using COMPASS.Common.Features.Filtering;
using COMPASS.Common.Features.ImportExport;
using COMPASS.Common.Features.Settings;
using COMPASS.Infra.Avalonia.DragDrop;
using COMPASS.Infra.Avalonia.Threading;
using COMPASS.Infra.Collections;
using COMPASS.Infra.DependencyInjection;
using COMPASS.Infra.Preferences;
using COMPASS.Common.Domain;
using COMPASS.Infra.Avalonia.Validation;
using COMPASS.Common.Features.Tabs;

namespace COMPASS.Common.Features.Layouts
{
    public abstract class LayoutViewModel : ValidatableViewModelBase, IDisposable
    {
        public LayoutViewModel(
            CodexInfoViewModelFactory codexInfoVmFactory,
            ImportFilesViewModelFactory importFilesVmFactory, 
            CodexCollectionOperations collectionOperations, 
            CollectionTabVM tabVM)
        {
            _tabViewModel = tabVM;
            CodexInfoVM = codexInfoVmFactory.Create(tabVM);
            tabVM.CollectionChanging += OnCollectionChanging;
            tabVM.CollectionChanged += OnCollectionChanged;

            FileDropManager = CreateFileDropManager(importFilesVmFactory, collectionOperations, tabVM);
        }

        protected CollectionTabVM _tabViewModel;

        public RangeObservableCollection<CodexViewModel> FilteredCodexVms => _tabViewModel.FiltersVM.FilteredCodices;


        #region Properties

        public abstract CodexLayout LayoutType { get; }
        
        public CodexInfoViewModel CodexInfoVM { get; }

        public FiltersViewModel FiltersVM => _tabViewModel.FiltersVM;

        public CodexOperations CodexCommands => _tabViewModel.CodexCommands;

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

        public DropManager FileDropManager { get; }
            
        private DropManager CreateFileDropManager(
            ImportFilesViewModelFactory importFilesVmFactory, 
            CodexCollectionOperations codexCollectionOperations,
            CollectionTabVM tabVm) => new DropManager()
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
                        using ImportFilesViewModel folderImportVM = importFilesVmFactory.Create(autoImport: false);
                        folderImportVM.RecursiveDirectories = folders;
                        folderImportVM.Files = files;
                        await folderImportVM.Import();
                    }
                    else
                    {
                        switch (files.Count)
                        {
                            case 0:
                                return;
                            case 1 when files.First().EndsWith(Constants.SatchelExtension):
                                await tabVm.ImportSatchelAsync(files.First());
                                break;
                            default:
                                await codexCollectionOperations.ImportFilesAsync(files, _tabViewModel.CollectionVM.Identifier);
                                break;
                        }
                    }
                }
            });

        public void Dispose()
        {
            _tabViewModel.CollectionChanging -= OnCollectionChanging;
            _tabViewModel.CollectionChanged -= OnCollectionChanged;
        }
    }

    /// <summary>
    /// Base for per-layout factories.
    /// </summary>
    public abstract class LayoutViewModelFactoryBase
    {
        public abstract LayoutViewModel Create(CollectionTabVM tabVm);
    }

    [Factory]
    public class LayoutViewModelFactory(
        IIndex<string, LayoutViewModelFactoryBase> layoutFactories,
        IPreferencesService preferencesService)
    {
        public LayoutViewModel Create(CollectionTabVM tabVm, CodexLayout? layout = null)
        {
            var UIstate = preferencesService.GetPreferences<UIState>();
            CodexLayout resolvedLayout = layout ?? UIstate.StartupLayout;
            UIstate.StartupLayout = resolvedLayout;
            
            if (!layoutFactories.TryGetValue(resolvedLayout.ToString(), out LayoutViewModelFactoryBase? layoutFactory))
            {
                throw new NotImplementedException($"No layout factory registered for {resolvedLayout}");
            }
            return layoutFactory.Create(tabVm);
        }
    }
}
