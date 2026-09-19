using VoiceDictation.Core.Models;

namespace VoiceDictation.Core.Interfaces;

public interface IAudioCaptureService : IDisposable
{
    bool IsCapturing { get; }
    event EventHandler<AudioChunk>? AudioChunkAvailable;
    void StartCapture(string? deviceId = null);
    Task<byte[]> StopCaptureAndGetWavAsync();
    IReadOnlyList<AudioDeviceInfo> GetInputDevices();
}

public sealed record AudioDeviceInfo(string Id, string Name, bool IsDefault);
