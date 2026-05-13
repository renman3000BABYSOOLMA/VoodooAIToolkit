using AIToolkit.Services;

namespace AIToolkit.Core
{
    public static class ServiceFactory
    {
        public static IImageService GetImageService(AIService service, string apiKey, string model)
        {
            return service switch
            {
                AIService.OpenAI  => new OpenAIImageService(apiKey, model),
                AIService.Gemini  => new GeminiImageService(apiKey, model),
                _                 => null
            };
        }

        public static ITextService GetTextService(AIService service, string apiKey, string model = "gpt-4o")
        {
            return service switch
            {
                AIService.OpenAI    => new OpenAITextService(apiKey, model),
                AIService.Anthropic => new AnthropicService(apiKey, model),
                AIService.Gemini    => new GeminiService(apiKey, model),
                AIService.Ollama    => new OllamaService(model),
                _                   => null
            };
        }
    }
}
