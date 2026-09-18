using Avalonia.Platform.Storage;
using COMPASS.Common.Interfaces.Services;
using COMPASS.Infra.Tools.Logging;
using COMPASS.Infra.Models.Progress;

namespace COMPASS.Common.Services.FileSystem
{
    public abstract class IOServiceBase(IFilesService filesService, ILogger logger) : IIOService
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
            string path = folder.Path.LocalPath;

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
            var paths = folders.Select(f => f.Path.LocalPath).ToList();

            //Dispose the handles for now and just keep the paths, might need to hold on to this later
            foreach (var folder in folders)
            {
                folder.Dispose();
            }

            return paths;
        }

        #endregion
        
        #region Manipulate data on disk

        public async Task<bool> CopyDataAsync(string sourceDir, string destDir, IProgress<IProgressReport>? progressTracker = null, CancellationToken cancellationToken = default)
        {
            try
            {
                string[] filesToCopy = await Task.Run(() => Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories), cancellationToken);
                progressTracker?.Report(ProgressReports.XOutOfY(0, filesToCopy.Length));

                //Create all the directories
                foreach (string dirPath in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
                {
                    Directory.CreateDirectory(dirPath.Replace(sourceDir, destDir));
                }

                //Copy all the files & replace any files with the same name
                await Task.Run(() =>
                {
                    foreach (string sourcePath in filesToCopy)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        // don't copy log files, causes error because log file is open
                        bool skippedOpenLog = Path.GetExtension(sourcePath) == ".log";
                        if (!skippedOpenLog)
                        {
                            File.Copy(sourcePath, sourcePath.Replace(sourceDir, destDir), true);
                            logger.Info($"Copied {sourcePath}");
                        }

                        progressTracker?.Report(ProgressReports.Increment);
                    }
                }, cancellationToken).ConfigureAwait(false);

                return true;
            }
            catch (OperationCanceledException)
            {
                //let caller decide what to do with the cancellation
                throw;
            }
            catch (Exception ex)
            {
                logger.Error($"Could not move data to {destDir}", ex);
                return false;
            }
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
                    logger.Error($"Failed to create required folders for path {path}", ex);
                    return false;
                }
            }

            return true;
        }
        
        /// <inheritdoc/>
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
                    logger.Error($"Failed to get files of folder {path}", ex);
                }
            }

            return files;
        }

        /// <inheritdoc/>
        public IEnumerable<string> TryGetDirectories(string directory)
        {
            try
            {
                return Directory.GetDirectories(directory);
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to get subfolders of {directory}", ex);
                return Enumerable.Empty<string>();
            }

        }
    }
}
