using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Interfaces.ViewModels;
using COMPASS.Common.Models;
using COMPASS.Common.Models.CodexProperties;
using COMPASS.Common.Models.Preferences;
using COMPASS.Common.Services;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.ViewModels.Import;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Infra.Tools;
using System.Collections.ObjectModel;

namespace COMPASS.Common.ViewModels.Modals
{
    public class SettingsViewModel : ViewModelBase, IModalViewModel, IDisposable
    {
        private static readonly List<string> TabOrder = ["General", "Import", "Metadata", "Tools", "About", "Legal"];

        public SettingsViewModel(string tabToOpen = "")
        {
            int tabIndex = TabOrder.FindIndex(t => t.Equals(tabToOpen, StringComparison.OrdinalIgnoreCase));
            SelectedTabIndex = tabIndex >= 0 ? tabIndex : 0;

            _applicationDataService = ServiceResolver.Resolve<IApplicationDataService>();
            _ioService = ServiceResolver.Resolve<IIOService>();
            _preferencesService = ServiceResolver.Resolve<IPreferencesService>();
            

            SelectedCollectionVm = CollectionManager.CollectionVms.SingleOrDefault(vm => vm.Identifier == ActiveCollection.Name);

            if (SelectedCollectionVm == null)
            {
                Logger.Warn("The active collection was not found in the list of all known collections");
            }

            if (SelectedCollection != null)
            {
                BanishedPaths = new(SelectedCollection.Info.BanishedPaths.OrderBy(x => x));
                BanishedPaths.CollectionChanged += OnBanishedPathsChanged;
            }
            else
            {
                BanishedPaths = new ObservableCollection<string>();
            }
        }

        private readonly IApplicationDataService _applicationDataService;
        private readonly IIOService _ioService;
        private readonly IPreferencesService _preferencesService;

        private int _selectedTabIndex;
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set => SetProperty(ref _selectedTabIndex, value);
        }

        public static IReadOnlyList<Dependency> Dependencies { get; } =
        [
            new("Autofac",                 "https://autofac.org/",                                     "Inversion of Control",   "MIT"),
            new("Avalonia",                "https://avaloniaui.net/",                                  "UI Framework",           "MIT"),
            new("CommunityToolkit.Mvvm",   "https://github.com/CommunityToolkit/dotnet",               "Reactivity",             "MIT"),
            new("FlashCap",                "https://github.com/kekyo/FlashCap",                        "Video Capture",          "Apache 2.0"),
            new("FuzzySharp",              "https://github.com/JakeBayer/FuzzySharp",                  "Fuzzy Search",           "MIT"),
            new("Host Grotesk",            "https://fonts.google.com/specimen/Host+Grotesk",           "Font",                   "SIL OFL 1.1"),
            new("HotAvalonia",             "https://github.com/Kir-Antipov/HotAvalonia",               "Hot Reload",             "MIT"),
            new("HTML Agility Pack",       "https://github.com/zzzprojects/html-agility-pack",         "HTML Parsing",           "MIT"),
            new("log4net",                 "https://github.com/apache/logging-log4net",                "Logging",                "Apache 2.0"),
            new("Magick.NET",              "https://github.com/dlemstra/Magick.NET",                   "Image Manipulation",     "Apache 2.0"),
            new("Material.Icons.Avalonia", "https://github.com/AvaloniaUtils/Material.Icons.Avalonia", "Icons and Controls",     "MIT"),
            new("OpenCvSharp",             "https://github.com/shimat/opencvsharp",                    "Barcode decoding",       "Apache 2.0"),
            new("PdfPig",                  "https://github.com/UglyToad/PdfPig",                       "PDF Data Extraction",    "Apache 2.0"),
            new("PDFtoImage",              "https://github.com/sungaila/PDFtoImage",                   "PDF to Image",           "MIT"),
            new("Selenium",                "https://github.com/SeleniumHQ/selenium",                   "Website Screenshotting", "Apache 2.0"),
            new("SharpCompress",           "https://github.com/adamhathcock/sharpcompress",            "Zipping and Unzipping",  "MIT"),
            new("Svg.Skia",                "https://github.com/wieslawsoltes/Svg.Skia",                "SVG Rendering",          "MIT"),
        ];
        
        
        private CollectionHandle? _selectedCollectionHandle;

