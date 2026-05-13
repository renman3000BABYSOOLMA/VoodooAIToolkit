using System;
using System.Collections.Generic;
using System.IO;
using AIToolkit.Core;
using UnityEditor;
using UnityEngine;

namespace AIToolkit.Windows
{
    public class AIScriptReviewerWindow : EditorWindow
    {
        // --- Chat ---
        private string  _chatInput       = "";
        private Vector2 _chatInputScroll;

        // --- Attachments ---
        private readonly List<string> _attachments = new List<string>();

        // --- Cached styles ---
        private GUIStyle _attachmentLabelStyle;
        private GUIStyle AttachmentLabelStyle => _attachmentLabelStyle ??= new GUIStyle(EditorStyles.label)
        {
            fontSize = EditorStyles.helpBox.fontSize,
            normal   = { textColor = EditorStyles.helpBox.normal.textColor }
        };

        // --- Review Type ---
        private ReviewType _reviewType = ReviewType.General;

        // --- State ---
        private string      _result     = "";
        private bool        _isBusy;
        private string      _status     = "";
        private MessageType _statusType = MessageType.Info;
        private Vector2     _resultScroll;

        [MenuItem("Tools/AI Toolkit/AI Script Reviewer")]
        public static void Open() => GetWindow<AIScriptReviewerWindow>("AI Script Reviewer");

        private void OnEnable()  => SettingsPanel.OnStatusMessage += OnSettingsStatus;
        private void OnDisable() => SettingsPanel.OnStatusMessage -= OnSettingsStatus;
        private void OnSettingsStatus(string msg) => SetStatus(msg, MessageType.Info);

        private void OnGUI()
        {
            DrawHeader();
            Space();
            SettingsPanel.Draw(SettingsPanel.ToolContext.Text);
            Space();
            DrawChatSection();
            Space();
            DrawAttachmentsSection();
            Space();
            DrawReviewType();
            Space();
            DrawSendButton();
            Space();
            DrawResults();
            DrawStatus();
        }

        // ── Header ────────────────────────────────────────────────────────────

        private void DrawHeader()
        {
            GUILayout.Label("AI Script Reviewer", new GUIStyle(EditorStyles.boldLabel) { fontSize = 15 });
            GUILayout.Label("Ask anything — optionally attach a script for code review.", EditorStyles.miniLabel);
        }

        // ── Chat ──────────────────────────────────────────────────────────────

