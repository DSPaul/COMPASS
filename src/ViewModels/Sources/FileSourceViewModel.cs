﻿using COMPASS.Models;
using COMPASS.Services;
using COMPASS.Tools;
using FuzzySharp;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace COMPASS.ViewModels.Sources
{
    public class FileSourceViewModel : SourceViewModel
    {
        public FileSourceViewModel()
        {
            _preferencesService = PreferencesService.GetInstance();
        }

        private PreferencesService _preferencesService;
        public override MetaDataSource Source => MetaDataSource.File;

        public override Task<bool> FetchCover(Codex codex) => throw new System.NotImplementedException();
        public override bool IsValidSource(Codex codex) => codex.HasOfflineSource();

        public override Task<Codex> GetMetaData(Codex codex)
        {
            // Work on a copy
            codex = new Codex(codex);

            // Title
            codex.Title = Path.GetFileNameWithoutExtension(codex.Path);

            // Tags based on file path
            foreach (var folderTagPair in TargetCollection.Info.FolderTagPairs)
            {
                Debug.Assert(IsValidSource(codex), "Codex without path was referenced in file source");
                if (codex.Path!.Contains(folderTagPair.Folder))
                {
                    App.SafeDispatcher.Invoke(() => codex.Tags.AddIfMissing(folderTagPair.Tag!));
                }
            }

            if (_preferencesService.Preferences.AutoLinkFolderTagSameName)
            {
                foreach (Tag tag in MainViewModel.CollectionVM.CurrentCollection.AllTags)
                {
                    var splitFolders = codex.Path!.Split("\\");
                    if (splitFolders.Any(folder => Fuzz.Ratio(folder.ToLowerInvariant(), tag.Content.ToLowerInvariant()) > 90))
                    {
                        App.SafeDispatcher.Invoke(() => codex.Tags.AddIfMissing(tag));
                    }
                }
            }

            MainViewModel.CollectionVM.FilterVM.ReFilter();
            return Task.FromResult(codex);
        }
    }
}
