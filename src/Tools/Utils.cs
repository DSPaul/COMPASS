using COMPASS.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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
            IEnumerable<int> UsedIDs = Collection.Select(x => x.ID);
            while (UsedIDs.Contains(tempID))
            {
                tempID++;
            }
            return tempID;
        }

        //put all childeren of object in a flat enumerable
        public static IEnumerable<T> FlattenTree<T>(IEnumerable<T> l) where T : IHasChilderen<T>
        {
            var result = l.ToList();
            for (int i = 0; i < result.Count; i++)
            {
                T parent = result[i];
                result.AddRange(parent.Children);
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
