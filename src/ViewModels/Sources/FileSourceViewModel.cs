using COMPASS.Models;
using COMPASS.Tools;
using FuzzySharp;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace COMPASS.ViewModels.Sources
{
    public class FileSourceViewModel : SourceViewModel
    {
        public FileSourceViewModel() : base() { }
        public override MetaDataSource Source => MetaDataSource.File;

        public override Task<bool> FetchCover(Codex codex) => throw new System.NotImplementedException();
        public override bool IsValidSource(Codex codex) => File.Exists(codex.Path);

        public override async Task<Codex> SetMetaData(Codex codex)
        {
            // Work on a copy
            codex = new Codex(codex);

            codex.Title = Path.GetFileNameWithoutExtension(codex.Path);
            MainViewModel.CollectionVM.FilterVM.ReFilter();
            return codex;
        }

        public override Codex SetTags(Codex codex)
        {
            // Work on a copy
            codex = new Codex(codex);

            //Auto Add Tags based on file path
            foreach (var folderTagPair in TargetCollection.Info.FolderTagPairs)
            {
                if (codex.Path.Contains(folderTagPair.Folder))
                {
                    Application.Current.Dispatcher.Invoke(() => codex.Tags.AddIfMissing(folderTagPair.Tag));
                }
            }

            if (Properties.Settings.Default.AutoLinkFolderTagSameName)
            {
                foreach (Tag tag in MainViewModel.CollectionVM.CurrentCollection.AllTags)
                {
                    var SplitFolders = codex.Path.Split("\\");
                    if (SplitFolders.Any(folder => Fuzz.Ratio(folder.ToLowerInvariant(), tag.Content.ToLowerInvariant()) > 90))
                    {
                        Application.Current.Dispatcher.Invoke(() => codex.Tags.AddIfMissing(tag));
                    }
                }
            }

            return codex;
        }
    }
}
