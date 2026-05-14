using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using AIToolkit.Core;

namespace AIToolkit.Services
{
    public class OpenAIImageService : IImageService
    {
        private const string GenerationsEndpoint = "https://api.openai.com/v1/images/generations";
        private const string EditsEndpoint       = "https://api.openai.com/v1/images/edits";
        private const string EditsModel          = "gpt-image-1"; // edits endpoint requires gpt-image-1

        private static readonly HttpClient Http = new HttpClient();
        private readonly string _apiKey;
        private readonly string _model;

        public OpenAIImageService(string apiKey, string model) { _apiKey = apiKey; _model = model; }

        public async Task<byte[]> GenerateImageAsync(string prompt, string styleHint, ImageResolution resolution, byte[] referenceImageBytes = null)
        {
            string fullPrompt = string.IsNullOrEmpty(styleHint)
                ? prompt
                : $"{prompt}. Style: {styleHint}";

            return referenceImageBytes != null
                ? await GenerateWithReferenceAsync(fullPrompt, resolution, referenceImageBytes)
                : await GenerateStandardAsync(fullPrompt, resolution);
        }

        // ── Standard generations endpoint ─────────────────────────────────────

        private async Task<byte[]> GenerateStandardAsync(string fullPrompt, ImageResolution resolution)
        {
            string size = $"{(int)resolution}x{(int)resolution}";
            // response_format omitted — gpt-image-1 returns b64_json by default; dall-e-2 returns url
            string json = $"{{\"model\":\"{_model}\",\"prompt\":\"{JsonHelper.EscapeForJson(fullPrompt)}\",\"n\":1,\"size\":\"{size}\"}}";

            var request = new HttpRequestMessage(HttpMethod.Post, GenerationsEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await Http.SendAsync(request);
            string body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"OpenAI error {(int)response.StatusCode}: {body}");

            return await ParseImageBytesAsync(body);
        }

        // ── Edits endpoint (multipart/form-data, reference image provided) ────

        private async Task<byte[]> GenerateWithReferenceAsync(string fullPrompt, ImageResolution resolution, byte[] referenceImageBytes)
        {
            string size = $"{(int)resolution}x{(int)resolution}";

            var content = new MultipartFormDataContent();

            // Reference image — must be PNG
            var imageContent = new ByteArrayContent(referenceImageBytes);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            content.Add(imageContent, "image", "reference.png");

            content.Add(new StringContent(fullPrompt),  "prompt");
            content.Add(new StringContent(EditsModel),  "model");
            content.Add(new StringContent("1"),         "n");
            content.Add(new StringContent(size),        "size");

            var request = new HttpRequestMessage(HttpMethod.Post, EditsEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Content = content;

            var response = await Http.SendAsync(request);
            string body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"OpenAI edits error {(int)response.StatusCode}: {body}");

            return await ParseImageBytesAsync(body);
        }

        // ── Shared response parser ────────────────────────────────────────────

        private static async Task<byte[]> ParseImageBytesAsync(string body)
        {
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
