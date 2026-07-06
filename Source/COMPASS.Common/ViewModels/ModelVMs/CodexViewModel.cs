using Avalonia.Input;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Adorners;
using COMPASS.Common.Models;
using COMPASS.Common.Models.DragDrop;
using COMPASS.Common.Operations;
using COMPASS.Common.Services;
using COMPASS.Common.Services.FileSystem;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Infra.Avalonia.DragDrop;
using COMPASS.Infra.ExtensionMethods;
using COMPASS.Infra.Models;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace COMPASS.Common.ViewModels.ModelVMs;

public class CodexViewModel : ModelViewModelBase<Codex>
{
    private readonly CodexCollectionVM _codexCollectionVM;
    
    #region Constructors

    public CodexViewModel(Codex codex, CodexCollectionVM codexCollectionVM) : base(codex)
    {
        _codexCollectionVM = codexCollectionVM;

        Tags = new ReadOnlyCollection<TagViewModel>(GetTagVms());
        
        codex.Authors.CollectionChanged += OnCollectionChanged;
        codex.Tags.CollectionChanged += OnCollectionChanged;
        codex.CoverChanged += CodexOnCoverChanged;
        
        //Derived properties
        _derivedProperties.Add(nameof(Codex.Title), [nameof(SortingTitle), nameof(SortingTitleContainsNumbers)]);
        _derivedProperties.Add(nameof(Codex.UserDefinedSortingTitle), [nameof(SortingTitle), nameof(SortingTitleContainsNumbers)]);
        _derivedProperties.Add(nameof(Codex.Authors), [nameof(AuthorsAsString)]);
        _derivedProperties.Add(nameof(Codex.Tags), [nameof(OrderedTags)]);
        _derivedProperties.Add(nameof(Codex.ReleaseDate), [nameof(ReleaseDateAsString)]);
        
        //Validation
        AddValidation(nameof(PageCount), ValidatePageCount);

        DropManager = CreateDropManager();
    }

    #endregion

    #region Properties

    #region COMPASS related Metadata

    public DropManager DropManager { get; }

    public int Id => _model.Id;
    public Guid GlobalId => _model.GlobalId;
    
    private Bitmap? _thumbnail;
    public Task<Bitmap?> Thumbnail => _thumbnail == null ? LoadOrCreateThumbnail() : Task.FromResult<Bitmap?>(_thumbnail);

    private Bitmap? _cover;
    public Bitmap? Cover
    {
        get => _cover; 
        set => SetProperty(ref _cover, value);
    }

    #endregion

    #region Codex related Metadata
    
    public string Title
    {
        get => _model.Title;
        set => _model.Title = value;
    }

    /// <summary>
    /// Sorting title defined by the user, will only have a value if it is different from the title
    /// </summary>
    public string SortingTitle
    {
        get => (string.IsNullOrEmpty(_model.UserDefinedSortingTitle) ? _model.Title : _model.UserDefinedSortingTitle).PadNumbers();
        set => _model.SortingTitle = value;
    }
    public bool SortingTitleContainsNumbers => RegexConstants.Numbers().IsMatch(SortingTitle);
    public string ZeroPaddingExplainer =>
        "What's with all the 0's? \n \n" +
        "Zero-padding numbers ensures numerical sorting instead of alphabetical sorting. \n" +
        "Consider the numbers 1, 2, 13, and 20. \n" +
        "Without zero-padding, they would be sorted alphabetically as 1, 13, 2, 20. \n" +
        "However, with zero-padding, the order becomes 01, 02, 13, 20. \n";

    public ObservableCollection<string> Authors => _model.Authors;
    public string AuthorsAsString
    {
        get
        {
            string str = Authors.Count switch
            {
                1 => Authors[0],
                > 1 => String.Join(", ", Authors.OrderBy(a => a)),
                _ => ""
            };
            return str;
        }
    }
    
    public string Publisher
    {
        get => _model.Publisher;
        set => _model.Publisher = value;
    }
    
    public string Description
    {
        get => _model.Description;
        set => _model.Description = value;
    }
    
    public DateTime? ReleaseDate
    {
        get => _model.ReleaseDate;
        set => _model.ReleaseDate = value;
    }

