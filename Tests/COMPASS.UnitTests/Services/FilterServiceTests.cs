using Autofac;
using Avalonia.Platform.Storage;
using COMPASS.Common.Interfaces.Repos;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Filters;
using COMPASS.Common.Repositories;
using COMPASS.Common.Services;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Common.ViewModels.ModelVMs;
using COMPASS.Infra.Models;
using COMPASS.Infra.Tools;
using COMPASS.Tests.Common;
using System.ComponentModel;

namespace COMPASS.UnitTests.Services;

[TestFixture]
public class FilterServiceTests
{
    private FilterService _filterService = null!;
    private CodexCollectionVM _collectionVm = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        var builder = new ContainerBuilder();
        builder.RegisterModule<MockModule>();
        builder.RegisterType<StubImportExportService>().As<IImportExportService>();
        ServiceResolver.Initialize(builder.Build());
    }

    [SetUp]
    public void SetUp()
    {
        _filterService = new FilterService();
        _collectionVm = BuildTestCollection();
    }

    [TearDown]
    public void TearDown()
    {
        _collectionVm.Dispose();
    }

    #region FilterCodices

    [Test]
    public void FilterCodices_NoFilters_ReturnsAllCodices()
    {
        IList<CodexViewModel> result = Filter();

        Assert.That(Titles(result), Is.EqualTo(new[] { "Alpha", "Beta", "Delta", "Epsilon", "Eta", "Gamma", "Zeta" }));
    }

    [Test]
    public void FilterCodices_TagFilter_IncludesCodicesWithTagOrDescendants()
    {
        IList<CodexViewModel> result = Filter(included: [CreateTagFilter("Wizards")]);

        Assert.That(Titles(result), Is.EqualTo(new[] { "Alpha", "Beta", "Eta" }));
    }

    [Test]
    public void FilterCodices_TagFilterOnChild_AlsoMatchesCodicesWithParentTag()
    {
        IList<CodexViewModel> result = Filter(included: [CreateTagFilter("Eberron")]);

        Assert.That(Titles(result), Is.EqualTo(new[] { "Alpha", "Beta", "Eta" }));
    }

    [Test]
    public void FilterCodices_TagsWithinSameGroup_CombinedWithOr()
    {
        IList<CodexViewModel> result = Filter(included: [CreateTagFilter("Wizards"), CreateTagFilter("Paizo")]);

        Assert.That(Titles(result), Is.EqualTo(new[] { "Alpha", "Beta", "Eta", "Gamma" }));
    }

    [Test]
    public void FilterCodices_TagsInDifferentGroups_CombinedWithAnd()
    {
        IList<CodexViewModel> result = Filter(included: [CreateTagFilter("Wizards"), CreateTagFilter("StarWars")]);

        Assert.That(Titles(result), Is.EqualTo(new[] { "Eta" }));
    }

    [Test]
    public void FilterCodices_ExcludedTag_RemovesCodicesWithTagOrDescendants()
    {
        IList<CodexViewModel> result = Filter(excluded: [CreateTagFilter("Wizards")]);

        Assert.That(Titles(result), Is.EqualTo(new[] { "Delta", "Epsilon", "Gamma", "Zeta" }));
    }

    [Test]
    public void FilterCodices_PublisherFilter_OnlyReturnsMatchingPublisher()
    {
        IList<CodexViewModel> result = Filter(included: [new PublisherFilter("WotC")]);

        Assert.That(Titles(result), Is.EqualTo(new[] { "Alpha", "Beta", "Eta" }));
    }

    [Test]
    public void FilterCodices_MultipleFiltersOfSameType_CombinedWithOr()
    {
        IList<CodexViewModel> result = Filter(included: [new PublisherFilter("WotC"), new PublisherFilter("Grimm")]);

        Assert.That(Titles(result), Is.EqualTo(new[] { "Alpha", "Beta", "Epsilon", "Eta" }));
    }

    [Test]
    public void FilterCodices_MultipleFiltersOfDifferentTypes_CombinedWithAnd()
    {
        IList<CodexViewModel> result = Filter(included: [new PublisherFilter("WotC"), new OfflineSourceFilter()]);

        Assert.That(Titles(result), Is.EqualTo(new[] { "Beta" }));
    }

    [Test]
    public void FilterCodices_ExcludedFilter_RemovesMatchingCodices()
    {
        IList<CodexViewModel> result = Filter(excluded: [new OfflineSourceFilter()]);

        Assert.That(Titles(result), Is.EqualTo(new[] { "Alpha", "Delta", "Epsilon", "Eta", "Zeta" }));
    }

    [Test]
    public void FilterCodices_IncludedAndExcludedCombined_ReturnsOnlyIncludedNotExcluded()
    {
        IList<CodexViewModel> result = Filter(included: [new PublisherFilter("WotC")], excluded: [new OfflineSourceFilter()]);

        Assert.That(Titles(result), Is.EqualTo(new[] { "Alpha", "Eta" }));
    }

    [Test]
    public void FilterCodices_AuthorFilter_ReturnsCodicesWithAuthor()
    {
        IList<CodexViewModel> result = Filter(included: [new AuthorFilter("Brandon")]);

        Assert.That(Titles(result), Is.EqualTo(new[] { "Beta", "Delta" }));
    }

    [Test]
    public void FilterCodices_MinimumRatingFilter_ReturnsCodicesAtOrAboveRating()
    {
        IList<CodexViewModel> result = Filter(included: [new MinimumRatingFilter(4)]);

        Assert.That(Titles(result), Is.EqualTo(new[] { "Epsilon", "Zeta" }));
    }

    [Test]
    public void FilterCodices_FavoriteFilter_ReturnsOnlyFavorites()
    {
        IList<CodexViewModel> result = Filter(included: [new FavoriteFilter()]);

        Assert.That(Titles(result), Is.EqualTo(new[] { "Epsilon" }));
    }

    #endregion

    #region SortCodices

    [Test]
    public void SortCodices_AscendingByTitle_SortsInPlace()
    {
        List<CodexViewModel> codices = _collectionVm.AllCodexVms.ToList();

        _filterService.SortCodices(codices, nameof(CodexViewModel.Title), ListSortDirection.Ascending);

        Assert.That(codices.Select(vm => vm.Title), Is.EqualTo(new[] { "Alpha", "Beta", "Delta", "Epsilon", "Eta", "Gamma", "Zeta" }));
    }

    [Test]
    public void SortCodices_DescendingByTitle_SortsInPlace()
    {
        List<CodexViewModel> codices = _collectionVm.AllCodexVms.ToList();

        _filterService.SortCodices(codices, nameof(CodexViewModel.Title), ListSortDirection.Descending);

        Assert.That(codices.Select(vm => vm.Title), Is.EqualTo(new[] { "Zeta", "Gamma", "Eta", "Epsilon", "Delta", "Beta", "Alpha" }));
    }

    [Test]
    public void SortCodices_AscendingByRating_SortsInPlace()
    {
        List<CodexViewModel> codices = _collectionVm.AllCodexVms.ToList();

        _filterService.SortCodices(codices, nameof(CodexViewModel.Rating), ListSortDirection.Ascending);

        Assert.That(codices.Select(vm => vm.Rating), Is.EqualTo(new[] { 1, 1, 2, 2, 3, 4, 5 }));
    }

    [Test]
    public void SortCodices_WithRangeObservableCollection_SortsInPlace()
    {
        RangeObservableCollection<CodexViewModel> codices = new(_collectionVm.AllCodexVms);

        _filterService.SortCodices(codices, nameof(CodexViewModel.Title), ListSortDirection.Descending);

        Assert.That(codices.Select(vm => vm.Title), Is.EqualTo(new[] { "Zeta", "Gamma", "Eta", "Epsilon", "Delta", "Beta", "Alpha" }));
    }

    [Test]
    public void SortCodices_InvalidSortDirection_Throws()
    {
        List<CodexViewModel> codices = _collectionVm.AllCodexVms.ToList();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _filterService.SortCodices(codices, nameof(CodexViewModel.Title), (ListSortDirection)42));
    }

    #endregion

    #region Test Data

    private CodexCollectionVM BuildTestCollection()
    {
        // Tag tree:
        // Fantasy (group)
        // ├── Wizards
        // │   └── Eberron
        // ├── Paizo
        // └── DnD
        // SciFi (group)
        // ├── StarWars
        // └── Dune
        //
        // Codices can only ever be tagged with non-group tags.
        var eberronTag = new Tag { Name = "Eberron" };
        var wizardsTag = new Tag { Name = "Wizards", Children = new RangeObservableCollection<Tag> { eberronTag } };
        var paizoTag = new Tag { Name = "Paizo" };
        var dndTag = new Tag { Name = "DnD" };
        var fantasyTag = new Tag
        {
            Name = "Fantasy",
            IsGroup = true,
            Children = new RangeObservableCollection<Tag> { wizardsTag, paizoTag, dndTag }
        };
        var starWarsTag = new Tag { Name = "StarWars" };
        var duneTag = new Tag { Name = "Dune" };
        var sciFiTag = new Tag { Name = "SciFi", IsGroup = true, Children = new RangeObservableCollection<Tag> { starWarsTag, duneTag } };

        var collection = new CodexCollection("FilterServiceTestCollection");
        collection.AddTags([fantasyTag, sciFiTag]);

        AddCodex(collection, "Alpha", [wizardsTag], publisher: "WotC", rating: 1);
        AddCodex(collection, "Beta", [eberronTag], publisher: "WotC", rating: 2, path: @"C:\books\beta.pdf", authors: ["Brandon"]);
        AddCodex(collection, "Gamma", [paizoTag], publisher: "Paizo", rating: 3, path: @"C:\books\gamma.pdf");
        AddCodex(collection, "Delta", [dndTag], publisher: "Paizo", rating: 1, authors: ["Brandon", "George"]);
        AddCodex(collection, "Epsilon", [starWarsTag], publisher: "Grimm", rating: 4, favorite: true);
        AddCodex(collection, "Zeta", [duneTag], publisher: "Paizo", rating: 5);
        AddCodex(collection, "Eta", [wizardsTag, starWarsTag], publisher: "WotC", rating: 2);

        return new CodexCollectionVM("FilterServiceTestCollection", collection, new CodexCollectionMemRepository());
    }

    private static void AddCodex(CodexCollection collection, string title, Tag[] tags,
        string publisher = "", int rating = 0, string path = "", bool favorite = false, string[]? authors = null)
    {
        Codex codex = new(collection)
        {
            Id = title.Length,
            Title = title,
            Publisher = publisher,
            Rating = rating,
            Favorite = favorite,
            Sources = new SourceSet { Path = path },
            Authors = authors is null ? new() : new(authors),
            Tags = new RangeObservableCollection<Tag>(tags)
        };
        collection.AllCodices.Add(codex);
    }

    private TagFilter CreateTagFilter(string tagName) =>
        new(_collectionVm.GetTagVm(_collectionVm.Collection.AllTags.Single(tag => tag.Name == tagName)));

    private IList<CodexViewModel> Filter(IEnumerable<Filter>? included = null, IEnumerable<Filter>? excluded = null) =>
        _filterService.FilterCodices(
            _collectionVm.AllCodexVms.ToList(),
            new FiltersState
            {
                IncludedFilters = included?.ToList() ?? [],
                ExcludedFilters = excluded?.ToList() ?? []
            });

    private static string[] Titles(IList<CodexViewModel> result) =>
        result.Select(vm => vm.Title).OrderBy(title => title).ToArray();

    #endregion

    private class StubImportExportService : IImportExportService
    {
        public Task<CodexCollection?> OpenSatchel(string? satchelPath = null) => Task.FromResult<CodexCollection?>(null);
        public Task ExportCollection(CodexCollection collection, IStorageFile? file, bool includeFiles, bool includeCovers) => Task.CompletedTask;
        public Task ExportTags(CodexCollection collection) => Task.CompletedTask;
        public void CompressUserDataToZip(string zipPath) { }
    }
}
