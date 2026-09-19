namespace VoiceDictation.Core.Models;

public sealed record TranscriptionResult(
    string Text,
    string Language,
    TimeSpan Duration,
    bool IsSuccess,
    string? ErrorMessage = null
);
