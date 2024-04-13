using COMPASS.Common.Commands;
using COMPASS.Common.Models;
using COMPASS.Common.Services;
using COMPASS.Common.Tools;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Media;

namespace COMPASS.Common.ViewModels
{
    public class FileNotFoundViewModel
    {
        public FileNotFoundViewModel(Codex codex)
        {
            _codex = codex;
            SystemSounds.Exclamation.Play();
        }

        private Codex _codex;

        private ActionCommand? _findFileCommand;
        public ActionCommand FindFileCommand => _findFileCommand ??= new(FindFile);
        public void FindFile()
        {
            OpenFileDialog openFileDialog = new()
            {
                AddExtension = false,
            };

            if (openFileDialog.ShowDialog() == true)
            {
                //find the replaced parh of the path
                string oldPath = _codex.Path;
                string newPath = openFileDialog.FileName;
                var (toReplace, replaceWith) = IOService.GetDifferingRoot(oldPath, newPath);

                //fix the path of this codex
                _codex.Path = openFileDialog.FileName;
                int fixedRefs = 1;

                //try to fix the path of all codices
                var codicesWithBrokenPaths = MainViewModel.CollectionVM.CurrentCollection.AllCodices
                    .Where(c => c.HasOfflineSource() && !File.Exists(c.Path))
                    .ToList();
                foreach (var c in codicesWithBrokenPaths)
                {
                    string possiblePath = Path.Combine(replaceWith, c.Path[toReplace.Length..]);
                    if (File.Exists(possiblePath))
                    {
                        c.Path = possiblePath;
                        fixedRefs++;
                    }
                }

                string message = $"Fixed {fixedRefs} broken references based on recent manual fix, {codicesWithBrokenPaths.Count - fixedRefs + 1} remaining.";
                Logger.Info(message);
                Logger.Debug(message);

                MainViewModel.CollectionVM.CurrentCollection.SaveCodices();
                CodexViewModel.OpenCodexLocally(_codex);
                SetDialogResult?.Invoke(true);
                CloseAction?.Invoke();
            }
        }

        private ActionCommand? _removePathCommand;
        public ActionCommand RemovePathCommand => _removePathCommand ??= new(RemovePath);
        private void RemovePath()
        {
            _codex.Path = "";
            CloseAction?.Invoke();
        }

        private ActionCommand? _deleteCodexCommand;
        public ActionCommand DeleteCodexCommand => _deleteCodexCommand ??= new(DeleteCodex);
        private void DeleteCodex()
        {
            CodexViewModel.DeleteCodex(_codex);
            SetDialogResult?.Invoke(true);
            CloseAction?.Invoke();
        }

        public Action? CloseAction;
        public Action<bool>? SetDialogResult;
    }
}
