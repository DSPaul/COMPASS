using COMPASS.Models;
using COMPASS.Tools;
using COMPASS.Windows;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace COMPASS.ViewModels.Import
{
    public static class ImportViewModel
    {
        public static void Import(ImportSource source) => Import(source, MainViewModel.CollectionVM.CurrentCollection);
        public static void Import(ImportSource source, CodexCollection targetCollection)
        {
            List<string> pathsToImport;
            switch (source)
            {
                case ImportSource.File:
                    pathsToImport = ChooseFiles();
                    ImportFiles(pathsToImport, targetCollection);
                    break;

                case ImportSource.Folder:
                    pathsToImport = new ImportFolderViewModel(targetCollection).ChooseFolders();
                    ImportFiles(pathsToImport, targetCollection);
                    break;
                case ImportSource.Manual:
                    ImportManual();
                    break;
                case ImportSource.GmBinder:
                case ImportSource.Homebrewery:
                case ImportSource.GoogleDrive:
                case ImportSource.GenericURL:
                case ImportSource.ISBN:
                    ImportURL(source);
                    break;
            };
        }

        public static bool Stealth = true;

        public static List<string> ChooseFiles()
        {
            OpenFileDialog openFileDialog = new()
            {
                AddExtension = false,
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == false) return new();
            return openFileDialog.FileNames.ToList();
        }

        public static void ImportManual()
        {
            CodexEditWindow editWindow = new(new CodexEditViewModel(null));
            editWindow.ShowDialog();
            editWindow.Topmost = true;
        }

        public static void ImportURL(ImportSource source)
        {
            ImportURLViewModel importVM = new(source);
            ImportURLWindow window = new(importVM);
            window.Show();
        }

        public static async void ImportFiles(List<string> paths, CodexCollection targetCollection = null)
        {
            targetCollection ??= MainViewModel.CollectionVM.CurrentCollection;

            //filter out files already in collection & banned paths
            IEnumerable<string> existingPaths = targetCollection.AllCodices.Select(codex => codex.Path);
            paths = paths
                .Except(existingPaths)
                .Except(targetCollection.Info.BanishedPaths)
                .ToList();

            var ProgressVM = ProgressViewModel.GetInstance();

            ProgressVM.TotalAmount = paths.Count;
            ProgressVM.ResetCounter();
            ProgressVM.Text = "Importing files";

            if (paths.Count == 0) return;

            List<Codex> newCodices = new();

            if (!Stealth)
            {
                ProgressWindow window = new()
                {
                    Owner = Application.Current.MainWindow
                };
                window.Show();
            }

            await Task.Run(() =>
            {
                //make new codices synchonously so they all have a valid ID
                foreach (string path in paths)
                {
                    Codex newCodex = new(targetCollection) { Path = path };
                    newCodices.Add(newCodex);
                    targetCollection.AllCodices.Add(newCodex);

                    LogEntry logEntry = new(LogEntry.MsgType.Info, $"Importing {path}");
                    ProgressVM.IncrementCounter();
                    ProgressVM.AddLogEntry(logEntry);
                }
                MainViewModel.CollectionVM.CurrentCollection.Save();
            });

            await FinishImport(newCodices);
            Stealth = true;
        }

        public static async Task FinishImport(List<Codex> newCodices)
        {
            var ProgressVM = ProgressViewModel.GetInstance();
            ProgressVM.TotalAmount = newCodices.Count;

            await Task.Run(() => CodexViewModel.GetMetaData(newCodices));

            ProgressVM.ResetCounter();
            ProgressVM.Text = "Getting MetaData";
            await CoverFetcher.GetCover(newCodices);

            foreach (Codex codex in newCodices) codex.RefreshThumbnail();
        }
    }
}
