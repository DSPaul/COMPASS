using Autofac;
using Avalonia.Platform.Storage;
using COMPASS.Common.Interfaces;
using COMPASS.Common.Models;
using COMPASS.Common.Models.Enums;
using COMPASS.Common.Tools;
using COMPASS.Common.ViewModels;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Microsoft.Extensions.FileSystemGlobbing;

namespace COMPASS.Common.Services.FileSystem
{
    public static class IOService
    {
        #region Web related

        //Download data and put it in a byte[]
        public static async Task<byte[]> DownloadFileAsync(string uri)
        {
            using HttpClient client = new();
            // Set headers to mimic a browser, gets around some auth issues
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/90.0.4430.93 Safari/537.36");

            if (!Uri.TryCreate(uri, UriKind.Absolute, out Uri? _))
                throw new InvalidOperationException("URI is invalid.");
            try
            {
                return await client.GetByteArrayAsync(uri);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to fetch data at {uri}", ex);
                return [];
            }
        }

        public static async Task<JObject?> GetJsonAsync(string uri)
        {
            using HttpClient client = new();

            JObject? json = null;

            if (!Uri.TryCreate(uri, UriKind.Absolute, out Uri? _))
                throw new InvalidOperationException("URI is invalid.");
            try
            {
                HttpResponseMessage response = await client.GetAsync(uri);
                if (response.IsSuccessStatusCode)
                {
                    var data = response.Content.ReadAsStringAsync();
                    json = JObject.Parse(data.Result);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to fetch data at {uri}", ex);
            }

            return json;
        }

        public static async Task<HtmlDocument?> ScrapeSite(string url)
        {
            HtmlWeb web = new();
            HtmlDocument doc;

            var progressVM = ProgressViewModel.GetInstance();

            try
            {
                doc = await Task.Run(() => web.Load(url));
            }

            catch (Exception ex)
            {
                //fails if URL could not be loaded
                progressVM.AddLogEntry(new(Severity.Error, ex.Message));
                Logger.Error($"Could not load {url}", ex);
                return null;
            }

            if (doc.ParsedText is null || doc.DocumentNode is null)
            {
                LogEntry entry = new(Severity.Error, $"Failed to reach {url}");
                progressVM.AddLogEntry(entry);
                Logger.Error($"{url} does not have any content", new Exception());
                return null;
            }
            else
            {
                return doc;
            }
        }

        //check internet connection
        private static bool _showedOfflineWarning = false;

        public static bool PingURL(string url = "8.8.8.8")
        {
            Ping p = new();
            try
            {
                PingReply reply = p.Send(url, 3000);
                if (reply.Status != IPStatus.Success) return false;
                if (_showedOfflineWarning)
                {
                    const string msg = "Internet connection restored";
                    Logger.Info(msg);
                    Logger.FileLog?.Info(msg);
                    _showedOfflineWarning = false;
                }

                return true;
            }
            catch (Exception ex)
            {
                if (_showedOfflineWarning == false)
                {
                    Logger.Warn($"Could not ping {url}", ex);
                }

                _showedOfflineWarning = true;
            }

            return false;
        }

        #endregion

        #region Tmp data
        
        public static void ClearTmpData(string? tempPath = null)
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

        #region File formats

        public static bool IsImageFile(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            string extension = Path.GetExtension(path).ToLower();
            List<string> imgExtensions =
            [
                ".png",
                ".jpg",
                ".jpeg",
                ".webp"
            ];

            return imgExtensions.Contains(extension);
        }

        public static bool IsPDFFile(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            return Path.GetExtension(path).ToLower() == ".pdf";
        }

        #endregion

        #region Dialogs/explorer

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <exception cref="ArgumentException"></exception>
        public static void ShowInExplorer(string path)
        {
            if (!Path.Exists(path))
            {
                throw new ArgumentException("path does not exist");
            }

            ProcessStartInfo startInfo = new()
            {
                Arguments = path,
                FileName = "explorer.exe" //TODO THIS IS WINDOWS ONLY
            };
            Process.Start(startInfo);
        }

        /// <summary>
        /// Allow the user to select a single folder using a dialog
        /// </summary>
        /// <returns> the selected path, null if canceled/failed / whatever </returns>
        public static async Task<string?> PickFolder()
        {
            var filesService = App.Container.Resolve<IFilesService>();

            IList<IStorageFolder> folders = await filesService.OpenFoldersAsync();

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
        public static async Task<IList<string>> TryPickFolders()
        {
            var filesService = App.Container.Resolve<IFilesService>();

            FolderPickerOpenOptions options = new()
            {
                AllowMultiple = true,
            };

            IList<IStorageFolder> folders = await filesService.OpenFoldersAsync(options);

            if (folders.Count == 0) return [];
            var paths = folders.Select(f => f.Path.AbsolutePath).ToList();

            //Dispose the handles for now and just keep the paths, might need to hold on to this later
            foreach (var folder in folders)
            {
                folder.Dispose();
            }

            return paths;
        }

        /// <summary>
        /// If the existing codex path is inaccessible for any reason, prompt the user to pick another one
        /// </summary>
        /// <returns></returns>
        public static async Task AskNewCompassDataPath(string msg)
        {
            var windowedNotificationService =
                App.Container.Resolve<INotificationService>();

            Notification pickNewPath = new("Pick a location to save your data", msg, Severity.Warning)
            {
                ConfirmText = "Continue"
            };
            await windowedNotificationService.ShowDialog(pickNewPath);

            bool success = false;
            while (!success)
            {
                string? newPath = await PickFolder();
                if (!string.IsNullOrWhiteSpace(newPath) && Path.Exists(newPath))
                {
                    success = await SettingsViewModel.GetInstance().SetNewDataPath(newPath);
                }
                else
                {
                    Notification notValid = new("Invalid path", $"{newPath} is not a valid path, please try again",
                        Severity.Warning);
                    await windowedNotificationService.ShowDialog(notValid);
                }
            }
        }

        #endregion

        #region Path matching

        /// <summary>
        /// Get the longest common path shared by all the given paths
        /// </summary>
        /// <param name="paths"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static string GetCommonFolder(List<string> paths)
        {
            if (paths is null) throw new ArgumentNullException(nameof(paths));
            if (paths.Count == 0) return string.Empty;

            string reference = paths.First();
            string[] folders = reference.Split(Path.DirectorySeparatorChar);
            string commonFolder = "";
            foreach (string folder in folders)
            {
                string nextFolderToTest = Path.Combine(commonFolder, folder);
                if (paths.All(path => path.StartsWith(nextFolderToTest)))
                {
                    commonFolder = nextFolderToTest;
                }
                else
                {
                    break;
                }
            }

            return commonFolder;
        }

        /// <summary>
        /// Returns the paths without the longest common end
        /// </summary>
        /// <param name="path1"></param>
        /// <param name="path2"></param>
        /// <returns></returns>
        public static (string, string) GetDifferingRoot(in string path1, in string path2)
        {
            if (path1 is null)
            {
                throw new ArgumentNullException(nameof(path1));
            }

            if (path2 is null)
            {
                throw new ArgumentNullException(nameof(path2));
            }

            if (path1 == path2)
            {
                return (path1, path2);
            }

            var dirs1 = path1.Split(Path.DirectorySeparatorChar);
            var dirs2 = path2.Split(Path.DirectorySeparatorChar);

            int shortestLength = Math.Min(dirs1.Length, dirs2.Length);

            int commonDirs = 0;
            while (commonDirs < shortestLength //while we have not iterated over the entire shortest array
                   && dirs1.TakeLast(commonDirs + 1)
                       .SequenceEqual(dirs2.TakeLast(commonDirs + 1))) //and the last x elements are the same
            {
                commonDirs++;
            }

            string remainingPath1 = string.Join(Path.DirectorySeparatorChar, dirs1.Take(dirs1.Length - commonDirs));
            string remainingPath2 = string.Join(Path.DirectorySeparatorChar, dirs2.Take(dirs2.Length - commonDirs));

            return (remainingPath1, remainingPath2);
        }

        public static bool MatchesAnyGlob(string filePath, IList<string> globs )
        {
            if (string.IsNullOrEmpty(filePath)) return false;
            
            var matcher = new Matcher();
            matcher.AddIncludePatterns(globs);
            
            string directory = Path.GetDirectoryName(filePath) ?? string.Empty;
            string fileName = Path.GetFileName(filePath);
            
            return matcher.Match(directory, fileName).HasMatches;
        }

        #endregion

        /// <summary>
        /// Tries to create the required directories for the given path
        /// </summary>
        /// <param name="path"></param>
        public static bool EnsureDirectoryExists(string path)
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
        public static IEnumerable<string> TryGetFilesInFolder(string path)
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