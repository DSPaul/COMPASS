using Autofac.Features.Indexed;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.Sources;
using COMPASS.Common.ViewModels.Main;
using COMPASS.Infra.Tools;

namespace COMPASS.IntegrationTests.Common.Sources;

[TestFixture]
public class HomeBrewery
{
    private const string TEST_URL = @"https://homebrewery.naturalcrit.com/share/FegJIEB2KUUo";

    [Test]
    public async Task GetMetaDataFromHomeBrewery()
    {
        SourceSet sources = new()
        {
            SourceURL = TEST_URL
        };

        var collection = new CodexCollection("TEST_COLLECTION");
        var source = ServiceResolver.Resolve<IIndex<string, MetaDataSource>>()[MetaDataSourceType.Homebrewery.ToString()];

        SourceMetaData response = await source.GetMetaData(sources, collection.AllTags);

        Assert.Multiple(() =>
        {
            Assert.That(response, Is.Not.Null);
            Assert.That(string.IsNullOrEmpty(response.Title), Is.False);
            Assert.That(string.IsNullOrEmpty(response.Description), Is.False);
            Assert.That(response.ReleaseDate, Is.Not.Null);
            Assert.That(response.PageCount, Is.EqualTo(1));
        });
    }

    [Test, Order(2)]
    public async Task GetCoverFromHomeBrewery()
    {
        //Setup
        var source = ServiceResolver.Resolve<IIndex<string, MetaDataSource>>()[MetaDataSourceType.Homebrewery.ToString()];
        var sources = new SourceSet()
        {
            SourceURL = TEST_URL
        };

        //Fetch the cover
        var cover = await source!.FetchCover(sources);

        //see if it worked
        Assert.That(cover, Is.Not.Null, "Failed to fetch cover");
    }
}