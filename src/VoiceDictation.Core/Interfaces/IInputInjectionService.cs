using VoiceDictation.Core.Models;

namespace VoiceDictation.Core.Interfaces;

public interface IInputInjectionService
{
    Task InjectTextAsync(string text, InjectionMode mode, IntPtr targetWindow = default, CancellationToken cancellationToken = default);
}