        //TODO show a dropdown in the UI somewhere
        private CodexCollectionVM? _selectedCollectionVm;
        public CodexCollectionVM? SelectedCollectionVm
        {
            get => _selectedCollectionVm;
            set
            {
                _selectedCollectionHandle?.Dispose();
                SetProperty(ref _selectedCollectionVm, value);
                _selectedCollectionHandle = _selectedCollectionVm?.Load();
            }
        }

        private CodexCollection? SelectedCollection =>  SelectedCollectionVm?.Collection;

        #region IModalWindow

        public string WindowTitle => "Settings";
        public Action CloseAction { get; set; } = () => { };

        #endregion
        
        #region Load and Save Settings

        public Preferences Preferences => _preferencesService.Preferences;

        private void ApplyPreferences()
        {
            if (SelectedCollection == null) return;
            //Convert list back to dict because dict does not support two-way binding
            SelectedCollection.Info.FiletypePreferences = FiletypePreferences.ToDictionary(x => x.Key, x => x.Value);

            _preferencesService.SavePreferences();
        }

        public void Refresh()
        {
            if (SelectedCollection == null) return;
            
            //Tell the window that the FiletypePreferences dict might have changed so it needs to fetch it again
            _filetypePreferences = null;
            OnPropertyChanged(nameof(FiletypePreferences));

            SelectedCollection.Info.AutoImportFolders.CollectionChanged += (_, _) => OnPropertyChanged(nameof(AutoImportFolders));
            SelectedCollection.Info.BanishedPaths.CollectionChanged += (_, _) => OnPropertyChanged(nameof(BanishedPaths));
        }
        #endregion

        #region Tab: General

        public bool PreferOnlineSource
        {
            get => _preferencesService.Preferences.OpenCodexPriority.First().Id == Preferences.ONLINE_SOURCE_PRIORITY_ID;
            set
            {
                if (value == PreferOnlineSource) return;
                //If the value changed, we can just switch the order around, because there are only 2 options
                _preferencesService.Preferences.OpenCodexPriority = new(_preferencesService.Preferences.OpenCodexPriority.Reverse());
                OnPropertyChanged();
            }
        }

        #region Manage Data

        #region Data Path 

        public string UserDataPath => _applicationDataService.UserDataPath;
        public string LogsPath => Path.Combine(IApplicationDataService.ApplicationDataPath, Constants.DIR_LOGS);

        public AsyncRelayCommand ChangeDataPathCommand => field ??= new(ChooseNewDataPath);
        private async Task ChooseNewDataPath()
        {
            var folders = await ServiceResolver.Resolve<IFilesService>().OpenFoldersAsync(new()
            {
                Title = "Choose a new data location",
            });

            if (folders.Any())
            {
                var folder = folders.Single();
                string newPath = folder.Path.LocalPath;
                folder.Dispose();
                await _applicationDataService.UpdateUserDataPath(newPath);

                OnPropertyChanged(nameof(UserDataPath));
            }
        }

        #endregion
        public AsyncRelayCommand ResetDataPathCommand => field ??= new(ResetDataPath);
        private async Task ResetDataPath()
        {
            await _applicationDataService.ResetUserDataPath();
            OnPropertyChanged(nameof(UserDataPath));
        }

        public RelayCommand BrowseLocalFilesCommand => field ??= new(BrowseUserFiles);
        public void BrowseUserFiles() => _ioService.ShowInExplorer(_applicationDataService.UserDataPath);

        public RelayCommand OpenLogsFolderCommand => field ??= new(BrowseLogFiles);
        public void BrowseLogFiles() => _ioService.ShowInExplorer(LogsPath);

        #endregion

        #endregion

        #region Tab: Import

        //Open folder in explorer
        public RelayCommand<string> ShowInExplorerCommand => field ??= new(path =>
        {
            if (string.IsNullOrEmpty(path) || !Path.Exists(path)) return;
            _ioService.ShowInExplorer(path);
        });

