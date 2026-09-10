using CommunityToolkit.Mvvm.Input;
using Autofac.Features.Indexed;
using COMPASS.Common.DependencyInjection;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.ViewModels;
using COMPASS.Common.Operations;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.Sources;
using COMPASS.Common.ViewModels.Import;
using COMPASS.Common.ViewModels.Modals.Edit;

namespace COMPASS.Common.ViewModels.Modals.Import
{
    public class ImportURLViewModel : ViewModelBase, IModalViewModel, IConfirmable
    {       
        private readonly CodexEditViewModelFactory _codexEditViewModelFactory;
        private readonly CodexCollectionOperations _codexCollectionOperations;
        private readonly ConnectivityManager _connectivityManager;
        private readonly IIndex<MetaDataSourceType, MetaDataSource> _metaDataSources;
        private readonly ILogger _logger;

        public ImportURLViewModel(ILogger logger, CodexEditViewModelFactory codexEditViewModelFactory, CodexCollectionOperations codexCollectionOperations, ConnectivityManager connectivityManager, IIndex<MetaDataSourceType, MetaDataSource> metaDataSources, ImportSource importSource)
        {
            _logger = logger;
            _codexEditViewModelFactory = codexEditViewModelFactory;
            _codexCollectionOperations = codexCollectionOperations;
            _connectivityManager = connectivityManager;
            _metaDataSources = metaDataSources;

            //TODO: Name should be a property of the metadatasource itself
            SourceName = importSource switch
            {
                ImportSource.GmBinder => "GM Binder",
                ImportSource.Homebrewery => "Homebrewery",
                ImportSource.GoogleDrive => "Google Drive",
                _ => "Any URL"
            };

            //TODO importSource should probably be simpified to categories File, Folder, URL, ISBN, etc
            //with optional metadatasource to narrow it down, rather than these almost identical enums
            var associatedMetadataSource = importSource switch
            {
                ImportSource.GmBinder => MetaDataSourceType.GmBinder,
                ImportSource.Homebrewery => MetaDataSourceType.Homebrewery,
                ImportSource.GoogleDrive => MetaDataSourceType.GoogleDrive,
                _ => MetaDataSourceType.GenericURL
            };

            var metadataSource = _metaDataSources.TryGetValue(associatedMetadataSource, out MetaDataSource? source)
                ? source as OnlineMetaDataSource
                : null;

            ExampleURL = metadataSource?.UrlPrefix ?? "";
            ShowValidateDisableCheckbox = importSource == ImportSource.Homebrewery;
        }

        //configuration props
        public string SourceName { get; } = "";
        public string ExampleURL { get; init; } = "";
        public bool ShowValidateDisableCheckbox { get; init; } = false;

        //State props
        public bool ValidateURL { get; set; } = true;
        public bool ShowEditWhenDone { get; set; } = false;

        public string InputURL
        {
            get;
            set
            {
                SetProperty(ref field, value);
                Validate(nameof(InputURL));
            }
        } = "";

        private string _importError = "";
        public string ImportError
        {
            get => _importError;
            set => SetProperty(ref _importError, value);
        }

        #region IConfirmable
        
        private RelayCommand? _cancelCommand;
        public IRelayCommand CancelCommand => _cancelCommand ??= new(CloseAction);
        
        private AsyncRelayCommand? _submitUrlCommand;
        public IRelayCommand ConfirmCommand => _submitUrlCommand ??= new(SubmitURL, () => !HasErrors);
        private async Task SubmitURL()
        {
            if (!InputURL.Contains(ExampleURL) && ValidateURL)
            {
                ImportError = $"'{InputURL}' is not a valid URL for {SourceName}";
                return;
            }
            if (!await _connectivityManager.CheckConnection())
            {
                ImportError = "You need to be connected to the internet to import an online source.";
                return;
            }

            CloseAction();

            List<SourceSet> sourceSets = [new() { SourceURL = InputURL }];
            await _codexCollectionOperations.CreateCodicesAsync(sourceSets);

            if (ShowEditWhenDone)
            {
                Codex? addedCodex = ActiveCollection.AllCodices
                    .OrderByDescending(c => c.DateAdded)
                    .FirstOrDefault(c => c.Sources.SourceURL == InputURL);
                if(addedCodex != null)
                {
                    CodexEditViewModel vm = _codexEditViewModelFactory.Create(addedCodex);
                    await WindowManager.OpenModal(vm);
                }
                else
                {
                    _logger.Warn($"Could not find codex with source URL '{InputURL}' after import. Cannot open edit modal.");
                }
            }
        }

        
        #endregion
        
        #region IModelViewModel

        public string WindowTitle => $"Import an item from {SourceName}";
        public Action CloseAction { get; set; } = () => { };

        #endregion
    }

    [Factory]
    public class ImportURLViewModelFactory(ILogger logger, CodexEditViewModelFactory codexEditViewModelFactory, CodexCollectionOperations codexCollectionOperations, ConnectivityManager connectivityManager, IIndex<MetaDataSourceType, MetaDataSource> metaDataSources)
    {
        public ImportURLViewModel Create(ImportSource importSource)
            => new(logger, codexEditViewModelFactory, codexCollectionOperations, connectivityManager, metaDataSources, importSource);
    }
}
