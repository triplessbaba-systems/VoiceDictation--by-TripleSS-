using System.IO;
using System.Runtime.InteropServices;
using Whisper.net.LibraryLoader;

namespace VoiceDictation.Infrastructure.Native;

public static partial class NativeLibraryBootstrapper
{
    [LibraryImport("kernel32.dll", EntryPoint = "SetDllDirectoryW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetDllDirectory(string lpPathName);

    private static readonly string[] NativeFiles =
    [
        "ggml-base-whisper.dll",
        "ggml-cpu-whisper.dll",
        "ggml-whisper.dll",
        "whisper.dll"
    ];

    public static void EnsureNativeLibrariesInstalled()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var baseDir = Path.Combine(appData, "VoiceDictation");
        var runtimesDir = Path.Combine(baseDir, "runtimes");
        var targetWinX64Dir = Path.Combine(runtimesDir, "win-x64");

        if (!Directory.Exists(targetWinX64Dir))
        {
            Directory.CreateDirectory(targetWinX64Dir);
        }

        var assembly = typeof(NativeLibraryBootstrapper).Assembly;

        foreach (var file in NativeFiles)
        {
            var destinationPath = Path.Combine(targetWinX64Dir, file);
            var resourceName = $"VoiceDictation.Infrastructure.Native.runtimes.win_x64.{file}";

            using var resourceStream = assembly.GetManifestResourceStream(resourceName);
            if (resourceStream is not null)
            {
                if (!File.Exists(destinationPath) || new FileInfo(destinationPath).Length != resourceStream.Length)
                {
                    using var destinationStream = File.Create(destinationPath);
                    resourceStream.CopyTo(destinationStream);
                }
            }
        }

        SetDllDirectory(targetWinX64Dir);
        RuntimeOptions.LibraryPath = runtimesDir;
        RuntimeOptions.RuntimeLibraryOrder = [RuntimeLibrary.Cpu];
    }
}