    public string ReleaseDateAsString => ReleaseDate == null ? "" : ReleaseDate.Value.ToString("dd/MM/yyyy");
    
    public int PageCount
    {
        get => _model.PageCount;
        set=> _model.PageCount = value;
    }
    
    public string Version
    {
        get => _model.Version;
        set => _model.Version = value;
    }

    #endregion

    #region User related Metadata
    
    public IReadOnlyCollection<TagViewModel> Tags { get; private set; }

    //order them in same order as allTags by starting with allTags and keeping the ones we need using intersect
    public IEnumerable<TagViewModel> OrderedTags => _model.Collection.RootTags.Flatten()
                                                                              .Intersect(_model.Tags)
                                                                              .Select(_codexCollectionVM.GetTagVm);
    
    public bool PhysicallyOwned
    {
        get => _model.PhysicallyOwned;
        set => _model.PhysicallyOwned = value;
    }
    
    public int Rating
    {
        get => _model.Rating;
        set => _model.Rating = value;
    }
    
    public bool Favorite
    {
        get => _model.Favorite;
        set => _model.Favorite = value;
    }

    #endregion

    #region User behaviour metadata
    public DateTime DateAdded => _model.DateAdded;

    public DateTime LastOpened
    {
        get => _model.LastOpened;
        set => _model.LastOpened = value;
    }
    
    public int OpenedCount
    {
        get => _model.OpenedCount;
        set => _model.OpenedCount = value;
    }

    #endregion
    
    public SourceSet Sources => _model.Sources;
    #endregion

    #region Validation
    
    private void ValidatePageCount()
    {
        if (PageCount < 0)
        {
            AddError(nameof(PageCount), "Pagecount must be a positive number.");
        }
    }
    
    #endregion
    
    #region Methods

    public void LoadCover()
    {
        try
        {
            Cover = File.Exists(_model.CoverArtPath) ? 
                new(_model.CoverArtPath) : 
                AssetsService.GetPlaceholder(_model);
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to load thumbnail", ex);
        }
    }

    private async Task<Bitmap?> LoadOrCreateThumbnail()
    {
        try
        {
            // Invalidate stale thumbnail: if the cover art was updated after the thumbnail was generated, delete it
            if (File.Exists(_model.ThumbnailPath) && File.Exists(_model.CoverArtPath) &&
                File.GetLastWriteTimeUtc(_model.CoverArtPath) > File.GetLastWriteTimeUtc(_model.ThumbnailPath))
            {
                File.Delete(_model.ThumbnailPath);
            }

            if (File.Exists(_model.ThumbnailPath))
            {
                return await Task.Run(() => _thumbnail = new Bitmap(_model.ThumbnailPath));
            }
            else if(File.Exists(_model.CoverArtPath))
            {
                return await Task.Run(() =>
                {
                    using var thumbnail = CoverService.CreateThumbnail(_model);
                    if(thumbnail == null)
                    {
                        return null;
                    }
                    using MemoryStream ms = new();
                    thumbnail?.Write(ms);
                    ms.Position = 0;
                    return _thumbnail = new Bitmap(ms);
                });
            }
            else
            {
                return _thumbnail = AssetsService.GetPlaceholder(_model);
            }
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to load thumbnail", ex); 
            return null;
        }
    }

    private IList<TagViewModel> GetTagVms() => _model.Tags.Select(_codexCollectionVM.GetTagVm).ToList();
    
    private DropManager CreateDropManager() => new DropManager()
        .AddHandler(new DropHandler<Tag>(DataTransferFormats.TagFormat, DragDropEffects.Link)
        {
            OnDroppedSingle = tag => _model.Tags.AddIfMissing(tag),
            CanDrop = tag => !tag.IsGroup,
            AdornerFactory = tags => new DropTagAdorner(tags[0]) { Format = "Assign tag {0}" },
        });

    private void OnCollectionChanged(object? o, NotifyCollectionChangedEventArgs args)
    {
        if (o == _model.Tags)
        {
            Tags = new ReadOnlyCollection<TagViewModel>(GetTagVms());
            
            HandlePropertyChanged(nameof(Tags));
        }
        else if (o == _model.Authors)
        {
            HandlePropertyChanged(nameof(Authors));
        }
    }
    
