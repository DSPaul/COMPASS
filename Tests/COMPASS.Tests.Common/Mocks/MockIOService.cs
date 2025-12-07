using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Services.FileSystem;

namespace COMPASS.Tests.Common.Mocks
{
    public class MockIOService : IOServiceBase
    {
        public MockIOService(IFilesService filesService) : base(filesService)
        {
            
        }

        public override void ShowInExplorer(string filePath)
        {
            //No implementation needed for mock
        }
    }
}
