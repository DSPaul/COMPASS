using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Autofac.Features.Indexed;
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

namespace COMPASS.Common.ViewModels.Layouts
{
    public abstract class LayoutViewModel : ViewModelBase, IDisposable
    {
        public LayoutViewModel(
            CodexInfoViewModelFactory codexInfoVmFactory,
            ImportFilesViewModelFactory importFilesVmFactory, 
            CodexCollectionOperations collectionOperations, 
            CollectionTabVM tabVM)
        {
            _tabViewModel = tabVM;
            CodexInfoVM = codexInfoVmFactory.Create();
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
                                await codexCollectionOperations.ImportFilesAsync(files);
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
            CodexLayout resolvedLayout = layout ?? preferencesService.Preferences.UIState.StartupLayout;
            preferencesService.Preferences.UIState.StartupLayout = resolvedLayout;
            
            if (!layoutFactories.TryGetValue(resolvedLayout.ToString(), out LayoutViewModelFactoryBase? layoutFactory))
            {
                throw new NotImplementedException($"No layout factory registered for {resolvedLayout}");
            }
            return layoutFactory.Create(tabVm);
        }
    }
}