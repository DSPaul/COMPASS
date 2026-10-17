using COMPASS.Infra.Avalonia.Files;
using COMPASS.Infra.Logging;

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
