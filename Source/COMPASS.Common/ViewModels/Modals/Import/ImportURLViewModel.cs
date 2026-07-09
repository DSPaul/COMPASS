using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Interfaces.ViewModels;
using COMPASS.Common.Models;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.ViewModels.Import;
using COMPASS.Common.ViewModels.Modals.Edit;

namespace COMPASS.Common.ViewModels.Modals.Import
{
    public class ImportURLViewModel : ViewModelBase, IModalViewModel, IConfirmable
    {       
        public ImportURLViewModel(ImportSource importSource)
        {
            switch (importSource)
            {
                case ImportSource.GmBinder:
                    SourceName = "GM Binder";
                    ExampleURL = "https://www.gmbinder.com/share/";
                    break;
                case ImportSource.Homebrewery:
                    SourceName = "Homebrewery";
                    ExampleURL = "https://homebrewery.naturalcrit.com/share/";
                    ShowValidateDisableCheckbox = true;
                    break;
                case ImportSource.GoogleDrive:
                    SourceName = "Google Drive";
                    ExampleURL = "https://drive.google.com/file/";
                    break;
                case ImportSource.GenericURL:
                    SourceName = "Any URL";
                    ExampleURL = "https://";
                    break;
                }
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
            if (!await ConnectivityManager.CheckConnection())
            {
                ImportError = "You need to be connected to the internet to import an online source.";
                return;
            }

            CloseAction();

            List<SourceSet> sourceSets = [new() { SourceURL = InputURL }];
            await ImportViewModel.CreateCodicesAsync(sourceSets);

            if (ShowEditWhenDone)
            {
                Codex? addedCodex = ActiveCollection.AllCodices
                    .OrderByDescending(c => c.DateAdded)
                    .FirstOrDefault(c => c.Sources.SourceURL == InputURL);
                if(addedCodex != null)
                {
                    CodexEditViewModel vm = new(addedCodex);
                    await WindowManager.OpenModal(vm);
                }
                else
                {
                    Logger.Warn($"Could not find codex with source URL '{InputURL}' after import. Cannot open edit modal.");
                }
            }
        }

        
        #endregion
        
        #region IModelViewModel

        public string WindowTitle => $"Import an item from {SourceName}";
        public Action CloseAction { get; set; } = () => { };

        #endregion
    }
}