    private void CodexOnCoverChanged(object? sender, EventArgs e)
    {
        DisposeThumbnail();
        OnPropertyChanged(nameof(Thumbnail));

        //only reload cover is it was already loaded
        if (Cover != null)
        {
            DisposeCover();
            LoadCover();
        }
    }
    
    public override void Dispose()
    {
        DisposeThumbnail();
        DisposeCover();
        
        _model.Authors.CollectionChanged -= OnCollectionChanged;
        _model.Tags.CollectionChanged -= OnCollectionChanged;
        _model.CoverChanged -= CodexOnCoverChanged;
        
        base.Dispose();
    }

    private void DisposeThumbnail()
    {
        if (_thumbnail != null && !AssetsService.IsSharedAsset(_thumbnail))
        {
            _thumbnail.Dispose();
        }
        _thumbnail = null;
    }
    
    public void DisposeCover()
    {
        if (_cover != null && !AssetsService.IsSharedAsset(_cover))
        {
            _cover.Dispose();
        }
        _cover = null;
    }
    
    #endregion

    #region Commands

    //Open codex
    public AsyncRelayCommand OpenCodexCommand => field ??= new(OpenCodex, CanOpenCodex);
    private async Task<bool> OpenCodex() => await CodexOperations.OpenCodex(_model);
    private bool CanOpenCodex() => CodexOperations.CanOpenCodex(_model);
    
    //Open codex Offline
    public AsyncRelayCommand OpenCodexLocallyCommand => field ??= new(OpenCodexLocally, CanOpenCodexLocally);
    private async Task<bool> OpenCodexLocally() => await CodexOperations.OpenCodexLocally(_model);
    private bool CanOpenCodexLocally() => CodexOperations.CanOpenCodexLocally(_model);

    //Open codex Online
    public RelayCommand OpenCodexOnlineCommand => field ??= new(OpenCodexOnline, CanOpenCodexOnline);
    private void OpenCodexOnline() => CodexOperations.OpenCodexOnline(_model);
    private bool CanOpenCodexOnline() => CodexOperations.CanOpenCodexOnline(_model);
    
    //Edit File
    public AsyncRelayCommand EditCodexCommand => field ??= new(EditCodex);
    private async Task EditCodex() => await CodexOperations.EditCodex(_model);
    
    //Toggle Favorite
    public RelayCommand FavoriteCodexCommand => field ??= new(FavoriteCodex);
    private void FavoriteCodex() => CodexOperations.FavoriteCodex(_model);

    //Show in Explorer
    public RelayCommand ShowInExplorerCommand => field ??= new(ShowInExplorer, CanOpenCodexLocally);
    private void ShowInExplorer() => CodexOperations.ShowInExplorer(_model);
    
    //Move Codex to other CodexCollection
    public AsyncRelayCommand<string> MoveToCollectionCommand => field ??= new(MoveToCollection);
    private async Task MoveToCollection(string? targetCollectionIdentifier)
    {
        if (string.IsNullOrEmpty(targetCollectionIdentifier)) return;
        await CodexOperations.MoveToCollection(targetCollectionIdentifier, [_model]);
    }
    
    //Delete Codex
    public AsyncRelayCommand DeleteCodexCommand => field ??= new(DeleteCodex);
    private async Task DeleteCodex() => await CodexOperations.DeleteCodex(_model);
    

    //Banish Codex
    public AsyncRelayCommand BanishCodexCommand => field ??= new(BanishCodex);
    private async Task BanishCodex() => await CodexOperations.BanishCodex(_model);

    //Get Metadata
    public AsyncRelayCommand GetMetaDataCommand => field ??= new(StartGetMetaDataProcess);
    private async Task StartGetMetaDataProcess() => await CodexOperations.StartGetMetaDataProcess(_model);
    
    //Get Cover
    public AsyncRelayCommand GetCoverCommand => field ??= new(GetCover);
    private async Task GetCover() => await CodexOperations.GetCover(_model);
    
    #endregion
}