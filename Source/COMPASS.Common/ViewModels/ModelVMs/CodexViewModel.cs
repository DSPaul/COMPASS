using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Models;
using COMPASS.Common.Operations;
using COMPASS.Common.Services.FileSystem;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Infra.ExtensionMethods;
using COMPASS.Infra.Models;

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
        _derivedProperties.Add(nameof(Codex.ThumbnailPath), [nameof(Thumbnail)]);
        
        //Validation
        AddValidation(nameof(PageCount), ValidatePageCount);
    }

    #endregion

    #region Properties

    #region COMPASS related Metadata

    public int Id => _model.Id;
    
    private Bitmap? _thumbnail;
    public Task<Bitmap?> Thumbnail => _thumbnail == null ? LoadThumbnail() : Task.FromResult<Bitmap?>(_thumbnail);

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
    public string UserDefinedSortingTitle => _model.UserDefinedSortingTitle;
    public string SortingTitle
    {
        get => (string.IsNullOrEmpty(_model.UserDefinedSortingTitle) ? _model.Title : _model.UserDefinedSortingTitle).PadNumbers();
        set => _model.SortingTitle = value;
    }
    public bool SortingTitleContainsNumbers => RegexConstants.NumbersOnly().IsMatch(SortingTitle);
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
    public DateTime DateAdded
    {
        get => _model.DateAdded;
        set => _model.DateAdded = value;
    }
    
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

    private async Task<Bitmap?> LoadThumbnail()
    {
        try
        {
            if (File.Exists(_model.ThumbnailPath))
            {
                return await Task.Run(() => _thumbnail = new Bitmap(_model.ThumbnailPath));
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

    //Open codex Offline
    private AsyncRelayCommand? _openCodexLocallyCommand;
    public AsyncRelayCommand OpenCodexLocallyCommand => _openCodexLocallyCommand ??= new(OpenCodexLocally, CanOpenCodexLocally);
    private async Task<bool> OpenCodexLocally() => await CodexOperations.OpenCodexLocally(_model);
    private bool CanOpenCodexLocally() => CodexOperations.CanOpenCodexLocally(_model);

    //Open codex Online
    private RelayCommand? _openCodexOnlineCommand;
    public RelayCommand OpenCodexOnlineCommand => _openCodexOnlineCommand ??= new(OpenCodexOnline, CanOpenCodexOnline);
    private void OpenCodexOnline() => CodexOperations.OpenCodexOnline(_model);
    private bool CanOpenCodexOnline() => CodexOperations.CanOpenCodexOnline(_model);
    

    //Edit File
    private AsyncRelayCommand? _editCodexCommand;
    public AsyncRelayCommand EditCodexCommand => _editCodexCommand ??= new(EditCodex);
    private async Task EditCodex() => await CodexOperations.EditCodex(_model);
    

    //Toggle Favorite
    private RelayCommand? _favoriteCodexCommand;
    public RelayCommand FavoriteCodexCommand => _favoriteCodexCommand ??= new(FavoriteCodex);
    private void FavoriteCodex() => CodexOperations.FavoriteCodex(_model);

    //Show in Explorer
    private RelayCommand? _showInExplorerCommand;
    public RelayCommand ShowInExplorerCommand => _showInExplorerCommand ??= new(ShowInExplorer, CanOpenCodexLocally);
    private void ShowInExplorer() => CodexOperations.ShowInExplorer(_model);
    
    //Move Codex to other CodexCollection
    private AsyncRelayCommand<string>? _moveToCollectionCommand;
    public AsyncRelayCommand<string> MoveToCollectionCommand => _moveToCollectionCommand ??= new(MoveToCollection);
    private async Task MoveToCollection(string? targetCollectionIdentifier)
    {
        if (string.IsNullOrEmpty(targetCollectionIdentifier)) return;
        await CodexOperations.MoveToCollection(targetCollectionIdentifier, [_model]);
    }
    
    //Delete Codex
    private AsyncRelayCommand? _deleteCodexCommand;
    public AsyncRelayCommand DeleteCodexCommand => _deleteCodexCommand ??= new(DeleteCodex);
    private async Task DeleteCodex() => await CodexOperations.DeleteCodex(_model);
    

    //Banish Codex
    private AsyncRelayCommand? _banishCodexCommand;
    public AsyncRelayCommand BanishCodexCommand => _banishCodexCommand ??= new(BanishCodex);
    private async Task BanishCodex() => await CodexOperations.BanishCodex(_model);

    //Get Metadata
    private AsyncRelayCommand? _getMetaDataCommand;
    public AsyncRelayCommand GetMetaDataCommand => _getMetaDataCommand ??= new(StartGetMetaDataProcess);
    private async Task StartGetMetaDataProcess() => await CodexOperations.StartGetMetaDataProcess(_model);
    
    //Get Cover
    private AsyncRelayCommand? _getCoverCommand;
    public AsyncRelayCommand GetCoverCommand => _getCoverCommand ??= new(GetCover);
    private async Task GetCover() => await CodexOperations.GetCover(_model);
        
        

    #endregion
}