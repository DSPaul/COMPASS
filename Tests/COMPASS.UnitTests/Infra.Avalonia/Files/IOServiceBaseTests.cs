using COMPASS.Tests.Common.Mocks;

namespace COMPASS.UnitTests.Infra.Avalonia.Files
{
    [TestFixture]
    public class IOServiceBaseTests
    {
        private string _tempRoot = "";
        private MockLogger _logger = null!;
        private MockIOService _ioService = null!;

        [SetUp]
        public void SetUp()
        {
            _tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempRoot);
            _logger = new MockLogger();
            _ioService = new MockIOService(new MockFilesService(), _logger);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempRoot))
            {
                Directory.Delete(_tempRoot, true);
            }
        }

        private string CreateSubDirectory(string name)
        {
            string path = Path.Combine(_tempRoot, name);
            Directory.CreateDirectory(path);
            return path;
        }

        [Test]
        public void TryGetDirectories_VisibleSubFolders_AreAllReturned()
        {
            // Arrange
            CreateSubDirectory("books");
            CreateSubDirectory("rules");

            // Act
            var subFolders = _ioService.TryGetDirectories(_tempRoot);

            // Assert
            Assert.That(subFolders.Select(Path.GetFileName), Is.EquivalentTo(new[] { "books", "rules" }));
        }

        [Test]
        public void TryGetDirectories_HiddenSubFolder_IsSkipped()
        {
            // Hidden is a real Windows attribute, other platforms derive it from the name
            Assume.That(OperatingSystem.IsWindows(), "Hidden attributes are only settable on Windows");

            // Arrange
            CreateSubDirectory("books");
            string hiddenFolder = CreateSubDirectory("System Volume Information");
            File.SetAttributes(hiddenFolder, FileAttributes.Hidden);

            // Act
            var subFolders = _ioService.TryGetDirectories(_tempRoot);

            // Assert
            Assert.That(subFolders.Select(Path.GetFileName), Is.EqualTo(new[] { "books" }));
        }

        [Test]
        public void TryGetDirectories_MissingDirectory_ReturnsEmpty()
        {
            // Arrange
            string missingDirectory = Path.Combine(_tempRoot, "does-not-exist");

            // Act
            var subFolders = _ioService.TryGetDirectories(missingDirectory);

            // Assert
            Assert.That(subFolders, Is.Empty);
            Assert.That(_logger.Errors, Has.Count.EqualTo(1));
        }
    }
}
