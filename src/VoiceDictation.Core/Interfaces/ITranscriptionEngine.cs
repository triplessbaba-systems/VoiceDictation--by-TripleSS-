using VoiceDictation.Core.Models;

namespace VoiceDictation.Core.Interfaces;

public interface ITranscriptionEngine : IAsyncDisposable
{
    bool IsModelLoaded { get; }
    Task InitializeAsync(string modelPath, CancellationToken cancellationToken = default);
    Task<TranscriptionResult> TranscribeAsync(byte[] wavData, string language, CancellationToken cancellationToken = default);
}
