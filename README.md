# VoodooAIToolkit

A Unity Editor plugin that integrates multiple AI providers directly into the Unity workflow — reducing repetitive developer and artist tasks without leaving the Editor.

Built as a working proof-of-concept for AI-assisted game production pipelines.

---

## Tools

### Code Generator
Chat-based code review panel embedded in the Unity Editor. Attach any C# script and get instant AI feedback tuned to game development concerns.

**Review modes:**
- **General** — overall feedback and suggestions
- **Performance** — GC allocations, frame rate impact, expensive operations
- **Code Quality** — naming conventions, Unity best practices, maintainability
- **Mobile Optimization** — battery efficiency, memory footprint, draw call reduction

**Free-form chat** mode for general AI questions without attaching a script.

### Art Generator
Prompt-to-asset image generation inside the Unity Editor. Generate concept art or placeholder assets, preview them, and auto-import directly into your project's asset library.

**Supports:** text prompts, style hints, resolution selection (512×512, 1024×1024), configurable output folder.

---

## AI Providers

| Provider | Text (Code Review / Chat) | Image Generation |
|---|---|---|
| **OpenAI** | GPT-4o, GPT-4o-mini | DALL-E (gpt-image-1, gpt-image-2) |
| **Anthropic** | Claude Sonnet 3.5 | — |
| **Google Gemini** | Gemini 2.0 Flash | Gemini image models |
| **Ollama** (local) | Llama 3 (offline, no API key) | — |

Ollama support enables fully offline operation — no API costs, no data leaving the machine.

---

## Architecture

```
Assets/Editor/AIToolkit/
├── Core/
│   ├── ITextService.cs          # Interface for all text/LLM providers
│   ├── IImageService.cs         # Interface for all image providers
│   ├── ServiceFactory.cs        # Factory — swap providers without changing UI
│   ├── AIToolkitSettings.cs     # EditorPrefs-based config, per-provider API keys
│   └── JsonHelper.cs            # Zero-dependency JSON parser (no Newtonsoft)
├── Services/
│   ├── OpenAITextService.cs
│   ├── OpenAIImageService.cs
│   ├── AnthropicService.cs
│   ├── GeminiService.cs
│   ├── GeminiImageService.cs
│   └── OllamaService.cs
└── Windows/
    ├── AIScriptReviewerWindow.cs
    ├── AIArtPipelineWindow.cs
    └── SettingsPanel.cs         # Shared settings UI, reused across both windows
```

**Design decisions:**
- Interface-driven — adding a new AI provider means implementing `ITextService` or `IImageService`, nothing else changes
- Factory pattern — service selection is centralized, windows are provider-agnostic
- Zero external dependencies — no Newtonsoft.Json, no third-party packages; ships as a self-contained Editor plugin
- Async/await throughout — non-blocking API calls, Editor stays responsive

---

## Setup

1. Clone this repo into your Unity project's `Assets/` folder, or copy the `Assets/Editor/AIToolkit/` directory
2. Open Unity (2022.3 LTS or later)
3. Go to `Tools > AI Toolkit > Code Generator` or `Tools > AI Toolkit > Art Generator`
4. Enter your API keys in the Settings panel for whichever providers you want to use
5. For Ollama: install [Ollama](https://ollama.ai) locally and pull a model (`ollama pull llama3`) — no API key needed

---

## Requirements

- Unity 2022.3 LTS or later
- .NET 4.x scripting backend (default in Unity 2022+)
- API keys for whichever cloud providers you use (OpenAI, Anthropic, Gemini)
- Ollama installed locally for offline/local model support
