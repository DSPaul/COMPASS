using Autofac;
using COMPASS.Interfaces;
using COMPASS.Models;
using COMPASS.Models.Enums;
using COMPASS.Tools;
using COMPASS.ViewModels;
using HtmlAgilityPack;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using Ookii.Dialogs.Wpf;
using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace COMPASS.Services
{
    public static class IOService
    {

        #region Web related

        //Download data and put it in a byte[]
        public static async Task<byte[]> DownloadFileAsync(string uri)
        {
            using HttpClient client = new();

            if (!Uri.TryCreate(uri, UriKind.Absolute, out Uri? _)) throw new InvalidOperationException("URI is invalid.");

            return await client.GetByteArrayAsync(uri);
        }
        public static async Task<JObject?> GetJsonAsync(string uri)
        {
            using HttpClient client = new();

            JObject? json = null;

            if (!Uri.TryCreate(uri, UriKind.Absolute, out Uri? _)) throw new InvalidOperationException("URI is invalid.");

            HttpResponseMessage response = await client.GetAsync(uri);
            if (response.IsSuccessStatusCode)
            {
                var data = response.Content.ReadAsStringAsync();
                json = JObject.Parse(data.Result);
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
                if (reply?.Status == IPStatus.Success)
                {
                    if (_showedOfflineWarning)
                    {
                        const string msg = "Internet connection restored";
                        Logger.Info(msg);
                        Logger.FileLog?.Info(msg);
                        _showedOfflineWarning = false;
                    }
                    return true;
                }
                return false;
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

        #region (De)Serialization

        /// <summary>
        /// Unzips a collection stored in a satchel file
        /// </summary>
        /// <param name="path">Path to the sathcel file</param>
        /// <returns>Path to unzipped folder</returns>
        public static async Task<string> UnZipCollection(string path)
        {
            string fileName = Path.GetFileName(path);
            string tmpCollectionPath = Path.Combine(CodexCollection.CollectionsPath, $"__{fileName}");

            //make sure any previous temp data is gone
            ClearTmpData(tmpCollectionPath);

            //unzip the file to tmp folder
            using ZipArchive archive = ZipArchive.Open(path);

            //report progress
            var progressVM = ProgressViewModel.GetInstance();
            progressVM.Text = $"Reading {path}";
            progressVM.ResetCounter();
            progressVM.TotalAmount = 1;

            //extract
            await Task.Run(() => archive.ExtractToDirectory(tmpCollectionPath, progressReport: new Action<double>(progressVM.UpdateFromPercentage)));

            progressVM.IncrementCounter();
            return tmpCollectionPath;
        }

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
            if (String.IsNullOrEmpty(path)) { return false; }
            string extension = Path.GetExtension(path).ToLower();
            List<string> imgExtensions = new()
            {
                ".png",
                ".jpg",
                ".jpeg",
                ".webp"
            };

            return imgExtensions.Contains(extension);
        }

        public static bool IsPDFFile(string path)
        {
            if (String.IsNullOrEmpty(path)) { return false; }
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
                FileName = "explorer.exe"
            };
            Process.Start(startInfo);
        }

        /// <summary>
        /// Allow the user to select a single folder using a dialog
        /// </summary>
        /// <returns> the selected path, null if canceled/failed / whatever </returns>
        public static string? PickFolder()
        {
            VistaFolderBrowserDialog openFolderDialog = new();
            var dialogResult = openFolderDialog.ShowDialog();
            if (dialogResult == false) return null;
            return openFolderDialog.SelectedPath;
        }

        /// <summary>
        /// Allow the user to select multiple folders using a dialog
        /// </summary>
        /// <param name="selectedPaths"> the selected Paths </param>
        /// <returns> Ilist with selected paths, empty list if canceled/failed / whatever </returns>
        public static bool TryPickFolders(out string[] selectedPaths)
        {
            VistaFolderBrowserDialog openFolderDialog = new()
            {
                Multiselect = true,
            };
            var dialogResult = openFolderDialog.ShowDialog();
            selectedPaths = openFolderDialog.SelectedPaths;
            return dialogResult == true;
        }

        public static async Task<CodexCollection?> OpenSatchel(string? path = null)
        {
            if (path == null)
            {
                //ask for satchel file using fileDialog
                OpenFileDialog openFileDialog = new()
                {
                    Filter = Constants.SatchelExtensionFilter,
                    CheckFileExists = true,
                    Multiselect = false,
                    Title = "Choose a COMPASS Satchel file to import",
                };

                if (openFileDialog.ShowDialog() != true) return null;
                path = openFileDialog.FileName;
            }

            var windowedNotificationService = App.Container.ResolveKeyed<INotificationService>(NotificationDisplayType.Windowed);

            //Check compatibility
            using (ZipArchive archive = ZipArchive.Open(path))
            {
                var satchelInfoFile = archive.Entries.SingleOrDefault(entry => entry.Key == Constants.SatchelInfoFileName);
                if (satchelInfoFile == null)
                {
                    //No version information means we cannot ensure compatibility, so abort
                    string message = $"Cannot import {Path.GetFileName(path)} because it does not contain version info, and might therefor not be compatible with your version v{Reflection.Version}.";
                    Logger.Warn(message);
                    Notification warnNotification = new($"Could not import {Path.GetFileName(path)}", message, Severity.Warning);
                    windowedNotificationService.Show(warnNotification);
                    return null;
                }

                //Read the file contents
                using var stream = new MemoryStream();
                satchelInfoFile.WriteTo(stream);
                stream.Seek(0, SeekOrigin.Begin);
                using StreamReader reader = new(stream);
                string json = reader.ReadToEnd();

                var satchelInfo = JsonSerializer.Deserialize<SatchelInfo>(json);
                if (satchelInfo == null)
                {
                    //No version information means we cannot ensure compatibility, so abort
                    string message = $"Cannot import {Path.GetFileName(path)} because it does not contain version info, and might therefor not be compatible with your version v{Reflection.Version}.";
                    Logger.Warn(message);
                    Notification warnNotification = new($"Could not import {Path.GetFileName(path)}", message, Severity.Warning);
                    windowedNotificationService.Show(warnNotification);
                    return null;
                }

                var currentVersion = Assembly.GetExecutingAssembly().GetName().Version!;
                var minVersions = new List<Version> { currentVersion }; //keep a list of min requirements

                var filesInZip = archive.Entries.Select(entry => entry.Key).ToList();

                //Check Codex version
                if (filesInZip.Contains(Constants.CodicesFileName))
                {
                    Version minCodexVersion = Version.Parse(satchelInfo.MinCodexInfoVersion);
                    minVersions.Add(minCodexVersion);
                }

                //Check tags version
                if (filesInZip.Contains(Constants.TagsFileName))
                {
                    Version minVersion = Version.Parse(satchelInfo.MinTagsVersion);
                    minVersions.Add(minVersion);
                }

                //Check collection info version
                if (filesInZip.Contains(Constants.CollectionInfoFileName))
                {
                    Version minVersion = Version.Parse(satchelInfo.MinCollectionInfoVersion);
                    minVersions.Add(minVersion);
                }

                //current version must exceed all min versions
                if (minVersions.Max() > currentVersion)
                {
                    string message = $"Cannot import {Path.GetFileName(path)} because it was created in a newer version of COMPASS (v{satchelInfo.CreationVersion}), " +
                        $"and has indicated to be incompatible with your version v{Reflection.Version}. Please update and try again.";
                    Logger.Warn(message);
                    Notification warnNotification = new($"Could not import {Path.GetFileName(path)}", message, Severity.Warning);
                    windowedNotificationService.Show(warnNotification);
                    return null;
                }
            }

            //unzip the file
            try
            {
                string unzipLocation = await UnZipCollection(path);
                return new(Path.GetFileName(unzipLocation));
            }
            catch (Exception ex)
            {
                Logger.Warn($"Failed to read {path}", ex);
                return null;
            }
        }
        #endregion

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
            while (commonDirs < shortestLength                                    //while we have not iterated over the entire shortest array
                && dirs1.TakeLast(commonDirs + 1).SequenceEqual(dirs2.TakeLast(commonDirs + 1)))  //and the last x elements are the same
            {
                commonDirs++;
            }

            string remainingPath1 = String.Join(Path.DirectorySeparatorChar, dirs1.Take(dirs1.Length - commonDirs));
            string remainingPath2 = String.Join(Path.DirectorySeparatorChar, dirs2.Take(dirs2.Length - commonDirs));

            return (remainingPath1, remainingPath2);
        }
    }
}
