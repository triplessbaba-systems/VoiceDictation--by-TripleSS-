namespace VoiceDictation.Core.Models;

public readonly record struct AudioChunk(
    ReadOnlyMemory<float> Samples,
    float PeakAmplitude,
    DateTime Timestamp
);
