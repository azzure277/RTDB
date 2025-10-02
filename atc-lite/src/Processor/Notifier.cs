using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Processor
{
    public static class Notifier
    {
        private static readonly HttpClient _http = new HttpClient();

        public static async Task NotifyAsync(string url, string type, object payload)
        {
            var body = JsonSerializer.Serialize(new { type, payload });
            var content = new StringContent(body, Encoding.UTF8, "application/json");
            await _http.PostAsync(url, content);
        }
    }
}
