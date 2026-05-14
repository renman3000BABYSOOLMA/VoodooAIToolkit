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

        // --- Reference image ---
        private Texture2D _referenceImage;

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

        [MenuItem("Tools/AI Toolkit/Art Generator")]
        public static void Open() => GetWindow<AIArtPipelineWindow>("Art Generator");

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
            GUILayout.Label("Art Generator", new GUIStyle(EditorStyles.boldLabel) { fontSize = 15 });
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

            DrawReferenceImageField();

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

        // ── Reference image field ─────────────────────────────────────────────

        private void DrawReferenceImageField()
        {
            _referenceImage = (Texture2D)EditorGUILayout.ObjectField(
                "Reference Image (optional)", _referenceImage, typeof(Texture2D), false);

            if (_referenceImage != null)
            {
                // Small inline preview (~80x80 px)
                const float previewSize = 80f;
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(EditorGUIUtility.labelWidth);
                GUILayout.Label(_referenceImage, GUILayout.Width(previewSize), GUILayout.Height(previewSize));
                EditorGUILayout.EndHorizontal();
            }
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
                // Read reference image bytes before entering the async call so we stay on the main thread
                byte[] refBytes = _referenceImage != null ? TextureToReadableCopy(_referenceImage) : null;

                var service = ServiceFactory.GetImageService(imgSvc, apiKey, SettingsPanel.SelectedImageModelString);
                byte[] bytes = await service.GenerateImageAsync(_prompt, _styleHint, _resolution, refBytes);

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

        // Creates a CPU-readable copy of any Texture2D via a temporary RenderTexture blit,
        // bypassing the isReadable restriction that many imported textures have.
        private static byte[] TextureToReadableCopy(Texture2D source)
        {
            RenderTexture rt  = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
            RenderTexture prev = RenderTexture.active;

            Graphics.Blit(source, rt);
            RenderTexture.active = rt;

            Texture2D readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            readable.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            readable.Apply();

            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);

            byte[] png = readable.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(readable);
            return png;
        }

        private void DrawStatus()
        {
            if (!string.IsNullOrEmpty(_status))
                EditorGUILayout.HelpBox(_status, _statusType);
        }

        private void SetStatus(string msg, MessageType type) { _status = msg; _statusType = type; }
        private static void Space() => GUILayout.Space(6);
    }
}