        #region Auto import folders
        public ObservableCollection<Folder> AutoImportFolders => SelectedCollection != null ? 
            new(SelectedCollection.Info.AutoImportFolders.OrderBy(f => f.FullPath)) :
            [];

        //Edit a folder from auto import
        public AsyncRelayCommand<Folder> EditAutoImportDirectoryCommand => field ??= new(EditAutoImportFolder);
        private async Task EditAutoImportFolder(Folder? folder)
        {
            if (folder is null) return;
            using var importFolderVM = new ImportFilesViewModel(autoImport: false);
            importFolderVM.ExistingFolders = [folder];
            await importFolderVM.Import();
            
            OnPropertyChanged(nameof(AutoImportFolders));
        }
        
        //Remove a folder from auto import;
        public RelayCommand<Folder> RemoveAutoImportDirectoryCommand => field ??= new(RemoveAutoImportDirectory);
        private void RemoveAutoImportDirectory(Folder? folder)
        {
            if (folder == null) return;
            SelectedCollection!.Info.AutoImportFolders.Remove(folder);
            OnPropertyChanged(nameof(AutoImportFolders));
        }

        //Add a directory from auto import
        public AsyncRelayCommand<string> AddAutoImportDirectoryCommand => field ??= new(AddAutoImportDirectory);
        private async Task AddAutoImportDirectory(string? dir)
        {
            if (!String.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
            {
                using var importFolderVM = new ImportFilesViewModel(false);
                importFolderVM.RecursiveDirectories = [dir];
                await importFolderVM.Import();
            }
            await Dispatcher.UIThread.InvokeAsync(() =>
                OnPropertyChanged(nameof(AutoImportFolders)));
        }
        
        public AsyncRelayCommand PickAutoImportDirectoryCommand => field ??= new(PickAutoImportDirectory);

        private async Task PickAutoImportDirectory() => await AddAutoImportDirectory(await _ioService.PickFolder());

        //File types to import
        private List<ObservableKeyValuePair<string, bool>>? _filetypePreferences;
        public List<ObservableKeyValuePair<string, bool>> FiletypePreferences
            => _filetypePreferences ??= SelectedCollection != null ? 
                SelectedCollection.Info.FiletypePreferences
                .Select(x => new ObservableKeyValuePair<string, bool>(x))
                .OrderBy(x => x.Key)
                .ToList() : 
                [];

        public ObservableCollection<string> BanishedPaths { get; }

        public void OnBanishedPathsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            SelectedCollection?.Info.BanishedPaths.ReplaceRange(BanishedPaths);
        }
        
        #endregion

        #region Link Folders to Tag

        public bool AutoLinkFolderTagSameName
        {
            get => _preferencesService.Preferences.AutoLinkFolderTagSameName;
            set
            {
                _preferencesService.Preferences.AutoLinkFolderTagSameName = value;
                OnPropertyChanged();
            }
        }
        #endregion

        #endregion

        #region Tab: Metadata
        public List<CodexProperty> MetaDataPreferences => Preferences.ImportableCodexProperties.OrderBy(x => x.Name).ToList();
        #endregion

        #region Tab: Tools
        public ToolsViewModel ToolsVM { get; } = new();
        #endregion
        
        #region Tab: About
        public string Version => "Version: " + ApplicationService.GetVersion();

        private AsyncRelayCommand? _checkForUpdatesCommand;
        public AsyncRelayCommand CheckForUpdatesCommand => _checkForUpdatesCommand ??= new(UpdateManager.CheckForUpdates);
        #endregion

        #region Tab: Legal
        public static string PrivacyPolicyText { get; } = ReadEmbeddedText("COMPASS.Common.PRIVACY_POLICY.md", "Privacy policy could not be loaded.");
        public static string LicenseText { get; } = ReadEmbeddedText("COMPASS.Common.LICENSE", "License could not be loaded.");

        private static string ReadEmbeddedText(string resourceName, string fallback)
        {
            var assembly = typeof(SettingsViewModel).Assembly;
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null) return fallback;
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
        #endregion

        public void Dispose()
        {
            BanishedPaths.CollectionChanged -= OnBanishedPathsChanged;
            _selectedCollectionHandle?.Dispose();
            ToolsVM.Dispose();
        }
    }
}
