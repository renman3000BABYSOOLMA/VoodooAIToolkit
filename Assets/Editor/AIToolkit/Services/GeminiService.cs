using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AIToolkit.Core;

namespace AIToolkit.Services
{
    public class GeminiService : ITextService
    {
        private static readonly HttpClient Http = new HttpClient();
        private readonly string _apiKey;
        private readonly string _model;

        public GeminiService(string apiKey, string model = "gemini-2.0-flash")
        {
            _apiKey = apiKey;
            _model  = model;
        }

        public async Task<string> ReviewCodeAsync(string code, ReviewType reviewType)
        {
            string endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";
            string fullPrompt = $"{GetSystemPrompt(reviewType)}\n\n{code}";
            string json = $"{{\"contents\":[{{\"parts\":[{{\"text\":\"{JsonHelper.EscapeForJson(fullPrompt)}\"}}]}}]}}";

            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await Http.SendAsync(request);
            string body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Gemini error {(int)response.StatusCode}: {body}");

            // Gemini response: {"candidates":[{"content":{"parts":[{"text":"..."}]}}]}
            string text = JsonHelper.ExtractString(body, "text");
            return text ?? "Could not parse response.";
        }

        public async Task<string> ChatAsync(string message)
        {
            string endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";
            string fullPrompt = $"You are a helpful Unity game development expert. Answer clearly and concisely.\n\n{message}";
            string json = $"{{\"contents\":[{{\"parts\":[{{\"text\":\"{JsonHelper.EscapeForJson(fullPrompt)}\"}}]}}]}}";

            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await Http.SendAsync(request);
            string body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Gemini error {(int)response.StatusCode}: {body}");

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
