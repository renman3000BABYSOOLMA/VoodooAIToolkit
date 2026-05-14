using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AIToolkit.Core;

namespace AIToolkit.Services
{
    public class GeminiImageService : IImageService
    {
        private static readonly HttpClient Http = new HttpClient();
        private readonly string _apiKey;
        private readonly string _model;

        public GeminiImageService(string apiKey, string model = "gemini-3-pro-image-preview")
        {
            _apiKey = apiKey;
            _model  = model;
        }

        public async Task<byte[]> GenerateImageAsync(string prompt, string styleHint, ImageResolution resolution, byte[] referenceImageBytes = null)
        {
            string fullPrompt = string.IsNullOrWhiteSpace(styleHint)
                ? prompt
                : $"{prompt}. Style: {styleHint}";

            string json = BuildRequestJson(fullPrompt, referenceImageBytes);

            string endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";
            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await Http.SendAsync(request);
            string body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Gemini image error {(int)response.StatusCode}: {body}");

            // Response: candidates[0].content.parts[n].inlineData.data (base64 PNG)
            string base64 = JsonHelper.ExtractString(body, "data");
            if (string.IsNullOrEmpty(base64))
                throw new Exception("Gemini image response contained no image data.");

            return Convert.FromBase64String(base64);
        }

        // ── Request builders ──────────────────────────────────────────────────

        private static string BuildRequestJson(string fullPrompt, byte[] referenceImageBytes)
        {
            string textPart = "{\"text\":\"" + JsonHelper.EscapeForJson(fullPrompt) + "\"}";

            string parts;
            if (referenceImageBytes != null)
            {
                // Gemini multimodal: supply the reference image as an inline_data part
                // alongside the text part so the model can use it as visual context.
                string b64 = Convert.ToBase64String(referenceImageBytes);
                string imagePart = "{\"inline_data\":{\"mime_type\":\"image/png\",\"data\":\"" + b64 + "\"}}";
                parts = imagePart + "," + textPart;
            }
            else
            {
                parts = textPart;
            }

            return "{\"contents\":[{\"parts\":[" + parts + "]}]," +
                   "\"generationConfig\":{\"responseModalities\":[\"IMAGE\"]}}";
        }
    }
}
