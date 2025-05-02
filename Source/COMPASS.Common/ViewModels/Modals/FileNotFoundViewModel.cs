using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.DependencyInjection;
using COMPASS.Common.Interfaces;
using COMPASS.Common.Interfaces.Storage;
using COMPASS.Common.Models;
using COMPASS.Common.Operations;
using COMPASS.Common.Services.FileSystem;
using COMPASS.Common.Tools;

namespace COMPASS.Common.ViewModels.Modals
{
    public class FileNotFoundViewModel : IModalViewModel
    {
        public FileNotFoundViewModel(Codex codex)
        {
            Codex = codex;
        }

        public Codex Codex { get; }
        
        public bool FixedAndOpenedCodex { get; private set; }

        private void MarkAsResolved(bool fixedAndOpenedCodex)
        {
            FixedAndOpenedCodex = fixedAndOpenedCodex;
            CloseAction();
        }
        
        private AsyncRelayCommand? _findFileCommand;
        public AsyncRelayCommand FindFileCommand => _findFileCommand ??= new(FindFile);
        public async Task FindFile()
        {
            var files = await ServiceResolver.Resolve<IFilesService>().OpenFilesAsync();

            if (files.Any())
            {
                using var file = files.Single();
                
                //find the replaced part of the path
                string oldPath = Codex.Sources.Path;
                string newPath = file.Path.AbsolutePath;
                var (toReplace, replaceWith) = IOService.GetDifferingRoot(oldPath, newPath);

                //fix the path of this codex
                Codex.Sources.Path = newPath;
                int fixedRefs = 1;

                //try to fix the path of all codices in this collection
                var codicesWithBrokenPaths = Codex.Collection.AllCodices
                    .Where(c => c.Sources.HasOfflineSource() && !File.Exists(c.Sources.Path))
                    .ToList();
                
                foreach (var c in codicesWithBrokenPaths)
                {
                    if (c.Sources.Path.StartsWith(toReplace))
                    {
                        string possiblePath = Path.Combine(replaceWith, c.Sources.Path[toReplace.Length..]);
                        if (File.Exists(possiblePath))
                        {
                            c.Sources.Path = possiblePath;
                            fixedRefs++;
                        }
                    }
                }

                string message = $"Fixed {fixedRefs} broken references based on recent manual fix, {codicesWithBrokenPaths.Count - fixedRefs + 1} remaining.";
                Logger.Info(message);
                Logger.Debug(message);

                ServiceResolver.Resolve<ICodexCollectionStorageService>().SaveCodices(Codex.Collection);
                bool opened = await CodexOperations.OpenCodexLocally(Codex);
                MarkAsResolved(opened);
            }
        }

        private RelayCommand? _removePathCommand;
        public RelayCommand RemovePathCommand => _removePathCommand ??= new(RemovePath);
        private void RemovePath()
        {
            Codex.Sources.Path = "";
            MarkAsResolved(false);
        }

        private AsyncRelayCommand? _deleteCodexCommand;
        public AsyncRelayCommand DeleteCodexCommand => _deleteCodexCommand ??= new(DeleteCodex);
        private async Task DeleteCodex()
        {
            await CodexOperations.DeleteCodex(Codex);
            MarkAsResolved(false);
        }
        
        private RelayCommand? _doNothingCommand;
        public RelayCommand DoNothingCommand => _doNothingCommand ??= new(DoNothing);
        private void DoNothing()
        {
            MarkAsResolved(false);
        }
        
        public string WindowTitle => "File Not Found";
        public int? WindowWidth => null;
        public int? WindowHeight => null;
        public Action CloseAction { get; set; } = () => { };
    }
}
