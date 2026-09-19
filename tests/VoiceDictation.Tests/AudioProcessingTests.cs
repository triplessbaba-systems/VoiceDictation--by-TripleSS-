using System.Runtime.InteropServices;
using System.Text.Json;
using VoiceDictation.Core.Models;
using VoiceDictation.Infrastructure.Native;
using Xunit;

namespace VoiceDictation.Tests;

public sealed class AudioProcessingTests
{
    [Fact]
    public void Win32_InputStructSize_ShouldMatchArchitecture()
    {
        var size = Marshal.SizeOf<Win32Imports.INPUT>();
        var expectedSize = Environment.Is64BitProcess ? 40 : 28;
        Assert.Equal(expectedSize, size);
    }

    [Fact]
    public void NativeLibraryBootstrapper_ShouldExtractFilesAndSetPaths()
    {
        NativeLibraryBootstrapper.EnsureNativeLibrariesInstalled();
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var targetWinX64Dir = Path.Combine(appData, "VoiceDictation", "runtimes", "win-x64");

        Assert.True(File.Exists(Path.Combine(targetWinX64Dir, "whisper.dll")));
        Assert.True(File.Exists(Path.Combine(targetWinX64Dir, "ggml-whisper.dll")));
        Assert.True(File.Exists(Path.Combine(targetWinX64Dir, "ggml-cpu-whisper.dll")));
        Assert.True(File.Exists(Path.Combine(targetWinX64Dir, "ggml-base-whisper.dll")));
    }

    [Fact]
    public void AudioChunk_ShouldStorePropertiesCorrectly()
    {
        var samples = new float[] { 0.1f, -0.5f, 0.8f, -0.2f };
        var timestamp = DateTime.UtcNow;
        var chunk = new AudioChunk(samples, 0.8f, timestamp);

        Assert.Equal(4, chunk.Samples.Length);
        Assert.Equal(0.8f, chunk.PeakAmplitude);
        Assert.Equal(timestamp, chunk.Timestamp);
    }

    [Fact]
    public void TranscriptionResult_SuccessRecord_ShouldInitializeCorrectly()
    {
        var duration = TimeSpan.FromMilliseconds(450);
        var result = new TranscriptionResult("Merhaba dunya", "tr", duration, true);

        Assert.True(result.IsSuccess);
        Assert.Equal("Merhaba dunya", result.Text);
        Assert.Equal("tr", result.Language);
        Assert.Equal(duration, result.Duration);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void AppSettings_SerializationRoundTrip_ShouldPreserveValues()
    {
        var original = new AppSettings
        {
            ShortcutKey = "CapsLock",
            TriggerMode = TriggerMode.Toggle,
            InjectionMode = InjectionMode.DirectSendInput,
            SelectedLanguage = "en",
            ModelName = "ggml-small.bin",
            SilenceThreshold = 0.02f
        };

        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<AppSettings>(json);

        Assert.NotNull(deserialized);
        Assert.Equal("CapsLock", deserialized.ShortcutKey);
        Assert.Equal(TriggerMode.Toggle, deserialized.TriggerMode);
        Assert.Equal(InjectionMode.DirectSendInput, deserialized.InjectionMode);
        Assert.Equal("en", deserialized.SelectedLanguage);
        Assert.Equal("ggml-small.bin", deserialized.ModelName);
        Assert.Equal(0.02f, deserialized.SilenceThreshold);
    }
}
