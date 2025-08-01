using System.Net.Http;
using System.Threading.Tasks;

namespace UniversalDashboard.Services
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;

        public ApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> FetchSiteData(string apiUrl, string apiKey, string endpoint)
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey); // API key header
            var response = await _httpClient.GetAsync($"{apiUrl.TrimEnd('/')}/{endpoint}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }
}
