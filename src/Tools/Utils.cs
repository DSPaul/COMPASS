using COMPASS.Models;
using COMPASS.ViewModels;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace COMPASS.Tools
{
    public static class Utils
    {
        public static int GetAvailableID<T>(IEnumerable<T> collection) where T : IHasID
        {
            int tempID = 0;
            IList<int> usedIDs = collection.Select(x => x.ID).ToList();
            while (usedIDs.Contains(tempID))
            {
                tempID++;
            }
            return tempID;
        }

        //put all children of object in a flat enumerable
        public static IEnumerable<T> FlattenTree<T>(IEnumerable<T> l, string method = "dfs") where T : IHasChildren<T>
        {
            var result = l.ToList();

            switch (method)
            {
                //Breadth first search
                case "bfs":
                    {
                        for (int i = 0; i < result.Count; i++)
                        {
                            T parent = result[i];
                            result.AddRange(parent.Children);
                        }
                        break;
                    }
                //Depth first search (pre-order)
                case "dfs":
                    {
                        for (int i = 0; i < result.Count; i++)
                        {
                            T parent = result[i];
                            result.InsertRange(i + 1, parent.Children);
                        }
                        break;
                    }
            }

            return result;
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
                        Logger.FileLog.Info(msg);
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

        //Download data and put it in a byte[]
        public static async Task<byte[]> DownloadFileAsync(string uri)
        {
            using HttpClient client = new();

            if (!Uri.TryCreate(uri, UriKind.Absolute, out Uri _)) throw new InvalidOperationException("URI is invalid.");

            return await client.GetByteArrayAsync(uri);
        }

        public static async Task<JObject> GetJsonAsync(string uri)
        {
            using HttpClient client = new();

            JObject json = null;

            if (!Uri.TryCreate(uri, UriKind.Absolute, out Uri _)) throw new InvalidOperationException("URI is invalid.");

            HttpResponseMessage response = await client.GetAsync(uri);
            if (response.IsSuccessStatusCode)
            {
                var data = response.Content.ReadAsStringAsync();
                json = JObject.Parse(data.Result);
            }
            return json;
        }

        //based on https://seattlesoftware.wordpress.com/2008/09/11/hexadecimal-value-0-is-an-invalid-character/
        /// <summary>
        /// Remove illegal XML characters from a string.
        /// </summary>
        public static string SanitizeXmlString(string xml)
        {
            if (xml is null) { return null; }

            StringBuilder buffer = new(xml.Length);
            foreach (char c in xml.Where(c => IsLegalXmlChar(c)))
            {
                buffer.Append(c);
            }
            return buffer.ToString();
        }

        /// <summary>
        /// Whether a given character is allowed by XML 1.0.
        /// </summary>
        public static bool IsLegalXmlChar(int character) => character switch
        {
            0x9 => true, // '\t' == 9 
            0xA => true, // '\n' == 10         
            0xD => true, // '\r' == 13        
            (>= 0x20) and (<= 0xD7FF) => true,
            (>= 0xE000) and (<= 0xFFFD) => true,
            (>= 0x10000) and (<= 0x10FFFF) => true,
            _ => false
        };

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

        public static void ShowInExplorer(string path)
        {
            ProcessStartInfo startInfo = new()
            {
                Arguments = path,
                FileName = "explorer.exe"
            };
            Process.Start(startInfo);
        }

        public static string PickFolder()
        {
            VistaFolderBrowserDialog openFolderDialog = new();
            var dialogResult = openFolderDialog.ShowDialog();
            if (dialogResult == false) return null;
            return openFolderDialog.SelectedPath;
        }

        public static string GetCommonFolder(List<string> paths)
        {
            if (paths is null) throw new ArgumentNullException(nameof(paths));

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
                else break;
            }
            return commonFolder;
        }

        public static async Task<HtmlDocument> ScrapeSite(string url)
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
                progressVM.AddLogEntry(new(LogEntry.MsgType.Error, ex.Message));
                Logger.Error($"Could not load {url}", ex);
                return null;
            }

            if (doc.ParsedText is null || doc.DocumentNode is null)
            {
                LogEntry entry = new(LogEntry.MsgType.Error, $"Failed to reach {url}");
                progressVM.AddLogEntry(entry);
                Logger.Error($"{url} does not have any content", new ArgumentNullException());
                return null;
            }
            else
            {
                return doc;
            }
        }

        public static void ClearTmpData(string tempPath = null)
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
    }
}
