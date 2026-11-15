using Avalonia.Platform.Storage;
using COMPASS.Infra.Avalonia.Files;
using COMPASS.UnitTests.Infra.Avalonia.DragDrop;

namespace COMPASS.UnitTests.Infra.Avalonia.Files;

[TestFixture]
public class FilesServiceTests
{
    private FilesService _filesService = null!;

    [SetUp]
    public void SetUp()
    {
        _filesService = new FilesService();
    }

    [Test]
    public void ClassifyDroppedFiles_SeveralItemsOnDisk_KeepsTheirPathsInDropOrder()
    {
        string firstPath = Path.GetFullPath(Path.Combine("books", "first.pdf"));
        string secondPath = Path.GetFullPath(Path.Combine("books", "second.pdf"));

        _filesService.ClassifyDroppedFiles([FakeDroppedItems.OnDisk(firstPath), FakeDroppedItems.OnDisk(secondPath)],
            out List<string> filesOnDisk, out List<IStorageFile> filesFromStream);

        Assert.Multiple(() =>
        {
            Assert.That(filesOnDisk, Is.EqualTo(new[] { firstPath, secondPath }));
            Assert.That(filesFromStream, Is.Empty);
        });
    }

    [Test]
    public void ClassifyDroppedFiles_VirtualFile_IsReadFromStreamInsteadOfAPath()
    {
        //Dragging an entry out of a ZIP folder, or a mail attachment, hands over a virtual file that
        //only exists inside the source app. Its LocalPath still answers "/adventure.pdf", which looks
        //like a path on disk but is not one.
        IStorageFile virtualFile = FakeDroppedItems.VirtualFile("adventure.pdf");

        _filesService.ClassifyDroppedFiles([virtualFile], out List<string> filesOnDisk, out List<IStorageFile> filesFromStream);

        Assert.Multiple(() =>
        {
            Assert.That(filesOnDisk, Is.Empty);
            Assert.That(filesFromStream, Is.EqualTo(new[] { virtualFile }));
        });
    }

    [Test]
    public void ClassifyDroppedFiles_MixOfFileOnDiskAndVirtualFile_SplitsThemEachWay()
    {
        string filePath = Path.GetFullPath(Path.Combine("books", "adventure.pdf"));
        IStorageFile virtualFile = FakeDroppedItems.VirtualFile("rules.pdf");

        _filesService.ClassifyDroppedFiles([virtualFile, FakeDroppedItems.OnDisk(filePath)],
            out List<string> filesOnDisk, out List<IStorageFile> filesFromStream);

        Assert.Multiple(() =>
        {
            Assert.That(filesOnDisk, Is.EqualTo(new[] { filePath }));
            Assert.That(filesFromStream, Is.EqualTo(new[] { virtualFile }));
        });
    }

    [Test]
    public void ClassifyDroppedFiles_ItemWithoutAPathThatIsNotAFile_IsLeftOut()
    {
        _filesService.ClassifyDroppedFiles([FakeDroppedItems.VirtualItemWithoutContents("mystery")],
            out List<string> filesOnDisk, out List<IStorageFile> filesFromStream);

        Assert.Multiple(() =>
        {
            Assert.That(filesOnDisk, Is.Empty);
            Assert.That(filesFromStream, Is.Empty);
        });
    }

    [Test]
    public void ClassifyDroppedFiles_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => _filesService.ClassifyDroppedFiles(null!, out _, out _));
    }
}
