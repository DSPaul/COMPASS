using Autofac;
using Autofac.Core;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models;
using COMPASS.Common.Services.Storage;
using COMPASS.Infra.Tools;
using COMPASS.Tests.Common.Mocks;

namespace COMPASS.IntegrationTests.Common.Services;

[TestFixture]
public class CoverStorageServiceTests
{
    private string _testCollectionName = null!;
    private string _collectionsPath = null!;

    private IContainer BuildContainer() => Helpers.SetupContainer(builder => 
            builder.RegisterType<CoverStorageService>().As<ICoverStorageService>());

    [SetUp]
    public void SetUp()
    {
        var container = BuildContainer();
        _testCollectionName = $"__CoverTest_{Guid.NewGuid():N}";
        _collectionsPath = Path.Combine(container.Resolve<IApplicationDataService>().UserDataPath, Constants.DIR_COLLECTIONS);
    }

    [TearDown]
    public void TearDown()
    {
        string collectionDir = Path.Combine(_collectionsPath, _testCollectionName);
        if (Directory.Exists(collectionDir))
        {
            Directory.Delete(collectionDir, true);
        }
    }

    [Test]
    public void InitCodexImagePaths_SetsExpectedPath()
    {
        var container = BuildContainer();

        var collection = new CodexCollection(_testCollectionName);
        var codex = new Codex(collection) { Id = 42 };
        var coverStorageService = container.Resolve<ICoverStorageService>();
        coverStorageService.InitCodexImagePaths(codex);

        Assert.That(codex.CoverArtPath, Does.Contain(_testCollectionName));
        Assert.That(codex.CoverArtPath, Does.EndWith("42.png"));
    }

    [Test]
    public void MoveCodexDataToCollection_CopiesExistingCover()
    {
        var container = BuildContainer();

        var sourceCollection = new CodexCollection(_testCollectionName);
        string targetCollectionName = $"__CoverTestTarget_{Guid.NewGuid():N}";
        var targetCollection = new CodexCollection(targetCollectionName);

        // Create source cover directory and file
        string sourceCoverDir = Path.Combine(_collectionsPath, _testCollectionName, Constants.DIR_COVERS);
        Directory.CreateDirectory(sourceCoverDir);
        string sourceFile = Path.Combine(sourceCoverDir, "1.png");
        File.WriteAllBytes(sourceFile, [0x89, 0x50, 0x4E, 0x47]);

        var codex = new Codex(sourceCollection) { Id = 1, CoverArtPath = sourceFile };

        // Pre-create target cover directory (EnsureDirectoryExists treats path as file path, creating only parent)
        string targetCoverDir = Path.Combine(_collectionsPath, targetCollectionName, Constants.DIR_COVERS);
        Directory.CreateDirectory(targetCoverDir);

        try
        {
            var coverStorageService = container.Resolve<ICoverStorageService>();
            coverStorageService.MoveCodexDataToCollection(codex, targetCollection, copy: true);

            string expectedTarget = Path.Combine(_collectionsPath, targetCollectionName, Constants.DIR_COVERS, "1.png");
            Assert.That(File.Exists(expectedTarget), Is.True);
            // Original should still exist since we copied
            Assert.That(File.Exists(sourceFile), Is.True);
        }
        finally
        {
            string targetDir = Path.Combine(_collectionsPath, targetCollectionName);
            if (Directory.Exists(targetDir)) Directory.Delete(targetDir, true);
        }
    }

    [Test]
    public void OnCollectionRenamed_UpdatesCoverPaths()
    {
        var collection = new CodexCollection(_testCollectionName);
        var codex = new Codex(collection) { Id = 5, CoverArtPath = "old/path.png" };
        collection.AllCodices.Add(codex);

        var container = BuildContainer();
        var coverStorageService = container.Resolve<ICoverStorageService>();
        coverStorageService.OnCollectionRenamed(collection);

        Assert.That(codex.CoverArtPath, Does.Contain(_testCollectionName));
        Assert.That(codex.CoverArtPath, Does.EndWith("5.png"));
    }

    [Test]
    public void OnCodexDeleted_DeletesCoverFile()
    {
        var collection = new CodexCollection(_testCollectionName);
        string coverDir = Path.Combine(_collectionsPath, _testCollectionName, Constants.DIR_COVERS);
        Directory.CreateDirectory(coverDir);
        string coverFile = Path.Combine(coverDir, "10.png");
        File.WriteAllBytes(coverFile, [0x89, 0x50, 0x4E, 0x47]);

        var codex = new Codex(collection) { Id = 10, CoverArtPath = coverFile };

        var container = BuildContainer();
        var coverStorageService = container.Resolve<ICoverStorageService>();
        coverStorageService.OnCodexDeleted(codex);

        Assert.That(File.Exists(coverFile), Is.False);
    }

    [Test]
    public void OnCodexDeleted_NonExistentFile_DoesNotThrow()
    {
        var collection = new CodexCollection(_testCollectionName);
        string coverDir = Path.Combine(_collectionsPath, _testCollectionName, Constants.DIR_COVERS);
        var codex = new Codex(collection) { Id = 99, CoverArtPath = Path.Combine(coverDir, "99.png") };

        var container = BuildContainer();
        var coverStorageService = container.Resolve<ICoverStorageService>();
        Assert.DoesNotThrow(() => coverStorageService.OnCodexDeleted(codex));
    }
}
