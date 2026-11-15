using COMPASS.Common.Domain;
using COMPASS.Common.Features.Persistence.Storage;
using COMPASS.Tests.Common.Mocks;
using COMPASS.UnitTests.Infra.Avalonia.DragDrop;

namespace COMPASS.UnitTests.Features.Persistence;

[TestFixture]
public class SaveStorageFilesTests
{
    private string _tempRoot = "";
    private string _filesDirectory = "";
    private CodexCollection _collection = null!;
    private MockLogger _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
        _filesDirectory = Path.Combine(_tempRoot, Constants.DIR_COLLECTIONS, "Test", Constants.DIR_FILES);
        _collection = new CodexCollection("Test");
        _logger = new MockLogger();
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, true);
        }
    }

    #region Saving a virtual file

    [Test]
    public async Task SaveStorageFiles_VirtualFile_SavesItsContentsUnderItsNameInTheCollectionFileFolder()
    {
        UserFilesStorageService storage = CreateStorage();

        List<string> savedPaths = await storage.SaveStorageFiles(_collection, [FakeDroppedItems.VirtualFile("adventure.pdf", "the rules")]);

        string expectedPath = Path.Combine(_filesDirectory, "adventure.pdf");
        Assert.Multiple(() =>
        {
            Assert.That(savedPaths, Is.EqualTo(new[] { expectedPath }));
            Assert.That(File.ReadAllText(expectedPath), Is.EqualTo("the rules"));
        });
    }

    [Test]
    public async Task SaveStorageFiles_SeveralVirtualFiles_SavesEachOfThem()
    {
        UserFilesStorageService storage = CreateStorage();

        List<string> savedPaths = await storage.SaveStorageFiles(_collection,
            [FakeDroppedItems.VirtualFile("first.pdf", "one"), FakeDroppedItems.VirtualFile("second.pdf", "two")]);

        Assert.Multiple(() =>
        {
            Assert.That(savedPaths, Is.EqualTo(new[]
            {
                Path.Combine(_filesDirectory, "first.pdf"),
                Path.Combine(_filesDirectory, "second.pdf")
            }));
            Assert.That(File.ReadAllText(Path.Combine(_filesDirectory, "second.pdf")), Is.EqualTo("two"));
        });
    }

    [Test]
    public async Task SaveStorageFiles_NoFiles_ReturnsNoPaths()
    {
        UserFilesStorageService storage = CreateStorage();

        List<string> savedPaths = await storage.SaveStorageFiles(_collection, []);

        Assert.That(savedPaths, Is.Empty);
    }

    #endregion

    #region Name clashes

    [Test]
    public async Task SaveStorageFiles_NameAlreadyTakenByAnotherItemsFile_SavesUnderANumberedName()
    {
        UserFilesStorageService storage = CreateStorage();
        string existingPath = WriteFile("rules.pdf", "the old item");

        List<string> savedPaths = await storage.SaveStorageFiles(_collection, [FakeDroppedItems.VirtualFile("rules.pdf", "the new item")]);

        string expectedPath = Path.Combine(_filesDirectory, "rules (1).pdf");
        Assert.Multiple(() =>
        {
            Assert.That(savedPaths, Is.EqualTo(new[] { expectedPath }));
            Assert.That(File.ReadAllText(existingPath), Is.EqualTo("the old item"));
            Assert.That(File.ReadAllText(expectedPath), Is.EqualTo("the new item"));
        });
    }

    [Test]
    public async Task SaveStorageFiles_SameNameDroppedTwiceInOneGo_KeepsBoth()
    {
        UserFilesStorageService storage = CreateStorage();

        List<string> savedPaths = await storage.SaveStorageFiles(_collection,
            [FakeDroppedItems.VirtualFile("rules.pdf", "from the first zip"), FakeDroppedItems.VirtualFile("rules.pdf", "from the second zip")]);

        Assert.Multiple(() =>
        {
            Assert.That(savedPaths, Is.EqualTo(new[]
            {
                Path.Combine(_filesDirectory, "rules.pdf"),
                Path.Combine(_filesDirectory, "rules (1).pdf")
            }));
            Assert.That(File.ReadAllText(savedPaths[0]), Is.EqualTo("from the first zip"));
            Assert.That(File.ReadAllText(savedPaths[1]), Is.EqualTo("from the second zip"));
        });
    }

    [Test]
    public async Task SaveStorageFiles_NameThatCarriesADirectory_IsSavedInsideTheFileFolder()
    {
        UserFilesStorageService storage = CreateStorage();

        List<string> savedPaths = await storage.SaveStorageFiles(_collection,
            [FakeDroppedItems.VirtualFile(Path.Combine("..", "..", "escape.pdf"), "contents")]);

        Assert.Multiple(() =>
        {
            Assert.That(savedPaths, Has.Count.EqualTo(1));
            Assert.That(Path.GetDirectoryName(savedPaths[0]), Is.EqualTo(_filesDirectory));
            Assert.That(File.Exists(savedPaths[0]), Is.True);
        });
    }

    #endregion

    #region Failures

    [Test]
    public async Task SaveStorageFiles_FileThatCannotBeRead_IsLeftOutAndTheOthersAreSaved()
    {
        UserFilesStorageService storage = CreateStorage();

        List<string> savedPaths = await storage.SaveStorageFiles(_collection,
            [FakeDroppedItems.UnreadableVirtualFile("broken.pdf"), FakeDroppedItems.VirtualFile("adventure.pdf", "the rules")]);

        Assert.Multiple(() =>
        {
            Assert.That(savedPaths, Is.EqualTo(new[] { Path.Combine(_filesDirectory, "adventure.pdf") }));
            Assert.That(Directory.GetFiles(_filesDirectory), Is.EqualTo(new[] { Path.Combine(_filesDirectory, "adventure.pdf") }),
                "a failed read must not leave the broken file or its partial copy behind");
            Assert.That(_logger.Warnings, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task SaveStorageFiles_FileFolderDoesNotExistYet_CreatesItAndSavesTheFile()
    {
        UserFilesStorageService storage = CreateStorage();
        Assert.That(Directory.Exists(_filesDirectory), Is.False);

        List<string> savedPaths = await storage.SaveStorageFiles(_collection, [FakeDroppedItems.VirtualFile("adventure.pdf", "the rules")]);

        Assert.Multiple(() =>
        {
            Assert.That(savedPaths, Has.Count.EqualTo(1));
            Assert.That(File.Exists(Path.Combine(_filesDirectory, "adventure.pdf")), Is.True);
        });
    }

    [Test]
    public async Task SaveStorageFiles_FileFolderCannotBeCreated_ReturnsNoPaths()
    {
        UserFilesStorageService storage = CreateStorage();
        //A file where the folder should be makes creating the folder fail
        Directory.CreateDirectory(Path.GetDirectoryName(_filesDirectory)!);
        File.WriteAllText(_filesDirectory, "");

        List<string> savedPaths = await storage.SaveStorageFiles(_collection, [FakeDroppedItems.VirtualFile("adventure.pdf", "the rules")]);

        Assert.That(savedPaths, Is.Empty);
    }

    [Test]
    public async Task SaveStorageFiles_Collection_IsRequired()
    {
        UserFilesStorageService storage = CreateStorage();

        await Assert.ThrowsAsync<ArgumentNullException>(() => storage.SaveStorageFiles(null!, []));
    }

    #endregion

    #region Helpers

    private UserFilesStorageService CreateStorage()
    {
        MockApplicationDataService applicationDataService = new() { UserDataPath = _tempRoot };
        MockIOService ioService = new(new MockFilesService(), _logger);
        return new UserFilesStorageService(applicationDataService, ioService, _logger);
    }

    private string WriteFile(string fileName, string contents)
    {
        Directory.CreateDirectory(_filesDirectory);
        string path = Path.Combine(_filesDirectory, fileName);
        File.WriteAllText(path, contents);
        return path;
    }

    #endregion
}
