using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models;
using COMPASS.Infra.Tools;

namespace COMPASS.Common.Sources
{
    /// <summary>
    /// A baseclass for metadatasources that use the SourceURL to get metadata from a website
    /// </summary>
    public abstract class OnlineMetaDataSource : MetaDataSource
    {
        protected OnlineMetaDataSource(CodexCollection targetCollection) : base(targetCollection)
        { }

        protected IWebService WebService => field ??= ServiceResolver.Resolve<IWebService>();
        protected IWebDriverService WebDriverService => field ??= ServiceResolver.Resolve<IWebDriverService>();

        /// <summary>
        /// The prefix of the URL for this source. For example, "https://www.example.com/metadata/".
        /// </summary>
        public abstract string UrlPrefix { get; }

        public override bool IsValidSource(SourceSet sources) => sources.HasOnlineSource() && sources.SourceURL.StartsWith(UrlPrefix);
    }
}
