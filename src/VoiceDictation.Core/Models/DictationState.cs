namespace VoiceDictation.Core.Models;

public enum DictationState
{
    Idle,
    Recording,
    Transcribing,
    Injecting,
    Faulted
}
