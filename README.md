# VoiceDictation

VoiceDictation is a high-performance, system-wide local speech-to-text dictation utility built with .NET 10 (C#) and WPF. It captures microphone audio when a global hotkey is pressed, runs local transcription via embedded Whisper.net (GGML) models completely offline on CPU/GPU, and injects transcribed text directly into the active cursor position across any Windows application (Discord, Notepad, Visual Studio, Word, Chrome, etc.).

---

## Key Features

- **100% Offline & Private:** Powered by local Whisper GGML models. Zero audio or transcript data ever leaves your computer.
- **System-Wide Global Injection:** Works universally in any application using low-level Win32 `SendInput` (Unicode Direct Mode) or Clipboard swap injection.
- **Non-Activating Floating Capsule UI:** Minimal Antigravity Dark HUD (`#0d1117`, `#161b22`, `#58a6ff`) with real-time audio amplitude waveforms that never steals keyboard focus from your active target window.
- **Windows System Tray Integration:** Sits quietly in the notification area (Gizli Simgeler) with quick access to Settings, Capsule toggle, and Exit.
- **Single-File Self-Contained Distribution:** Embedded unmanaged C++ Whisper runtime libraries (`whisper.dll`, `ggml-cpu-whisper.dll`, etc.) automatically deployed on startup. No prerequisites, runtimes, or external installers required.
- **Customizable Controls:**
  - Configurable hotkey (Default: `Insert` or `F8`)
  - Push-to-Talk or Toggle mode
  - Language selection (Turkish, English, Multilingual Auto-Detect)
  - Microphone device and Whisper model selection (`base`, `small`, etc.)

---

## Architecture Overview

```
VoiceDictation/
├── src/
│   ├── VoiceDictation.Core/              # Domain Models, Finite State Machine, Abstractions
│   │   ├── Models/                      # DictationState, AudioChunk, TranscriptionResult, AppSettings
│   │   ├── State/                       # DictationStateMachine (Idle -> Listening -> Transcribing -> Injecting)
│   │   └── Interfaces/                  # Contracts for Audio, Transcription, Input, Hooks
│   ├── VoiceDictation.Infrastructure/    # Concrete Hardware & OS Implementations
│   │   ├── Audio/                       # WasapiAudioCaptureService (16kHz mono PCM resampling)
│   │   ├── Transcription/               # WhisperTranscriptionEngine (Whisper.net GGML runner)
│   │   ├── Input/                       # LowLevelKeyboardHookService (WH_KEYBOARD_LL), WindowsInputInjectionService
│   │   ├── Native/                      # Win32 P/Invoke declarations, NativeLibraryBootstrapper
│   │   └── Configuration/               # JsonSettingsService (%APPDATA%\VoiceDictation\settings.json)
│   └── VoiceDictation.App/               # Presentation Layer (WPF / MVVM)
│       ├── ViewModels/                  # CapsuleViewModel, SettingsViewModel, RelayCommand
│       ├── Views/                       # CapsuleWindow, SettingsWindow
│       ├── Services/                    # TrayIconManager (NotifyIcon integration)
│       └── Styles/                      # Antigravity Dark tokens, vector geometries
└── tests/
    └── VoiceDictation.Tests/            # xUnit Test Suite (FSM, Audio Resampling, Win32 Struct Sizes, Bootstrapper)
```

---

## Prerequisites

- **Development:** Windows 10/11 x64, .NET 10 SDK
- **Runtime:** Windows 10/11 x64 (Standalone single-file package has zero dependencies)

---

## Getting Started

### 1. Build and Run from Source

```powershell
# Clone repository
git clone https://github.com/triplessbaba-systems/VoiceDictation.git
cd VoiceDictation

# Build solution
dotnet build VoiceDictation.slnx

# Run tests
dotnet test VoiceDictation.slnx

# Launch application
dotnet run --project src/VoiceDictation.App
```

### 2. Publish Standalone Single-File Executable

To generate a standalone `.exe` that can be transferred and executed on any Windows 10/11 PC without .NET installed:

```powershell
dotnet publish src/VoiceDictation.App/VoiceDictation.App.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o ./publish-standalone
```

The compiled binary will be located at `./publish-standalone/VoiceDictation.App.exe`.

---

## Configuration

Settings are persisted in `%APPDATA%\VoiceDictation\settings.json`:

```json
{
  "ShortcutKey": "Insert",
  "TriggerMode": 0,
  "InjectionMode": 0,
  "SelectedLanguage": "tr",
  "ModelName": "ggml-base.bin",
  "SilenceThreshold": 0.015,
  "AudioInputDeviceIndex": -1
}
```

---

## License

MIT License