        private void DrawChatSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Chat", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Clear Chat", GUILayout.Width(78)))
            {
                _chatInput = "";
                GUIUtility.keyboardControl = 0;
                SetStatus("Chat Cleared", MessageType.Info);
                Repaint();
            }
            if (GUILayout.Button("Clear All", GUILayout.Width(70)))
                ClearAll();
            EditorGUILayout.EndHorizontal();

            var wrapStyle = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
            _chatInputScroll = EditorGUILayout.BeginScrollView(_chatInputScroll, GUILayout.Height(100));
            _chatInput = EditorGUILayout.TextArea(_chatInput, wrapStyle, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void ClearAll()
        {
            _chatInput = "";
            _attachments.Clear();
            GUIUtility.keyboardControl = 0;
            SetStatus("All Cleared", MessageType.Info);
            Repaint();
        }

        // ── Attachments ───────────────────────────────────────────────────────

        private void DrawAttachmentsSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Attachments", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+", GUILayout.Width(24)))
            {
                string path = EditorUtility.OpenFilePanel("Select Script", "Assets", "cs");
                if (!string.IsNullOrEmpty(path))
                    AddAttachment("Assets" + path.Substring(Application.dataPath.Length));
            }
            EditorGUILayout.EndHorizontal();

            // Drag & drop zone — always visible as a drop target
            Rect dropArea = GUILayoutUtility.GetRect(0, 32, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drop .cs files here", EditorStyles.helpBox);
            HandleDragAndDrop(dropArea);

            // Attachment list — indented, text matches drop zone style
            string toRemove = null;
            foreach (string path in _attachments)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(10);
                GUILayout.Label(Path.GetFileName(path), AttachmentLabelStyle, GUILayout.ExpandWidth(true));
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                    toRemove = path;
                EditorGUILayout.EndHorizontal();
            }
            if (toRemove != null) _attachments.Remove(toRemove);

            EditorGUILayout.EndVertical();
        }

        private void HandleDragAndDrop(Rect dropArea)
        {
            Event evt = Event.current;
            if (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform) return;
            if (!dropArea.Contains(evt.mousePosition)) return;

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                foreach (string path in DragAndDrop.paths)
                    if (path.EndsWith(".cs"))
                        AddAttachment(path);
            }
            evt.Use();
        }

        private void AddAttachment(string path)
        {
            if (!string.IsNullOrEmpty(path) && !_attachments.Contains(path))
            {
                _attachments.Add(path);
                SetStatus($"{Path.GetFileName(path)} Added", MessageType.Info);
            }
        }

        // ── Review Type ───────────────────────────────────────────────────────

        private void DrawReviewType()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Review Type", GUILayout.Width(EditorGUIUtility.labelWidth));
            _reviewType = (ReviewType)EditorGUILayout.EnumPopup(_reviewType);
            EditorGUILayout.EndHorizontal();
        }

        // ── Send Button ───────────────────────────────────────────────────────

        private void DrawSendButton()
        {
            bool hasInput = !string.IsNullOrWhiteSpace(_chatInput) || _attachments.Count > 0;
            GUI.enabled = !_isBusy && hasInput;
            if (GUILayout.Button(_isBusy ? "Waiting…" : "Send", GUILayout.Height(36)))
                _ = RunAsync();
            GUI.enabled = true;
        }

        private async System.Threading.Tasks.Task RunAsync()
        {
            if (AIToolkitSettings.SelectedService != AIService.Ollama && string.IsNullOrEmpty(AIToolkitSettings.ApiKey))
            {
                SetStatus("Enter and save an API key in Settings first.", MessageType.Error);
                return;
            }

            _isBusy = true;
            _result = "";
            SetStatus("Sending…", MessageType.Info);
            Repaint();

            try
            {
                var service = ServiceFactory.GetTextService(
                    AIToolkitSettings.SelectedService,
                    AIToolkitSettings.ApiKey,
                    SettingsPanel.SelectedTextModelString);

                bool hasAttachments = _attachments.Count > 0;
                bool hasChat        = !string.IsNullOrWhiteSpace(_chatInput);

                if (hasAttachments)
                {
                    // Read and concatenate all attached scripts
                    var scriptBuilder = new System.Text.StringBuilder();
                    foreach (string attachment in _attachments)
                    {
                        string fullPath = Path.IsPathRooted(attachment)
                            ? attachment
                            : Path.GetFullPath(Path.Combine(Application.dataPath, "..", attachment));

                        if (!File.Exists(fullPath)) { SetStatus($"File not found: {attachment}", MessageType.Error); return; }

                        try
                        {
                            scriptBuilder.AppendLine($"// --- {Path.GetFileName(attachment)} ---");
                            scriptBuilder.AppendLine(File.ReadAllText(fullPath));
                            scriptBuilder.AppendLine();
                        }
                        catch (Exception e) { SetStatus($"Could not read {attachment}: {e.Message}", MessageType.Error); return; }
                    }

                    string scripts = scriptBuilder.ToString();

                    if (hasChat)
                    {
                        string combined = $"{_chatInput}\n\nAttached scripts:\n{scripts}";
                        _result = await service.ChatAsync(combined);
                    }
                    else
                    {
                        _result = await service.ReviewCodeAsync(scripts, _reviewType);
                    }
                }
                else
                {
                    _result = await service.ChatAsync(_chatInput);
                }

                SetStatus("Response Received", MessageType.Info);
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

        // ── Results ───────────────────────────────────────────────────────────

        private void DrawResults()
        {
            if (string.IsNullOrEmpty(_result)) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            GUILayout.Label("Response", EditorStyles.boldLabel);

            _resultScroll = EditorGUILayout.BeginScrollView(_resultScroll, GUILayout.Height(280));
            EditorGUILayout.TextArea(_result, EditorStyles.wordWrappedLabel, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Copy to Clipboard"))
                EditorGUIUtility.systemCopyBuffer = _result;
            if (GUILayout.Button("Clear Response"))
            {
                _result = "";
                GUIUtility.keyboardControl = 0;
                SetStatus("Response Cleared", MessageType.Info);
                Repaint();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
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
