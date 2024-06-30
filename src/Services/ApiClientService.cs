using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace COMPASS.Services
{
    public class ApiClientService
    {
        private static HttpClient? _httpClient;

        //Api Key is shared by all instances of COMPASS, the api is open
        //it's only purpose is to filter out blind spam from bots, so it's fine to 'leak' it
        private const string ApiKey = "uwr2BswLryEsaXvjXEuumN6rwtKIGSBGv002APVgwDN4UCcb6LGKpFyuxAM9FuV9Ai030vQVc9NXL7ekKiQ0TJ9si53jvGxyiix5EVVqvqJeZkZ7wcG4hYtqdkuJNaFE";
        //private const string ApiDomain = "https://api.compassapp.info"; //custom domain starts at 10$/month on azure, I'm not paying for that
        private const string ApiDomain = "https://compass-api-pds.azurewebsites.net";


        public ApiClientService()
        {
            Client.DefaultRequestHeaders.Add("Api-Key", ApiKey);
        }

        private HttpClient Client => _httpClient ??= new HttpClient();

        public async Task<HttpResponseMessage> PostAsync(object o, string endpoint)
        {
            string json = JsonSerializer.Serialize(o);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await Client.PostAsync($"{ApiDomain}/{endpoint}", content).ConfigureAwait(false);
            return response;
        }
    }
}
