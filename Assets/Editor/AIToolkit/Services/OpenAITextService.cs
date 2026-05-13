using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using AIToolkit.Core;

namespace AIToolkit.Services
{
    public class OpenAITextService : ITextService
    {
        private const string Endpoint = "https://api.openai.com/v1/chat/completions";
        private static readonly HttpClient Http = new HttpClient();
        private readonly string _apiKey;

        private readonly string _model;

        public OpenAITextService(string apiKey, string model = "gpt-4o") { _apiKey = apiKey; _model = model; }

        public async Task<string> ReviewCodeAsync(string code, ReviewType reviewType)
        {
            string system = GetSystemPrompt(reviewType);
            string json = $"{{\"model\":\"{_model}\",\"messages\":[{{\"role\":\"system\",\"content\":\"{JsonHelper.EscapeForJson(system)}\"}},{{\"role\":\"user\",\"content\":\"{JsonHelper.EscapeForJson(code)}\"}}],\"max_completion_tokens\":1024}}";

            var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await Http.SendAsync(request);
            string body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"OpenAI error {(int)response.StatusCode}: {body}");

            string content = JsonHelper.ExtractString(body, "content");
            return content ?? "Could not parse response.";
        }

        public async Task<string> ChatAsync(string message)
        {
            string system = JsonHelper.EscapeForJson("You are a helpful Unity game development expert. Answer clearly and concisely.");
            string json = $"{{\"model\":\"{_model}\",\"messages\":[{{\"role\":\"system\",\"content\":\"{system}\"}},{{\"role\":\"user\",\"content\":\"{JsonHelper.EscapeForJson(message)}\"}}],\"max_completion_tokens\":1024}}";

            var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await Http.SendAsync(request);
            string body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"OpenAI error {(int)response.StatusCode}: {body}");

            return JsonHelper.ExtractString(body, "content") ?? "Could not parse response.";
        }

        private static string GetSystemPrompt(ReviewType type) => type switch
        {
            ReviewType.Performance         => "You are a Unity performance expert. Review this C# script for GC allocations, expensive per-frame operations, and frame-rate impact. Be specific and concise.",
            ReviewType.CodeQuality         => "You are a senior Unity developer. Review this C# script for code quality, readability, naming conventions, and Unity best practices.",
            ReviewType.MobileOptimization  => "You are a mobile Unity developer. Review this C# script for mobile-specific issues: battery drain, memory usage, CPU cost, and draw call implications.",
            _                              => "You are a senior Unity developer. Review this C# script and provide clear, actionable feedback on any issues you find."
        };
    }
}
