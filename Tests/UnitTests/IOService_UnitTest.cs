using COMPASS.Services;

namespace Tests.UnitTests
{
    [TestClass]
    public class IOService_UnitTest
    {
        [TestMethod]
        public void TestGetDifferingRoot_Normal()
        {
            //normal case
            string path1 = @"a\path\to\a\file.txt";
            string path2 = @"another\root\that goes\to\a\file.txt";

            var (r1, r2) = IOService.GetDifferingRoot(path1, path2);

            Assert.AreEqual(@"a\path", r1);
            Assert.AreEqual(@"another\root\that goes", r2);
        }

        [TestMethod]
        public void TestGetDifferingRoot_Same()
        {
            string path1 = @"a\path\to\a\file.txt";

            var (r1, r2) = IOService.GetDifferingRoot(path1, path1);

            Assert.AreEqual(r1, r2);
            Assert.AreEqual(r1, path1);
        }

        [TestMethod]
        public void TestGetDifferingRoot_Subpath()
        {
            string path1 = @"a\path\to\a\file.txt";
            string path2 = @"a\file.txt";

            var (r1, r2) = IOService.GetDifferingRoot(path1, path2);

            Assert.AreEqual(@"a\path\to", r1);
            Assert.AreEqual(@"", r2);
        }

        [TestMethod]
        public void TestGetDifferingRoot_Different()
        {
            string path1 = @"a\path\to\a\file.txt";
            string path2 = @"another\root\that goes\to\anotherfile.txt";

            var (r1, r2) = IOService.GetDifferingRoot(path1, path2);

            Assert.AreEqual(path1, r1);
            Assert.AreEqual(path2, r2);
        }
    }
}
