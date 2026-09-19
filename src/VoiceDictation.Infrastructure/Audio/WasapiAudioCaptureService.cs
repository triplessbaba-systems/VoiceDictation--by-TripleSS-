using System.IO;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using VoiceDictation.Core.Interfaces;
using VoiceDictation.Core.Models;

namespace VoiceDictation.Infrastructure.Audio;

public sealed class WasapiAudioCaptureService : IAudioCaptureService
{
    private WasapiCapture? _capture;
    private MemoryStream? _recordedStream;
    private WaveFileWriter? _waveWriter;
    private readonly object _syncLock = new();
    private bool _isCapturing;

    public bool IsCapturing
    {
        get
        {
            lock (_syncLock)
            {
                return _isCapturing;
            }
        }
    }

    public event EventHandler<AudioChunk>? AudioChunkAvailable;

    public IReadOnlyList<AudioDeviceInfo> GetInputDevices()
    {
        var devices = new List<AudioDeviceInfo>();
        using var enumerator = new MMDeviceEnumerator();
        var defaultDevice = enumerator.HasDefaultAudioEndpoint(DataFlow.Capture, Role.Communications)
            ? enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications)
            : null;

        var endpoints = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
        foreach (var endpoint in endpoints)
        {
            var isDefault = defaultDevice is not null && endpoint.ID == defaultDevice.ID;
            devices.Add(new AudioDeviceInfo(endpoint.ID, endpoint.FriendlyName, isDefault));
        }

        return devices;
    }

    public void StartCapture(string? deviceId = null)
    {
        lock (_syncLock)
        {
            if (_isCapturing)
            {
                return;
            }

            MMDevice selectedDevice;
            using (var enumerator = new MMDeviceEnumerator())
            {
                if (!string.IsNullOrEmpty(deviceId))
                {
                    try
                    {
                        selectedDevice = enumerator.GetDevice(deviceId);
                    }
                    catch
                    {
                        selectedDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
                    }
                }
                else
                {
                    selectedDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
                }
            }

            _capture = new WasapiCapture(selectedDevice)
            {
                ShareMode = AudioClientShareMode.Shared
            };

            var targetFormat = new WaveFormat(16000, 16, 1);
            _recordedStream = new MemoryStream();
            _waveWriter = new WaveFileWriter(_recordedStream, targetFormat);

            _capture.DataAvailable += OnCaptureDataAvailable;
            _capture.RecordingStopped += OnCaptureRecordingStopped;

            _capture.StartRecording();
            _isCapturing = true;
        }
    }

    private void OnCaptureDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (e.BytesRecorded == 0 || _capture is null || _waveWriter is null)
        {
            return;
        }

        var sourceFormat = _capture.WaveFormat;
        var samples = ConvertToFloatSamples(e.Buffer, e.BytesRecorded, sourceFormat);

        var peak = 0.0f;
        for (var i = 0; i < samples.Length; i++)
        {
            var abs = Math.Abs(samples[i]);
            if (abs > peak)
            {
                peak = abs;
            }
        }

        var chunk = new AudioChunk(samples, peak, DateTime.UtcNow);
        AudioChunkAvailable?.Invoke(this, chunk);

        WriteResampledToWav(samples);
    }

    private void WriteResampledToWav(float[] samples)
    {
        if (_waveWriter is null || _capture is null)
        {
            return;
        }

        var sourceRate = _capture.WaveFormat.SampleRate;
        var targetRate = 16000;

        if (sourceRate == targetRate)
        {
            foreach (var sample in samples)
            {
                var clamped = Math.Clamp(sample, -1.0f, 1.0f);
                var pcm16 = (short)(clamped * 32767f);
                _waveWriter.WriteSample(clamped);
            }
        }
        else
        {
            var step = (double)sourceRate / targetRate;
            for (double pos = 0; pos < samples.Length; pos += step)
            {
                var index = (int)pos;
                if (index < samples.Length)
                {
                    var clamped = Math.Clamp(samples[index], -1.0f, 1.0f);
                    _waveWriter.WriteSample(clamped);
                }
            }
        }
    }

    private static float[] ConvertToFloatSamples(byte[] buffer, int bytesRecorded, WaveFormat format)
    {
        if (format.Encoding == WaveFormatEncoding.IeeeFloat)
        {
            var floatCount = bytesRecorded / 4;
            var channelCount = format.Channels;
            var monoCount = floatCount / channelCount;
            var result = new float[monoCount];

            for (var i = 0; i < monoCount; i++)
            {
                var sum = 0.0f;
                for (var ch = 0; ch < channelCount; ch++)
                {
                    var byteIndex = (i * channelCount + ch) * 4;
                    sum += BitConverter.ToSingle(buffer, byteIndex);
                }
                result[i] = sum / channelCount;
            }
            return result;
        }
        else if (format.BitsPerSample == 16)
        {
            var sampleCount = bytesRecorded / 2;
            var channelCount = format.Channels;
            var monoCount = sampleCount / channelCount;
            var result = new float[monoCount];

            for (var i = 0; i < monoCount; i++)
            {
                var sum = 0.0f;
                for (var ch = 0; ch < channelCount; ch++)
                {
                    var byteIndex = (i * channelCount + ch) * 2;
                    var val = BitConverter.ToInt16(buffer, byteIndex);
                    sum += val / 32768.0f;
                }
                result[i] = sum / channelCount;
            }
            return result;
        }

        return Array.Empty<float>();
    }

    private void OnCaptureRecordingStopped(object? sender, StoppedEventArgs e)
    {
        lock (_syncLock)
        {
            _isCapturing = false;
        }
    }

    public async Task<byte[]> StopCaptureAndGetWavAsync()
    {
        TaskCompletionSource<bool> tcs = new();

        lock (_syncLock)
        {
            if (!_isCapturing || _capture is null)
            {
                return _recordedStream?.ToArray() ?? Array.Empty<byte>();
            }

            _capture.RecordingStopped += (_, _) => tcs.TrySetResult(true);
            _capture.StopRecording();
            _isCapturing = false;
        }

        await Task.WhenAny(tcs.Task, Task.Delay(1000)).ConfigureAwait(false);

        lock (_syncLock)
        {
            if (_waveWriter is not null)
            {
                _waveWriter.Flush();
                _waveWriter.Dispose();
                _waveWriter = null;
            }

            _capture?.Dispose();
            _capture = null;

            var result = _recordedStream?.ToArray() ?? Array.Empty<byte>();
            _recordedStream?.Dispose();
            _recordedStream = null;

            return result;
        }
    }

    public void Dispose()
    {
        lock (_syncLock)
        {
            if (_isCapturing && _capture is not null)
            {
                _capture.StopRecording();
            }

            _waveWriter?.Dispose();
            _waveWriter = null;

            _recordedStream?.Dispose();
            _recordedStream = null;

            _capture?.Dispose();
            _capture = null;
            _isCapturing = false;
        }
    }
}
