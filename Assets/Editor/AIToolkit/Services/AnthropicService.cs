using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AIToolkit.Core;

namespace AIToolkit.Services
{
    public class AnthropicService : ITextService
    {
        private const string Endpoint   = "https://api.anthropic.com/v1/messages";
        private const string ApiVersion = "2023-06-01";
        private static readonly HttpClient Http = new HttpClient();
        private readonly string _apiKey;
        private readonly string _model;

        public AnthropicService(string apiKey, string model = "claude-sonnet-4-6")
        {
            _apiKey = apiKey;
            _model  = model;
        }

        public async Task<string> ReviewCodeAsync(string code, ReviewType reviewType)
        {
            string system = GetSystemPrompt(reviewType);
            string json = $"{{\"model\":\"{_model}\",\"max_tokens\":1024,\"system\":\"{JsonHelper.EscapeForJson(system)}\",\"messages\":[{{\"role\":\"user\",\"content\":\"{JsonHelper.EscapeForJson(code)}\"}}]}}";

            var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
            request.Headers.Add("x-api-key", _apiKey);
            request.Headers.Add("anthropic-version", ApiVersion);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await Http.SendAsync(request);
            string body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Anthropic error {(int)response.StatusCode}: {body}");

            // Anthropic response: {"content":[{"type":"text","text":"..."}]}
            string text = JsonHelper.ExtractString(body, "text");
            return text ?? "Could not parse response.";
        }

        public async Task<string> ChatAsync(string message)
        {
            string system = JsonHelper.EscapeForJson("You are a helpful Unity game development expert. Answer clearly and concisely.");
            string json = $"{{\"model\":\"{_model}\",\"max_tokens\":1024,\"system\":\"{system}\",\"messages\":[{{\"role\":\"user\",\"content\":\"{JsonHelper.EscapeForJson(message)}\"}}]}}";

            var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
            request.Headers.Add("x-api-key", _apiKey);
            request.Headers.Add("anthropic-version", ApiVersion);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await Http.SendAsync(request);
            string body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Anthropic error {(int)response.StatusCode}: {body}");

            return JsonHelper.ExtractString(body, "text") ?? "Could not parse response.";
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
