using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models;
using COMPASS.Infra.Models.Progress;
using COMPASS.Infra.Tools;
using COMPASS.Tests.Common.Mocks;

namespace COMPASS.UnitTests.Models
{
    [TestFixture]
    public class FolderTests
    {
        private string _tempRoot = "";
        private FolderFactory _folderFactory = null!;
        private MockLogger _folderFactoryLogger = null!;
        private MockLogger _ioServiceLogger = null!;

        [SetUp]
        public void SetUp()
        {
            _tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempRoot);
            _folderFactoryLogger = new MockLogger();
            _ioServiceLogger = new MockLogger();
            _folderFactory = new FolderFactory(_folderFactoryLogger, new MockIOService(new MockFilesService(), _ioServiceLogger));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempRoot))
            {
                Directory.Delete(_tempRoot, true);
            }
        }

        private string CreateSubDirectory(string parent, string name)
        {
            string path = Path.Combine(parent, name);
            Directory.CreateDirectory(path);
            return path;
        }

        [Test]
        public void SubFolders_FirstAccess_LoadsFromDisk()
        {
            // Arrange
            CreateSubDirectory(_tempRoot, "subA");
            CreateSubDirectory(_tempRoot, "subB");
            Folder folder = _folderFactory.Create(_tempRoot);

            // Act
            var subFolders = folder.SubFolders;

            // Assert
            Assert.That(subFolders.Select(f => f.Name).ToHashSet(), Is.EqualTo(new HashSet<string> { "subA", "subB" }));
            AssertNoWarningsOrErrors(_folderFactoryLogger);
            AssertNoWarningsOrErrors(_ioServiceLogger);
        }

        private static void AssertNoWarningsOrErrors(MockLogger mockLogger)
        {
            Assert.That(mockLogger.Warnings, Is.Empty, "Unexpected warnings logged");
            Assert.That(mockLogger.Errors, Is.Empty, "Unexpected errors logged");
            Assert.That(mockLogger.Fatals, Is.Empty, "Unexpected fatals logged");
        }

        [Test]
        public void SubFolders_LoadsLazily_SeesDiskChangesAfterCreate()
        {
            // Arrange
            Folder folder = _folderFactory.Create(_tempRoot);
            CreateSubDirectory(_tempRoot, "late");

            // Act
            var subFolders = folder.SubFolders;

            // Assert: an eager snapshot would still be empty here
            Assert.That(subFolders.Select(f => f.Name).ToList(), Is.EqualTo(new List<string> { "late" }));
            AssertNoWarningsOrErrors(_folderFactoryLogger);
            AssertNoWarningsOrErrors(_ioServiceLogger);
        }

        [Test]
        public void SubFolders_MissingDirectory_ReturnsEmptyWithoutThrowing()
        {
            // Arrange
            Folder folder = _folderFactory.Create(Path.Combine(_tempRoot, "does-not-exist"));

            // Act + Assert
            Assert.That(() => folder.SubFolders.ToList(), Throws.Nothing);
            Assert.That(folder.SubFolders, Is.Empty);
            AssertNoWarningsOrErrors(_folderFactoryLogger);
            AssertNoWarningsOrErrors(_ioServiceLogger);
        }

        [Test]
        public void UpdateAllSubFolders_RefreshesFromDisk()
        {
            // Arrange
            Folder folder = _folderFactory.Create(_tempRoot);
            Assert.That(folder.SubFolders, Is.Empty);
            CreateSubDirectory(_tempRoot, "new");

            // Act
            folder.UpdateAllSubFolders();

            // Assert
            Assert.That(folder.SubFolders.Select(f => f.Name).ToList(), Is.EqualTo(new List<string> { "new" }));
            AssertNoWarningsOrErrors(_folderFactoryLogger);
            AssertNoWarningsOrErrors(_ioServiceLogger);
        }

        [Test]
        public void SubFolders_SymbolicLinkLoop_Terminates()
        {
            // Arrange: real/loop -> tempRoot, so expanding loop revisits root
            string realDir = CreateSubDirectory(_tempRoot, "real");
            try
            {
                Directory.CreateSymbolicLink(Path.Combine(realDir, "loop"), _tempRoot);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
            {
                Assert.Ignore($"Symlinks require privileges on this machine: {ex.Message}");
                return;
            }
            Folder folder = _folderFactory.Create(_tempRoot);

            // Act: full expansion would hang forever on a cycle
            List<Folder> level1 = [], level2 = [], level3 = [];
            Assert.That(() =>
            {
                level1 = folder.SubFolders.ToList();
                level2 = level1.SelectMany(f => f.SubFolders).ToList();
                level3 = level2.SelectMany(f => f.SubFolders).ToList();
            }, Throws.Nothing);

            // Assert: the revisit of root through the loop is pruned
            Assert.That(level1.Select(f => f.Name).ToList(), Is.EqualTo(new List<string> { "real" }));
            Assert.That(level2.Select(f => f.Name).ToList(), Is.EqualTo(new List<string> { "loop" }));
            Assert.That(level3, Is.Empty);

            Assert.That(_folderFactoryLogger.Warnings, Has.Count.EqualTo(1));
            Assert.That(_folderFactoryLogger.Warnings[0], Does.Contain("cycle"));
            Assert.That(_folderFactoryLogger.Errors, Is.Empty);
            Assert.That(_folderFactoryLogger.Fatals, Is.Empty);
        }

        [Test]
        public void SetExplicitSubfolders_InvalidInput_LeavesPreviousIntact()
        {
            // Arrange
            string subA = CreateSubDirectory(_tempRoot, "subA");
            Folder folder = _folderFactory.Create(_tempRoot);
            folder.SetExplicitSubfolders(folder.AllSubFolders);
            Assert.That(folder.SubFolders.Select(f => f.FullPath).ToList(), Is.EqualTo(new List<string> { subA }));

            // Act: attempt to set a folder that lives outside this folder entirely
            string outsideRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(outsideRoot);
            try
            {
                var outsider = _folderFactory.Create(outsideRoot);
                Assert.Throws<InvalidOperationException>(() => folder.SetExplicitSubfolders([outsider]));
            }
            finally
            {
                Directory.Delete(outsideRoot, true);
            }

            // Assert: previous explicits untouched, no partial empty state
            Assert.That(folder.HasAllSubFolders, Is.False);
            Assert.That(folder.SubFolders.Select(f => f.FullPath).ToList(), Is.EqualTo(new List<string> { subA }));
            AssertNoWarningsOrErrors(_folderFactoryLogger);
            AssertNoWarningsOrErrors(_ioServiceLogger);
        }

        /// <summary>
        /// Fake listings that loop (child points back at root), simulating symlink
        /// indirection without needing link privileges. Runs on every machine.
        /// </summary>
        private sealed class CyclicListingIOService(string root, string child) : IIOService
        {
            public IEnumerable<string> TryGetDirectories(string directory)
            {
                if (PathUtils.PathsEqual(directory, child)) return [root];
                if (PathUtils.PathsEqual(directory, root)) return [child];
                return [];
            }

            public void ClearTmpData(string? tempPath = null) { }
            public void ShowInExplorer(string filePath) => throw new NotImplementedException();
            public Task<string?> PickFolder() => throw new NotImplementedException();
            public Task<IList<string>> TryPickFolders() => throw new NotImplementedException();
            public Task<bool> CopyDataAsync(string sourceDir, string destDir, IProgress<IProgressReport>? progressTracker, CancellationToken cancellationToken) => throw new NotImplementedException();
            public bool EnsureDirectoryExists(string path) => throw new NotImplementedException();
            public IEnumerable<string> TryGetFilesInFolder(string path) => [];
        }

        [Test]
        public void SubFolders_CyclicListing_TerminatesAndPrunesRevisit()
        {
            // Arrange: real dirs (pass the existence pre-checks), lying listings
            string child = CreateSubDirectory(_tempRoot, "child");
            var cyclicListingLogger = new MockLogger();
            var factory = new FolderFactory(cyclicListingLogger, new CyclicListingIOService(_tempRoot, child));
            Folder folder = factory.Create(_tempRoot);

            // Act: eager recursion would StackOverflow here, unguarded lazy would hang
            List<Folder> level1 = folder.SubFolders.ToList();
            List<Folder> level2 = level1.SelectMany(f => f.SubFolders).ToList();

            // Assert
            Assert.That(level1.Select(f => f.Name).ToList(), Is.EqualTo(new List<string> { "child" }));
            Assert.That(level2, Is.Empty);

            Assert.That(cyclicListingLogger.Warnings, Has.Count.EqualTo(1));
            Assert.That(cyclicListingLogger.Warnings[0], Does.Contain("cycle"));
            Assert.That(cyclicListingLogger.Errors, Is.Empty);
            Assert.That(cyclicListingLogger.Fatals, Is.Empty);
        }
    }
}
