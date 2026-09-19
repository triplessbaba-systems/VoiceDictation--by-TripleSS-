namespace VoiceDictation.Core.Models;

public enum TriggerMode
{
    PushToTalk,
    Toggle
}

public enum InjectionMode
{
    DirectSendInput,
    ClipboardHybrid
}

public sealed class AppSettings
{
    public TriggerMode TriggerMode { get; set; } = TriggerMode.PushToTalk;
    public InjectionMode InjectionMode { get; set; } = InjectionMode.DirectSendInput;
    public string ShortcutKey { get; set; } = "F8";
    public string SelectedLanguage { get; set; } = "tr";
    public string ModelName { get; set; } = "ggml-base.bin";
    public string SelectedAudioDeviceId { get; set; } = string.Empty;
    public float SilenceThreshold { get; set; } = 0.015f;
    public bool ShowAudioWaveform { get; set; } = true;
    public bool PlayAudioCue { get; set; } = false;
}
