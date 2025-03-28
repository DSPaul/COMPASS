using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Models.XmlDtos;
using COMPASS.Common.Services;
using COMPASS.Common.Tools;
using FuzzySharp;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using COMPASS.Common.Services.FileSystem;

namespace COMPASS.Common.ViewModels.Sources
{
    public class FileSourceViewModel : SourceViewModel
    {
        public FileSourceViewModel()
        {
            _preferencesService = PreferencesService.GetInstance();
        }

        private readonly PreferencesService _preferencesService;
        public override MetaDataSource Source => MetaDataSource.File;

        public override Task<bool> FetchCover(Codex codex) => throw new System.NotImplementedException();
        public override bool IsValidSource(SourceSet sources) => sources.HasOfflineSource();

        public override Task<CodexDto> GetMetaData(SourceSet sources)
        {
            // Use a codex dto to transfer the data
            CodexDto codex = new()
            {
                Title = Path.GetFileNameWithoutExtension(sources.Path)
            };

            // Tags based on file path
            foreach (Tag tag in TargetCollection.AllTags)
            {
                if (IOService.MatchesAnyGlob(sources.Path, tag.LinkedGlobs))
                {
                    codex.TagIDs.AddIfMissing(tag.ID);
                }
            }

            if (_preferencesService.Preferences.AutoLinkFolderTagSameName)
            {
                foreach (Tag tag in MainViewModel.CollectionVM.CurrentCollection.AllTags)
                {
                    var splitFolders = sources.Path.Split("\\");
                    if (splitFolders.Any(folder => Fuzz.Ratio(folder.ToLowerInvariant(), tag.Name.ToLowerInvariant()) > 90))
                    {
                        codex.TagIDs.AddIfMissing(tag.ID);
                    }
                }
            }

            MainViewModel.CollectionVM.FilterVM.ReFilter();
            return Task.FromResult(codex);
        }
    }
}
