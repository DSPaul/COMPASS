using COMPASS.Common.Models;
using COMPASS.Common.ViewModels.Modals.Import;
using COMPASS.Infra.Collections;
using COMPASS.Tests.Common.Mocks;

namespace COMPASS.UnitTests.ViewModels.Modals.Import;

[TestFixture]
public class ImportFolderViewModelTests
{
    private string _tempRoot = null!;
    private MockLogger _logger = null!;
    private FolderFactory _folderFactory = null!;

    private string _rootPdf = null!;
    private string _rootTxt = null!;
    private string _subAPdf1 = null!;
    private string _subAPdf2 = null!;
    private string _subBEpub = null!;
    private List<string> _allFiles = null!;

    [SetUp]
    public void SetUp()
    {
        _logger = new MockLogger();
        var mockIOService = new MockIOService(new MockFilesService(), _logger);
        _folderFactory = new FolderFactory(_logger, mockIOService);

        _tempRoot = Path.Combine(Path.GetTempPath(), $"CompassImportTest_{Guid.NewGuid()}");
        string subADirectory = Path.Combine(_tempRoot, "subA");
        string subBDirectory = Path.Combine(_tempRoot, "subB");
        Directory.CreateDirectory(subADirectory);
        Directory.CreateDirectory(subBDirectory);

        _rootPdf = CreateEmptyFile(_tempRoot, "root.pdf");
        _rootTxt = CreateEmptyFile(_tempRoot, "notes.txt");
        _subAPdf1 = CreateEmptyFile(subADirectory, "a1.pdf");
        _subAPdf2 = CreateEmptyFile(subADirectory, "a2.pdf");
        _subBEpub = CreateEmptyFile(subBDirectory, "b1.epub");
        _allFiles = [_rootPdf, _rootTxt, _subAPdf1, _subAPdf2, _subBEpub];
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }

    [Test]
    public void OpenEditWindowWhenDone_DefaultsToFalse()
    {
        var dialogVm = CreateDialogVm(isAutoImport: false, new CollectionInfo());

        Assert.That(dialogVm.OpenEditWindowWhenDone, Is.False);
    }

    [Test]
    public void IncludeSubfolders_DefaultsToTrue()
    {
        var dialogVm = CreateDialogVm(isAutoImport: false, new CollectionInfo());

        Assert.That(dialogVm.IncludeSubfolders, Is.True);
        Assert.That(dialogVm.GetFilteredFiles(_allFiles), Is.EquivalentTo(_allFiles));
    }

    [Test]
    public void GetFilteredFiles_IncludeSubfoldersUnchecked_ReturnsTopLevelFilesOnly()
    {
        var dialogVm = CreateDialogVm(isAutoImport: false, new CollectionInfo());

        dialogVm.IncludeSubfolders = false;

        List<string> filteredFiles = dialogVm.GetFilteredFiles(_allFiles).ToList();

        Assert.That(filteredFiles, Is.EquivalentTo(new[] { _rootPdf, _rootTxt }));
    }

    [Test]
    public void ExcludeFileTypes_KnownExtensionUncheckedInPreferences_DefaultsToTrue()
    {
        var collectionInfo = new CollectionInfo
        {
            FiletypePreferences = new Dictionary<string, bool>
            {
                [".pdf"] = true,
                [".txt"] = false,
                [".epub"] = true
            }
        };

        var dialogVm = CreateDialogVm(isAutoImport: false, collectionInfo);

        Assert.That(dialogVm.ExcludeFileTypes, Is.True);
        Assert.That(dialogVm.FilesToImportCount, Is.EqualTo(4));
    }

    [Test]
    public void ExcludeFileTypes_AllKnownExtensionsCheckedInPreferences_DefaultsToFalse()
    {
        var collectionInfo = new CollectionInfo
        {
            FiletypePreferences = new Dictionary<string, bool>
            {
                [".pdf"] = true,
                [".txt"] = true,
                [".epub"] = true
            }
        };

        var dialogVm = CreateDialogVm(isAutoImport: false, collectionInfo);

        Assert.That(dialogVm.ExcludeFileTypes, Is.False);
    }

    [Test]
    public void GetFilteredFiles_StoredExclusionWithoutExclusionEnabled_ReturnsAllFiles()
    {
        var collectionInfo = new CollectionInfo
        {
            FiletypePreferences = new Dictionary<string, bool>
            {
                [".pdf"] = true,
                [".txt"] = false,
                [".epub"] = true
            }
        };
        var dialogVm = CreateDialogVm(isAutoImport: false, collectionInfo);
        dialogVm.ExcludeFileTypes = false;

        List<string> filteredFiles = dialogVm.GetFilteredFiles(_allFiles).ToList();

        Assert.That(filteredFiles, Is.EquivalentTo(_allFiles));
    }

    private ImportFolderViewModel CreateDialogVm(bool isAutoImport, CollectionInfo collectionInfo)
    {
        List<Folder> folders = [_folderFactory.Create(_tempRoot)];

        return new ImportFolderViewModel(_logger, _folderFactory, isAutoImport, collectionInfo, folders, _allFiles);
    }

    private static string CreateEmptyFile(string directory, string fileName)
    {
        string filePath = Path.Combine(directory, fileName);
        File.WriteAllBytes(filePath, []);

        return filePath;
    }
}
