using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.ViewModels;
using COMPASS.Infra.Tools.Logging;
using COMPASS.Infra.Tools;
using ImageMagick;

namespace COMPASS.Common.Sources
{
    public abstract class MetaDataSource
    {
        protected MetaDataSource(ILogger logger, IPreferencesService preferencesService)
        {
            Logger = logger;
            Preferences = preferencesService;
        }

        protected ILogger Logger { get; }
        protected IPreferencesService Preferences { get; }

        #region Import Logic

        protected ProgressViewModel ProgressVM => ProgressViewModel.GetInstance();

        public abstract MetaDataSourceType Type { get; }

        public abstract bool IsValidSource(SourceSet sources);

        /// <param name="sources">The sources to get metadata for</param>
        /// <param name="availableTags">Tags that sources can choose from</param>
        /// <returns></returns>
        public abstract Task<SourceMetaData> GetMetaData(SourceSet sources, IList<Tag> availableTags);

        public abstract Task<IMagickImage<byte>?> FetchCover(SourceSet sources);
        #endregion

        protected virtual List<Tag> GetMatchingTags(SourceSet sources, IList<Tag> availableTags)
        {
            var autoLinkEnabled = Preferences.Preferences.AutoLinkFolderTagSameName;
            List<Tag> matchingTags = new();

            // Tags based on file path
            foreach (Tag tag in availableTags)
            {
                List<string> globs = [..tag.LinkedGlobs];

                if(autoLinkEnabled)
                {
                    globs.AddRange($"**/{tag.Name}/**");
                }

                if (PathUtils.MatchesAnyGlob(sources.Path, globs))
                {
                    matchingTags.Add(tag);
                }

                if (PathUtils.MatchesAnyGlob(sources.SourceURL, globs))
                {
                    matchingTags.Add(tag);
                }
            }

            return matchingTags;
        }
    }
}
