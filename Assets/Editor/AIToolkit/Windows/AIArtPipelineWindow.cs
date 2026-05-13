using System;
using System.IO;
using AIToolkit.Core;
using UnityEditor;
using UnityEngine;

namespace AIToolkit.Windows
{
    public class AIArtPipelineWindow : EditorWindow
    {
        // --- Prompt fields ---
        private string _prompt    = "";
        private string _styleHint = "flat design, mobile game, transparent background";
        private ImageResolution _resolution = ImageResolution._1024x1024;

        // --- Output fields ---
        private string _outputFolder = "Assets/AIGenerated";
        private string _fileName     = "generated_image";

        // --- State ---
        private Texture2D _preview;
        private byte[]    _pendingBytes;
        private bool      _isBusy;
        private string    _status     = "";
        private MessageType _statusType = MessageType.Info;
        private Vector2   _scroll;

        [MenuItem("Tools/AI Toolkit/AI Art Pipeline")]
        public static void Open() => GetWindow<AIArtPipelineWindow>("AI Art Pipeline");

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawHeader();
            Space();
            SettingsPanel.Draw(SettingsPanel.ToolContext.Image);
            Space();
            DrawPromptSection();
            Space();
            DrawOptionsSection();
            Space();
            DrawGenerateButton();
            Space();
            DrawPreview();
            Space();
            DrawStatus();

            EditorGUILayout.EndScrollView();
        }

        // ── Header ────────────────────────────────────────────────────────────

        private void DrawHeader()
        {
            GUILayout.Label("AI Art Pipeline", new GUIStyle(EditorStyles.boldLabel) { fontSize = 15 });
            GUILayout.Label("Generate image assets without leaving Unity.", EditorStyles.miniLabel);
        }

        // ── Prompt ────────────────────────────────────────────────────────────

        private void DrawPromptSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Prompt", EditorStyles.boldLabel);
            var wrapStyle = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
            _prompt    = EditorGUILayout.TextArea(_prompt, wrapStyle, GUILayout.Height(64));
            _styleHint = EditorGUILayout.TextField("Style Hint", _styleHint);
            EditorGUILayout.EndVertical();
        }

        // ── Options ───────────────────────────────────────────────────────────

        private void DrawOptionsSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Options", EditorStyles.boldLabel);

            _resolution = (ImageResolution)EditorGUILayout.EnumPopup("Resolution", _resolution);

            EditorGUILayout.BeginHorizontal();
            _outputFolder = EditorGUILayout.TextField("Output Folder", _outputFolder);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFolderPanel("Output Folder", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                    _outputFolder = "Assets" + path.Substring(Application.dataPath.Length);
            }
            EditorGUILayout.EndHorizontal();

            _fileName = EditorGUILayout.TextField("File Name", _fileName);
            EditorGUILayout.EndVertical();
        }

        // ── Generate ──────────────────────────────────────────────────────────

        private void DrawGenerateButton()
        {
            bool canGenerate = !_isBusy && !string.IsNullOrWhiteSpace(_prompt);

            GUI.enabled = canGenerate;
            if (GUILayout.Button(_isBusy ? "Generating…" : "Generate", GUILayout.Height(36)))
                _ = RunGenerationAsync();
            GUI.enabled = true;
        }

        private async System.Threading.Tasks.Task RunGenerationAsync()
        {
            AIService imgSvc = SettingsPanel.SelectedImageService;
            string    apiKey = AIToolkitSettings.GetApiKey(imgSvc);

            if (string.IsNullOrEmpty(apiKey))
            {
                SetStatus("Enter and save an API key in Settings first.", MessageType.Error);
                return;
            }

            _isBusy       = true;
            _preview      = null;
            _pendingBytes = null;
            SetStatus("Generating…", MessageType.Info);
            Repaint();

            try
            {
                var service = ServiceFactory.GetImageService(imgSvc, apiKey, SettingsPanel.SelectedImageModelString);
                byte[] bytes = await service.GenerateImageAsync(_prompt, _styleHint, _resolution);

                _pendingBytes = bytes;
                _preview      = new Texture2D(2, 2);
                _preview.LoadImage(bytes);
                SetStatus("Image ready — click 'Import to Project' to save.", MessageType.Info);
            }
            catch (Exception e)
            {
                SetStatus(e.Message, MessageType.Error);
            }
            finally
            {
                _isBusy = false;
                Repaint();
            }
        }

        // ── Preview ───────────────────────────────────────────────────────────

        private void DrawPreview()
        {
            if (_preview == null) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Preview", EditorStyles.boldLabel);

            float w      = position.width - 24f;
            float aspect = (float)_preview.height / _preview.width;
            GUILayout.Label(_preview, GUILayout.Width(w), GUILayout.Height(w * aspect));

            EditorGUILayout.BeginHorizontal();
            if (_pendingBytes != null && GUILayout.Button("Import to Project", GUILayout.Height(30)))
                ImportImage();
            if (GUILayout.Button("Clear Image", GUILayout.Height(30)))
            {
                _preview      = null;
                _pendingBytes = null;
                _status       = "";
                Repaint();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void ImportImage()
        {
            try
            {
                if (!Directory.Exists(_outputFolder))
                    Directory.CreateDirectory(_outputFolder);

                string path = Path.Combine(_outputFolder, $"{_fileName}.png");
                File.WriteAllBytes(path, _pendingBytes);
                AssetDatabase.Refresh();
                _pendingBytes = null;
                SetStatus($"Imported → {path}", MessageType.Info);
            }
            catch (Exception e)
            {
                SetStatus($"Import failed: {e.Message}", MessageType.Error);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void DrawStatus()
        {
            if (!string.IsNullOrEmpty(_status))
                EditorGUILayout.HelpBox(_status, _statusType);
        }

        private void SetStatus(string msg, MessageType type) { _status = msg; _statusType = type; }
        private static void Space() => GUILayout.Space(6);
    }
}
