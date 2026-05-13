using System;
using AIToolkit.Core;
using UnityEditor;
using UnityEngine;

namespace AIToolkit.Windows
{
    public static class SettingsPanel
    {
        // ── Text service data ─────────────────────────────────────────────────
        private static readonly string[] ServiceLabels =
            { "OpenAI", "Anthropic (Claude)", "Google Gemini", "Ollama (Local — no key needed)" };

        private static readonly string[][] TextModelLabelsByService =
        {
            new[] { "GPT 5.5", "GPT 5.4 Mini", "GPT 5.4 Nano" },          // OpenAI
            new[] { "Claude Sonnet 4.6" },                                  // Anthropic
            new[] { "Gemini 2.0 Flash", "Gemini 2.0 Flash Lite" },          // Gemini
            new[] { "Llama 3" },                                             // Ollama
        };

        private static readonly string[][] TextModelApiStringsByService =
        {
            new[] { "gpt-5.5", "gpt-5.4-mini", "gpt-5.4-nano" },           // OpenAI
            new[] { "claude-sonnet-4-6" },                                   // Anthropic
            new[] { "gemini-2.0-flash", "gemini-2.0-flash-lite" },          // Gemini
            new[] { "llama3" },                                              // Ollama
        };

        // ── Image service data ────────────────────────────────────────────────
        private static readonly string[] ImageServiceLabels =
            { "OpenAI", "Google Gemini" };

        private static readonly AIService[] ImageServices =
            { AIService.OpenAI, AIService.Gemini };

        private static readonly string[][] ImageModelLabelsByService =
        {
            new[] { "GPT Image 2", "GPT Image 1.5", "GPT Image 1", "GPT Image 1 Mini" }, // OpenAI
            new[] { "Gemini 3 Pro Image", "Gemini 2.5 Flash Image" },                     // Gemini
        };

        private static readonly string[][] ImageModelApiStringsByService =
        {
            new[] { "gpt-image-2", "gpt-image-1.5", "gpt-image-1", "gpt-image-1-mini" }, // OpenAI
            new[] { "gemini-3-pro-image-preview", "gemini-2.5-flash-image" },             // Gemini
        };

        // ── State — text ──────────────────────────────────────────────────────
        private static AIService _lastService    = (AIService)(-1);
        private static int       _textModelIndex = 0;
        private static bool      _editingTextKey = false;
        private static string    _textKeyBuffer  = null;

        // ── State — image ─────────────────────────────────────────────────────
        private static AIService _lastImageService  = (AIService)(-1);
        private static int       _imageServiceIndex = 0;
        private static int       _imageModelIndex   = 0;
        private static bool      _editingImageKey   = false;
        private static string    _imageKeyBuffer    = null;

        public static event Action<string> OnStatusMessage;

        public enum ToolContext { Image, Text }

        // ── Public accessors ──────────────────────────────────────────────────
        public static AIService SelectedImageService =>
            ImageServices[_imageServiceIndex];

        public static string SelectedImageModelString =>
            ImageModelApiStringsByService[_imageServiceIndex][_imageModelIndex];

        public static string SelectedTextModelString =>
            TextModelApiStringsByService[(int)AIToolkitSettings.SelectedService][_textModelIndex];

        // ── Draw ──────────────────────────────────────────────────────────────
        public static void Draw(ToolContext context)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Settings", EditorStyles.boldLabel);

            if (context == ToolContext.Image)
                DrawImageSettings();
            else
                DrawTextSettings();

            EditorGUILayout.EndVertical();
        }

        // ── Image settings ────────────────────────────────────────────────────
        private static void DrawImageSettings()
        {
            AIService imgSvc = ImageServices[_imageServiceIndex];

            // Detect image service switch
            if (imgSvc != _lastImageService)
            {
                _imageModelIndex = 0;
                _imageKeyBuffer  = AIToolkitSettings.GetApiKey(imgSvc);
                _editingImageKey = string.IsNullOrEmpty(_imageKeyBuffer);
                _lastImageService = imgSvc;
            }

            // Load key on first draw
            if (_imageKeyBuffer == null)
            {
                _imageKeyBuffer  = AIToolkitSettings.GetApiKey(imgSvc);
                _editingImageKey = string.IsNullOrEmpty(_imageKeyBuffer);
            }

            int imgSelected = EditorGUILayout.Popup("Service", _imageServiceIndex, ImageServiceLabels);
            if (imgSelected != _imageServiceIndex)
                _imageServiceIndex = imgSelected;

            _imageModelIndex = EditorGUILayout.Popup("Model", _imageModelIndex, ImageModelLabelsByService[_imageServiceIndex]);

            DrawKeyField(imgSvc, ref _imageKeyBuffer, ref _editingImageKey);
        }

        // ── Text settings ─────────────────────────────────────────────────────
        private static void DrawTextSettings()
        {
            AIService svc = AIToolkitSettings.SelectedService;

            // Detect text service switch
            if (svc != _lastService)
            {
                _textModelIndex  = 0;
                _textKeyBuffer   = AIToolkitSettings.GetApiKey(svc);
                _editingTextKey  = string.IsNullOrEmpty(_textKeyBuffer);
                if (_lastService != (AIService)(-1))
                    OnStatusMessage?.Invoke($"Service: {ServiceLabels[(int)svc]}");
                _lastService = svc;
            }

            // Load key on first draw
            if (_textKeyBuffer == null)
            {
                _textKeyBuffer  = AIToolkitSettings.GetApiKey(svc);
                _editingTextKey = string.IsNullOrEmpty(_textKeyBuffer);
            }

            int selected = EditorGUILayout.Popup("Service", (int)svc, ServiceLabels);
            if (selected != (int)svc)
                AIToolkitSettings.SelectedService = (AIService)selected;

            _textModelIndex = EditorGUILayout.Popup("Model", _textModelIndex, TextModelLabelsByService[(int)svc]);

            DrawKeyField(svc, ref _textKeyBuffer, ref _editingTextKey);
        }

        // ── Shared key field ──────────────────────────────────────────────────
        private static void DrawKeyField(AIService svc, ref string keyBuffer, ref bool editingKey)
        {
            bool needsKey = svc != AIService.Ollama;
            bool keySaved = needsKey && !string.IsNullOrEmpty(AIToolkitSettings.GetApiKey(svc));

            if (!needsKey)
            {
                EditorGUILayout.HelpBox(
                    "Ollama runs locally — no API key required. Ensure Ollama is running on localhost:11434.",
                    MessageType.Info);
                return;
            }

            if (keySaved && !editingKey)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("API Key", "● Saved");
                if (GUILayout.Button("Edit", GUILayout.Width(50)))
                {
                    editingKey = true;
                    keyBuffer  = "";
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                keyBuffer = EditorGUILayout.PasswordField("API Key", keyBuffer ?? "");
                if (GUILayout.Button("Save", GUILayout.Width(50)))
                {
                    AIToolkitSettings.SetApiKey(svc, keyBuffer);
                    editingKey = false;
                    Debug.Log("[AIToolkit] API key saved.");
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        public static void ResetBuffer()
        {
            _textKeyBuffer   = null;
            _imageKeyBuffer  = null;
            _lastService     = (AIService)(-1);
            _lastImageService = (AIService)(-1);
            _textModelIndex  = 0;
            _imageModelIndex = 0;
            _imageServiceIndex = 0;
            _editingTextKey  = false;
            _editingImageKey = false;
        }
    }
}
