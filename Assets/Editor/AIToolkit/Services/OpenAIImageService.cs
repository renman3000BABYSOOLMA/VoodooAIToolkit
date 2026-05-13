using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using AIToolkit.Core;

namespace AIToolkit.Services
{
    public class OpenAIImageService : IImageService
    {
        private const string Endpoint = "https://api.openai.com/v1/images/generations";
        private static readonly HttpClient Http = new HttpClient();
        private readonly string _apiKey;

        private readonly string _model;

        public OpenAIImageService(string apiKey, string model) { _apiKey = apiKey; _model = model; }

        public async Task<byte[]> GenerateImageAsync(string prompt, string styleHint, ImageResolution resolution)
        {
            string fullPrompt = string.IsNullOrEmpty(styleHint)
                ? prompt
                : $"{prompt}. Style: {styleHint}";

            string size = $"{(int)resolution}x{(int)resolution}";
            // response_format omitted — gpt-image-1 returns b64_json by default; dall-e-2 returns url
            string json = $"{{\"model\":\"{_model}\",\"prompt\":\"{JsonHelper.EscapeForJson(fullPrompt)}\",\"n\":1,\"size\":\"{size}\"}}";

            var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await Http.SendAsync(request);
            string body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"OpenAI error {(int)response.StatusCode}: {body}");

            // gpt-image-1 returns base64; dall-e-2 returns a URL — handle both
            string b64 = JsonHelper.ExtractString(body, "b64_json");
            if (!string.IsNullOrEmpty(b64))
                return Convert.FromBase64String(b64);

            string imageUrl = JsonHelper.ExtractString(body, "url");
            if (!string.IsNullOrEmpty(imageUrl))
                return await Http.GetByteArrayAsync(imageUrl);

            throw new Exception("Could not parse image data from response.");
        }
    }
}
