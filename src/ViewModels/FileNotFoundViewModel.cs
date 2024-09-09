using CommunityToolkit.Mvvm.Input;
using COMPASS.Models;
using COMPASS.Services;
using COMPASS.Tools;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Media;

namespace COMPASS.ViewModels
{
    public class FileNotFoundViewModel
    {
        public FileNotFoundViewModel(Codex codex)
        {
            _codex = codex;
            SystemSounds.Exclamation.Play();
        }

        private Codex _codex;

        private RelayCommand? _findFileCommand;
        public RelayCommand FindFileCommand => _findFileCommand ??= new(FindFile);
        public void FindFile()
        {
            OpenFileDialog openFileDialog = new()
            {
                AddExtension = false,
            };

            if (openFileDialog.ShowDialog() == true)
            {
                //find the replaced parh of the path
                string oldPath = _codex.Sources.Path;
                string newPath = openFileDialog.FileName;
                var (toReplace, replaceWith) = IOService.GetDifferingRoot(oldPath, newPath);

                //fix the path of this codex
                _codex.Sources.Path = openFileDialog.FileName;
                int fixedRefs = 1;

                //try to fix the path of all codices
                var codicesWithBrokenPaths = MainViewModel.CollectionVM.CurrentCollection.AllCodices
                    .Where(c => c.Sources.HasOfflineSource() && !File.Exists(c.Sources.Path))
                    .ToList();
                foreach (var c in codicesWithBrokenPaths)
                {
                    string possiblePath = Path.Combine(replaceWith, c.Sources.Path[toReplace.Length..]);
                    if (File.Exists(possiblePath))
                    {
                        c.Sources.Path = possiblePath;
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
            CodexViewModel.DeleteCodex(_codex);
            SetDialogResult?.Invoke(true);
            CloseAction?.Invoke();
        }

        public Action? CloseAction;
        public Action<bool>? SetDialogResult;
    }
}
