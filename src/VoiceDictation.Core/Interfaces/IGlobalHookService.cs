using VoiceDictation.Core.Models;

namespace VoiceDictation.Core.Interfaces;

public interface IGlobalHookService : IDisposable
{
    event EventHandler? TriggerPressed;
    event EventHandler? TriggerReleased;
    bool IsHookActive { get; }
    void StartHook(string keyName, TriggerMode mode);
    void StopHook();
    void UpdateTriggerKey(string keyName, TriggerMode mode);
}
