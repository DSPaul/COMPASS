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
            CodexCollectionOperations collectionOperations, 
            CollectionTabVM tabVM)
        {
            _tabViewModel = tabVM;
            CodexInfoVM = codexInfoVmFactory.Create(tabVM);
            tabVM.CollectionChanging += OnCollectionChanging;
            tabVM.CollectionChanged += OnCollectionChanged;

            FileDropManager = CreateFileDropManager(collectionOperations, tabVM);
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

        /// <summary>
        /// Raised when this layout should show codices as selected, for instance right after they were imported.
        /// The view applies it, because the selection of a list or grid is not bindable.
        /// </summary>
        public event EventHandler<CodexSelectionEventArgs>? SelectionRequested;

        /// <summary>
        /// Selects the given codices in this layout, so the user can see what was just imported.
        /// Codices that the active filters hide stay out of the selection, because they are not in the list.
        /// </summary>
        public void SelectCodices(IEnumerable<Codex> codicesToSelect)
        {
            HashSet<Codex> modelsToSelect = [.. codicesToSelect];
            if (modelsToSelect.Count == 0) return;

            //Filtering refreshes the visible list through the dispatcher, so only select once that queue has caught up
            Dispatcher.UIThread.Post(() =>
            {
                List<CodexViewModel> codexVmsToSelect = _tabViewModel.CollectionVM.AllCodexVms
                    .Where(codexVm => modelsToSelect.Contains(codexVm.GetModel()))
                    .ToList();

                if (codexVmsToSelect.Count == 0) return;

                SelectionRequested?.Invoke(this, new CodexSelectionEventArgs(codexVmsToSelect));
            });
        }


        protected virtual void OnCollectionChanging(object? sender, EventArgs? eventArgs)
        {
            
        }

        protected virtual void OnCollectionChanged(object? sender, EventArgs? eventArgs)
        {
            Dispatcher.UIThread.PostIfNeeded(() => OnPropertyChanged(nameof(FilteredCodexVms)));
        }

        public DropManager FileDropManager { get; }
            
        private static DropManager CreateFileDropManager(
            CodexCollectionOperations codexCollectionOperations,
            CollectionTabVM tabVm) => new DropManager()
            .AddHandler(new DropHandler<IStorageItem>(DataFormat.File, DragDropEffects.Copy)
            {
                AdornerFactory = storageItems => new FileDropAdorner(storageItems),
                OnDroppedMultipleAsync = async storageItems 
                    => await codexCollectionOperations.ImportStorageItemsAsync(storageItems, tabVm.CollectionVM.Identifier)
            });

        /// <summary>
        /// Raised when the layout is disposed, so views that were built for it can be released.
        /// </summary>
        public event EventHandler? Disposed;

        public void Dispose()
        {
            _tabViewModel.CollectionChanging -= OnCollectionChanging;
            _tabViewModel.CollectionChanged -= OnCollectionChanged;
            Disposed?.Invoke(this, EventArgs.Empty);
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
