using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AIToolkit.Core;

namespace AIToolkit.Services
{
    // Talks to a locally running Ollama instance. No API key required.
    // Ollama must be running: https://ollama.com — default model: llama3
    public class OllamaService : ITextService
    {
        private const string Endpoint = "http://localhost:11434/api/generate";
        private static readonly HttpClient Http = new HttpClient { Timeout = System.TimeSpan.FromSeconds(120) };
        private readonly string _model;

        public OllamaService(string model = "llama3") => _model = model;

        public async Task<string> ReviewCodeAsync(string code, ReviewType reviewType)
        {
            string fullPrompt = $"{GetSystemPrompt(reviewType)}\n\n{code}";
            string json = $"{{\"model\":\"{_model}\",\"prompt\":\"{JsonHelper.EscapeForJson(fullPrompt)}\",\"stream\":false}}";

            HttpResponseMessage response;
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                response = await Http.SendAsync(request);
            }
            catch (Exception e)
            {
                throw new Exception($"Could not reach Ollama. Is it running on localhost:11434?\n{e.Message}");
            }

            string body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Ollama error {(int)response.StatusCode}: {body}");

            // Ollama response: {"response":"..."}
            string text = JsonHelper.ExtractString(body, "response");
            return text ?? "Could not parse response.";
        }

        public async Task<string> ChatAsync(string message)
        {
            string fullPrompt = $"You are a helpful Unity game development expert. Answer clearly and concisely.\n\n{message}";
            string json = $"{{\"model\":\"{_model}\",\"prompt\":\"{JsonHelper.EscapeForJson(fullPrompt)}\",\"stream\":false}}";

            HttpResponseMessage response;
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                response = await Http.SendAsync(request);
            }
            catch (Exception e)
            {
                throw new Exception($"Could not reach Ollama. Is it running on localhost:11434?\n{e.Message}");
            }

            string body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Ollama error {(int)response.StatusCode}: {body}");

            return JsonHelper.ExtractString(body, "response") ?? "Could not parse response.";
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
