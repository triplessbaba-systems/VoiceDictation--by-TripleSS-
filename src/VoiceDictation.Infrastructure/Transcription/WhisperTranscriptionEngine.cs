using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using Whisper.net;
using Whisper.net.Ggml;
using VoiceDictation.Core.Interfaces;
using VoiceDictation.Core.Models;
using VoiceDictation.Infrastructure.Native;

namespace VoiceDictation.Infrastructure.Transcription;

public sealed class WhisperTranscriptionEngine : ITranscriptionEngine
{
    private WhisperFactory? _factory;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private string _currentModelPath = string.Empty;

    public bool IsModelLoaded => _factory is not null;

    public async Task InitializeAsync(string modelPath, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (string.Equals(_currentModelPath, modelPath, StringComparison.OrdinalIgnoreCase) && _factory is not null)
            {
                return;
            }

            if (!File.Exists(modelPath))
            {
                var directory = Path.GetDirectoryName(modelPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using var httpClient = new HttpClient();
                var downloader = new WhisperGgmlDownloader(httpClient);
                using var modelStream = await downloader.GetGgmlModelAsync(GgmlType.Base, cancellationToken: cancellationToken).ConfigureAwait(false);
                using var fileStream = File.OpenWrite(modelPath);
                await modelStream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
            }

            NativeLibraryBootstrapper.EnsureNativeLibrariesInstalled();
            _factory?.Dispose();
            _factory = WhisperFactory.FromPath(modelPath);
            _currentModelPath = modelPath;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<TranscriptionResult> TranscribeAsync(byte[] wavData, string language, CancellationToken cancellationToken = default)
    {
        if (wavData.Length == 0)
        {
            return new TranscriptionResult(string.Empty, language, TimeSpan.Zero, true);
        }

        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_factory is null)
            {
                return new TranscriptionResult(string.Empty, language, TimeSpan.Zero, false, "Whisper model is not initialized.");
            }

            var stopwatch = Stopwatch.StartNew();

            using var processor = _factory.CreateBuilder()
                .WithLanguage(language)
                .Build();

            using var memoryStream = new MemoryStream(wavData);
            var stringBuilder = new StringBuilder();

            await foreach (var segment in processor.ProcessAsync(memoryStream, cancellationToken).ConfigureAwait(false))
            {
                if (!string.IsNullOrWhiteSpace(segment.Text))
                {
                    stringBuilder.Append(segment.Text.Trim()).Append(' ');
                }
            }

            stopwatch.Stop();
            var text = stringBuilder.ToString().Trim();

            return new TranscriptionResult(text, language, stopwatch.Elapsed, true);
        }
        catch (Exception ex)
        {
            return new TranscriptionResult(string.Empty, language, TimeSpan.Zero, false, ex.Message);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public ValueTask DisposeAsync()
    {
        _factory?.Dispose();
        _factory = null;
        _semaphore.Dispose();
        return ValueTask.CompletedTask;
    }
}
