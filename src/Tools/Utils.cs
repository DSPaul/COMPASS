using COMPASS.Models;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace COMPASS.Tools
{
    public static class Utils
    {
        public static int GetAvailableID<T>(IEnumerable<T> Collection) where T : IHasID
        {
            int tempID = 0;
            while (Collection.Any(f => f.ID == tempID))
            {
                tempID++;
            }
            return tempID;
        }

        //put all childeren of object in a flat enumerable
        public static IEnumerable<T> FlattenTree<T>(IEnumerable<T> l) where T : IHasChilderen<T>
        {
            var result = new List<T>(l);
            for (int i = 0; i < result.Count; i++)
            {
                T parent = result[i];
                foreach (T child in parent.Children) result.Add(child);
            }
            return result;
        }

        //check internet connection
        public static bool PingURL(string URL = "8.8.8.8")
        {
            Ping p = new();
            try
            {
                PingReply reply = p.Send(URL, 3000);
                if (reply.Status == IPStatus.Success)
                    return true;
            }
            catch (Exception ex)
            {
                Logger.log.Warn(ex.InnerException);
            }
            return false;
        }

        //Try functions in order determined by list of preferablefunctions untill one succeeds
        public static bool TryFunctions<T>(List<PreferableFunction<T>> toTry, T arg)
        {
            bool success = false;
            int i = 0;
            while (!success && i < toTry.Count)
            {
                success = toTry[i].Function(arg);
                i++;
            }
            return success;
        }
        public static bool TryFunctions<T>(ObservableCollection<PreferableFunction<T>> toTry, T arg)
            => TryFunctions(toTry.ToList(), arg);

        //Download data and put it in a byte[]
        public static async Task<byte[]> DownloadFileAsync(string uri)
        {
            using HttpClient client = new();

            if (!Uri.TryCreate(uri, UriKind.Absolute, out Uri uriResult)) throw new InvalidOperationException("URI is invalid.");

            return await client.GetByteArrayAsync(uri);
        }

        public static async Task<JObject> GetJsonAsync(string uri)
        {
            using HttpClient client = new();

            JObject json = null;

            if (!Uri.TryCreate(uri, UriKind.Absolute, out Uri uriResult)) throw new InvalidOperationException("URI is invalid.");

            HttpResponseMessage response = await client.GetAsync(uri);
            if (response.IsSuccessStatusCode)
            {
                var data = response.Content.ReadAsStringAsync();
                json = JObject.Parse(data.Result);
            }
            return json;
        }
    }
}
