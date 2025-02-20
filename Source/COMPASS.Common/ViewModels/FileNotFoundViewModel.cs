using Autofac;
using CommunityToolkit.Mvvm.Input;
using COMPASS.Common.Interfaces;
using COMPASS.Common.Models;
using COMPASS.Common.Operations;
using COMPASS.Common.Services.FileSystem;
using COMPASS.Common.Tools;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace COMPASS.Common.ViewModels
{
    public class FileNotFoundViewModel
    {
        public FileNotFoundViewModel(Codex codex)
        {
            _codex = codex;
        }

        private Codex _codex;

        private AsyncRelayCommand? _findFileCommand;
        public AsyncRelayCommand FindFileCommand => _findFileCommand ??= new(FindFile);
        public async Task FindFile()
        {
            var files = await App.Container.Resolve<IFilesService>().OpenFilesAsync();

            if (files.Any())
            {
                using var file = files.Single();
                //find the replaced part of the path
                string oldPath = _codex.Sources.Path;
                string newPath = file.Path.AbsolutePath;
                var (toReplace, replaceWith) = IOService.GetDifferingRoot(oldPath, newPath);

                //fix the path of this codex
                _codex.Sources.Path = newPath;
                int fixedRefs = 1;

                //try to fix the path of all codices
                var codicesWithBrokenPaths = MainViewModel.CollectionVM.CurrentCollection.AllCodices
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

                MainViewModel.CollectionVM.CurrentCollection.SaveCodices();
                CodexOperations.OpenCodexLocally(_codex);
                SetDialogResult?.Invoke(true);
                CloseAction?.Invoke();
            }
        }

        private RelayCommand? _removePathCommand;
        public RelayCommand RemovePathCommand => _removePathCommand ??= new(RemovePath);
        private void RemovePath()
        {
            _codex.Sources.Path = "";
            CloseAction?.Invoke();
        }

        private RelayCommand? _deleteCodexCommand;
        public RelayCommand DeleteCodexCommand => _deleteCodexCommand ??= new(DeleteCodex);
        private void DeleteCodex()
        {
            CodexOperations.DeleteCodex(_codex);
            SetDialogResult?.Invoke(true);
            CloseAction?.Invoke();
        }

        public Action? CloseAction;
        public Action<bool>? SetDialogResult;
    }
}
