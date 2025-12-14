using Avalonia.Platform.Storage;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Services.StateManagers;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels;
using COMPASS.Common.Views.Windows;

namespace COMPASS.Common.Services.FileSystem
{
    public abstract class IOServiceBase(IFilesService filesService) : IIOService
    {
        #region Tmp data
        
        public void ClearTmpData(string? tempPath = null)
        {
            if (tempPath == null)
            {
                //TODO: find all paths with __ which are temp and delete them
            }

            if (Directory.Exists(tempPath))
            {
                Directory.Delete(tempPath, true);
            }
        }

        #endregion
        
        #region Dialogs/explorer
        
        public abstract void ShowInExplorer(string filePath);

        /// <summary>
        /// Allow the user to select a single folder using a dialog
        /// </summary>
        /// <returns> the selected path, null if canceled/failed / whatever </returns>
        public async Task<string?> PickFolder()
        {
            IList<IStorageFolder> folders = await filesService.OpenFoldersAsync().ConfigureAwait(false);

            if (folders.Count == 0) return null;

            var folder = folders.Single();
            string path = folder.Path.AbsolutePath;

            //Dispose the handle for now and just keep the path, might need to hold on to this later
            folder.Dispose();

            return path;
        }

        /// <summary>
        /// Allow the user to select multiple folders using a dialog
        /// </summary>
        /// <returns> IList with selected paths, empty list if canceled/failed / whatever </returns>
        public async Task<IList<string>> TryPickFolders()
        {
            FolderPickerOpenOptions options = new()
            {
                AllowMultiple = true,
            };

            IList<IStorageFolder> folders = await filesService.OpenFoldersAsync(options).ConfigureAwait(false);

            if (folders.Count == 0) return [];
            var paths = folders.Select(f => f.Path.AbsolutePath).ToList();

            //Dispose the handles for now and just keep the paths, might need to hold on to this later
            foreach (var folder in folders)
            {
                folder.Dispose();
            }

            return paths;
        }

        #endregion
        
        #region Manipulate data on disk

        public async Task<bool> CopyDataAsync(string sourceDir, string destDir)
        {
            ProgressViewModel progressVM = ProgressViewModel.GetInstance();
            ProgressWindow progressWindow = new();

            var toCopy = Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories);

            progressVM.TotalAmount = toCopy.Length;
            progressVM.ResetCounter();
            progressVM.Text = "Copying Files";

            progressWindow.Show(WindowManager.ActiveWindow);

            try
            {
                //Create all the directories
                foreach (string dirPath in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
                {
                    Directory.CreateDirectory(dirPath.Replace(sourceDir, destDir));
                }

                //Copy all the files & Replaces any files with the same name
                await Task.Run(() =>
                {
                    foreach (string sourcePath in toCopy)
                    {
                        ProgressViewModel.GlobalCancellationTokenSource.Token.ThrowIfCancellationRequested();

                        // don't copy log file, causes error because log file is open
                        if (Path.GetExtension(sourcePath) != ".log")
                        {
                            File.Copy(sourcePath, sourcePath.Replace(sourceDir, destDir), true);
                        }

                        progressVM.IncrementCounter();
                        progressVM.AddLogEntry(new LogEntry(Severity.Info, $"Copied {sourcePath}"));
                    }
                }).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex)
            {
                Logger.Warn($"Transfer was cancelled", ex);
                progressVM.ConfirmCancellation();
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error($"Could not move data to {destDir}", ex);
                progressVM.Clear();
                return false;
            }

            return true;
        }

        #endregion
        
        /// <summary>
        /// Tries to create the required directories for the given path
        /// </summary>
        /// <param name="path"></param>
        public bool EnsureDirectoryExists(string path)
        {
            var directory = Path.GetDirectoryName(path);

            if (string.IsNullOrEmpty(directory))
            {
                return false;
            }
            
            if (!Directory.Exists(directory))
            {
                try
                {
                    Directory.CreateDirectory(directory);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to create required folders for path {path}", ex);
                    return false;
                }
            }

            return true;
        }
        
        /// <summary>
        /// Safe alternative of <see cref="Directory.GetFiles(string)"/> that catches all exceptions
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public IEnumerable<string> TryGetFilesInFolder(string path)
        {
            IEnumerable<string> files = Enumerable.Empty<string>();
            if (Directory.Exists(path))
            {
                try
                {
                    files = Directory.GetFiles(path);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to get files of folder {path}", ex);
                }
            }

            return files;
        }
    }
}