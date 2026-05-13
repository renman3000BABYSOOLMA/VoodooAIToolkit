using UnityEditor;

namespace AIToolkit.Core
{
    public enum AIService { OpenAI, Anthropic, Gemini, Ollama }

    public static class AIToolkitSettings
    {
        private const string KeyService   = "AIToolkit_Service";
        private const string KeyOutFolder = "AIToolkit_OutputFolder";

        public static AIService SelectedService
        {
            get => (AIService)EditorPrefs.GetInt(KeyService, 0);
            set => EditorPrefs.SetInt(KeyService, (int)value);
        }

        // Per-service API key storage
        public static string GetApiKey(AIService service)
            => EditorPrefs.GetString($"AIToolkit_ApiKey_{service}", "");

        public static void SetApiKey(AIService service, string key)
            => EditorPrefs.SetString($"AIToolkit_ApiKey_{service}", key);

        // Convenience: key for currently selected service
        public static string ApiKey => GetApiKey(SelectedService);

        public static string OutputFolder
        {
            get => EditorPrefs.GetString(KeyOutFolder, "Assets/AIGenerated");
            set => EditorPrefs.SetString(KeyOutFolder, value);
        }
    }
}
