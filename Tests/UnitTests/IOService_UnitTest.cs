using COMPASS.Common.Services.FileSystem;

namespace Tests.UnitTests
{
    [TestFixture]
    public class IOService_UnitTest
    {
        [Test]
        public void TestGetDifferingRoot_Normal()
        {
            //normal case
            string path1 = @"a\path\to\a\file.txt";
            string path2 = @"another\root\that goes\to\a\file.txt";

            var (r1, r2) = IOService.GetDifferingRoot(path1, path2);

            Assert.Multiple(() =>
            {
                Assert.That(r1, Is.EqualTo(@"a\path"));
                Assert.That(r2, Is.EqualTo(@"another\root\that goes"));
            });
        }

        [Test]
        public void TestGetDifferingRoot_Same()
        {
            string path1 = @"a\path\to\a\file.txt";

            var (r1, r2) = IOService.GetDifferingRoot(path1, path1);

            Assert.Multiple(() =>
            {
                Assert.That(r1, Is.EqualTo(r2));
                Assert.That(r1, Is.EqualTo(path1));
            });
        }

        [Test]
        public void TestGetDifferingRoot_Subpath()
        {
            string path1 = @"a\path\to\a\file.txt";
            string path2 = @"a\file.txt";

            var (r1, r2) = IOService.GetDifferingRoot(path1, path2);

            Assert.Multiple(() =>
            {
                Assert.That(r1, Is.EqualTo(@"a\path\to"));
                Assert.That(r2, Is.EqualTo(""));
            });
        }

        [Test]
        public void TestGetDifferingRoot_Different()
        {
            string path1 = @"a\path\to\a\file.txt";
            string path2 = @"another\root\that goes\to\anotherfile.txt";

            var (r1, r2) = IOService.GetDifferingRoot(path1, path2);

            Assert.Multiple(() =>
            {
                Assert.That(r1, Is.EqualTo(path1));
                Assert.That(r2, Is.EqualTo(path2));
            });
        }
    }
}
