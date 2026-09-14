using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Services.FileSystem;
using COMPASS.Infra.Tools.Logging;

namespace COMPASS.Tests.Common.Mocks
{
    public class MockIOService : IOServiceBase
    {
        public MockIOService(IFilesService filesService, ILogger logger) : base(filesService, logger)
        {
            
        }

        public override void ShowInExplorer(string filePath)
        {
            //No implementation needed for mock
        }
    }
}
