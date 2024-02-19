using COMPASS.Commands;
using COMPASS.Models;
using Microsoft.Win32;
using System;
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
                _codex.Path = openFileDialog.FileName;
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
