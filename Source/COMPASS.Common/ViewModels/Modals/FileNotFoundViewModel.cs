using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.DependencyInjection;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Interfaces.ViewModels;
using COMPASS.Common.Models;
using COMPASS.Common.Operations;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Infra.Tools;

namespace COMPASS.Common.ViewModels.Modals
{
    public class FileNotFoundViewModel : ViewModelBase, IModalViewModel
    {

        private readonly ILogger _logger;
        private readonly IFilesService _filesService;
        private readonly CodexOperations _codexOperations;
        public FileNotFoundViewModel(ILogger logger, IFilesService filesService, CodexOperations codexOperations, Codex codex)
        {
            _logger = logger;
            _filesService = filesService;
            _codexOperations = codexOperations;
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
            var files = await _filesService.OpenFilesAsync();

            if (files.Any())
            {
                using var file = files.Single();
                
                //find the replaced part of the path
                string oldPath = Codex.Sources.Path;
                string newPath = file.Path.LocalPath;
                var (toReplace, replacement) = PathUtils.GetDifferingRoot(oldPath, newPath);

                //fix the path of this codex
                Codex.Sources.Path = newPath;
                int fixedRefs = 1;

                //try to fix the path of all codices in this collection
                var codicesWithBrokenPaths = Codex.Collection.AllCodices
                    .Where(c => c.Sources.HasOfflineSource() && !File.Exists(c.Sources.Path))
                    .ToList();
                
                foreach (var c in codicesWithBrokenPaths)
                {
                    if (PathUtils.IsPathInsideDirectory(c.Sources.Path, toReplace))
                    {
                        string possiblePath = Path.Combine(replacement, c.Sources.Path[toReplace.Length..]);
                        if (File.Exists(possiblePath))
                        {
                            c.Sources.Path = possiblePath;
                            fixedRefs++;
                        }
                    }
                }

                string message = $"Fixed {fixedRefs} broken references based on recent manual fix, {codicesWithBrokenPaths.Count - fixedRefs + 1} remaining.";
                _logger.Info(message);
                _logger.Debug(message);
                
                Codex.Collection.SaveCodices();
                bool opened = await _codexOperations.OpenCodexLocally(Codex);
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
            await _codexOperations.DeleteCodex(Codex);
            MarkAsResolved(false);
        }
        
        private RelayCommand? _doNothingCommand;
        public RelayCommand DoNothingCommand => _doNothingCommand ??= new(DoNothing);
        private void DoNothing()
        {
            MarkAsResolved(false);
        }
        
        public string WindowTitle => "File Not Found";
        public Action CloseAction { get; set; } = () => { };
    }

    [Factory]
    public class FileNotFoundViewModelFactory(ILogger logger, IFilesService filesService, CodexOperations codexOperations)
    {
        public FileNotFoundViewModel Create(Codex codex) => new(logger, filesService, codexOperations, codex);
    }
}
