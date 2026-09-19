using System.Diagnostics;
using System.Runtime.InteropServices;
using VoiceDictation.Core.Interfaces;
using VoiceDictation.Core.Models;
using VoiceDictation.Infrastructure.Native;

namespace VoiceDictation.Infrastructure.Input;

public sealed class LowLevelKeyboardHookService : IGlobalHookService
{
    private IntPtr _hookId = IntPtr.Zero;
    private Win32Imports.LowLevelKeyboardProc? _proc;
    private uint _targetVkCode = 0x77;
    private TriggerMode _mode = TriggerMode.PushToTalk;
    private bool _isKeyPressed;
    private bool _isToggleActive;
    private readonly object _syncRoot = new();

    public event EventHandler? TriggerPressed;
    public event EventHandler? TriggerReleased;

    public bool IsHookActive
    {
        get
        {
            lock (_syncRoot)
            {
                return _hookId != IntPtr.Zero;
            }
        }
    }

    public void StartHook(string keyName, TriggerMode mode)
    {
        lock (_syncRoot)
        {
            if (_hookId != IntPtr.Zero)
            {
                return;
            }

            _mode = mode;
            _targetVkCode = ResolveVkCode(keyName);
            _proc = HookCallback;

            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule;
            var moduleHandle = Win32Imports.GetModuleHandle(curModule?.ModuleName);
            _hookId = Win32Imports.SetWindowsHookEx(Win32Imports.WH_KEYBOARD_LL, _proc, moduleHandle, 0);

            if (_hookId == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Failed to install low-level keyboard hook: {Marshal.GetLastWin32Error()}");
            }
        }
    }

    public void StopHook()
    {
        lock (_syncRoot)
        {
            if (_hookId == IntPtr.Zero)
            {
                return;
            }

            Win32Imports.UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
            _proc = null;
            _isKeyPressed = false;
            _isToggleActive = false;
        }
    }

    public void UpdateTriggerKey(string keyName, TriggerMode mode)
    {
        lock (_syncRoot)
        {
            _targetVkCode = ResolveVkCode(keyName);
            _mode = mode;
            _isKeyPressed = false;
            _isToggleActive = false;
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var hookStruct = Marshal.PtrToStructure<Win32Imports.KBDLLHOOKSTRUCT>(lParam);
            var msg = (int)wParam;

            if (hookStruct.vkCode == _targetVkCode)
            {
                var isDown = msg is Win32Imports.WM_KEYDOWN or Win32Imports.WM_SYSKEYDOWN;
                var isUp = msg is Win32Imports.WM_KEYUP or Win32Imports.WM_SYSKEYUP;

                if (_mode == TriggerMode.PushToTalk)
                {
                    if (isDown && !_isKeyPressed)
                    {
                        _isKeyPressed = true;
                        ThreadPool.QueueUserWorkItem(_ => TriggerPressed?.Invoke(this, EventArgs.Empty));
                        return (IntPtr)1;
                    }
                    else if (isUp && _isKeyPressed)
                    {
                        _isKeyPressed = false;
                        ThreadPool.QueueUserWorkItem(_ => TriggerReleased?.Invoke(this, EventArgs.Empty));
                        return (IntPtr)1;
                    }
                }
                else
                {
                    if (isDown && !_isKeyPressed)
                    {
                        _isKeyPressed = true;
                        _isToggleActive = !_isToggleActive;

                        if (_isToggleActive)
                        {
                            ThreadPool.QueueUserWorkItem(_ => TriggerPressed?.Invoke(this, EventArgs.Empty));
                        }
                        else
                        {
                            ThreadPool.QueueUserWorkItem(_ => TriggerReleased?.Invoke(this, EventArgs.Empty));
                        }
                        return (IntPtr)1;
                    }
                    else if (isUp)
                    {
                        _isKeyPressed = false;
                        return (IntPtr)1;
                    }
                }
            }
        }

        return Win32Imports.CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private static uint ResolveVkCode(string keyName) => keyName.Trim().ToUpperInvariant() switch
    {
        "CAPSLOCK" => 0x14,
        "F1" => 0x70,
        "F2" => 0x71,
        "F3" => 0x72,
        "F4" => 0x73,
        "F5" => 0x74,
        "F6" => 0x75,
        "F7" => 0x76,
        "F8" => 0x77,
        "F9" => 0x78,
        "F10" => 0x79,
        "F11" => 0x7A,
        "F12" => 0x7B,
        "INSERT" => 0x2D,
        "PAUSE" => 0x13,
        "SCROLLLOCK" => 0x91,
        "RIGHTCTRL" => 0xA3,
        "RIGHTALT" => 0xA5,
        _ => 0x77
    };

    public void Dispose()
    {
        StopHook();
    }
}
