using System.Runtime.InteropServices;
using System.Windows;
using VoiceDictation.Core.Interfaces;
using VoiceDictation.Core.Models;
using VoiceDictation.Infrastructure.Native;

namespace VoiceDictation.Infrastructure.Input;

public sealed class WindowsInputInjectionService : IInputInjectionService
{
    public async Task InjectTextAsync(string text, InjectionMode mode, IntPtr targetWindow = default, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var trimmedText = text.Trim();

        if (targetWindow != IntPtr.Zero && Win32Imports.GetForegroundWindow() != targetWindow)
        {
            Win32Imports.SetForegroundWindow(targetWindow);
            await Task.Delay(60, cancellationToken).ConfigureAwait(false);
        }

        if (mode == InjectionMode.ClipboardHybrid)
        {
            await InjectViaClipboardAsync(trimmedText, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await InjectViaSendInputAsync(trimmedText, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task InjectViaClipboardAsync(string text, CancellationToken cancellationToken)
    {
        var staThread = new Thread(() =>
        {
            for (var i = 0; i < 5; i++)
            {
                try
                {
                    Clipboard.SetDataObject(text, true);
                    break;
                }
                catch
                {
                    Thread.Sleep(25);
                }
            }
        });
        staThread.SetApartmentState(ApartmentState.STA);
        staThread.Start();
        staThread.Join();

        await Task.Delay(50, cancellationToken).ConfigureAwait(false);
        SendCtrlV();
        await Task.Delay(50, cancellationToken).ConfigureAwait(false);
    }

    private static async Task InjectViaSendInputAsync(string text, CancellationToken cancellationToken)
    {
        var inputs = new List<Win32Imports.INPUT>(text.Length * 2);

        foreach (var ch in text)
        {
            var down = new Win32Imports.INPUT
            {
                type = Win32Imports.INPUT_KEYBOARD,
                u = new Win32Imports.InputUnion
                {
                    ki = new Win32Imports.KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = ch,
                        dwFlags = Win32Imports.KEYEVENTF_UNICODE,
                        time = 0,
                        dwExtraInfo = UIntPtr.Zero
                    }
                }
            };

            var up = new Win32Imports.INPUT
            {
                type = Win32Imports.INPUT_KEYBOARD,
                u = new Win32Imports.InputUnion
                {
                    ki = new Win32Imports.KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = ch,
                        dwFlags = Win32Imports.KEYEVENTF_UNICODE | Win32Imports.KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = UIntPtr.Zero
                    }
                }
            };

            inputs.Add(down);
            inputs.Add(up);
        }

        var inputArray = inputs.ToArray();
        var sent = Win32Imports.SendInput((uint)inputArray.Length, inputArray, Marshal.SizeOf<Win32Imports.INPUT>());
        if (sent == 0)
        {
            await InjectViaClipboardAsync(text, cancellationToken).ConfigureAwait(false);
        }
    }

    private static void SendCtrlV()
    {
        var inputs = new Win32Imports.INPUT[4];

        inputs[0] = new Win32Imports.INPUT
        {
            type = Win32Imports.INPUT_KEYBOARD,
            u = new Win32Imports.InputUnion
            {
                ki = new Win32Imports.KEYBDINPUT
                {
                    wVk = Win32Imports.VK_CONTROL,
                    wScan = 0,
                    dwFlags = 0,
                    time = 0,
                    dwExtraInfo = UIntPtr.Zero
                }
            }
        };

        inputs[1] = new Win32Imports.INPUT
        {
            type = Win32Imports.INPUT_KEYBOARD,
            u = new Win32Imports.InputUnion
            {
                ki = new Win32Imports.KEYBDINPUT
                {
                    wVk = Win32Imports.VK_V,
                    wScan = 0,
                    dwFlags = 0,
                    time = 0,
                    dwExtraInfo = UIntPtr.Zero
                }
            }
        };

        inputs[2] = new Win32Imports.INPUT
        {
            type = Win32Imports.INPUT_KEYBOARD,
            u = new Win32Imports.InputUnion
            {
                ki = new Win32Imports.KEYBDINPUT
                {
                    wVk = Win32Imports.VK_V,
                    wScan = 0,
                    dwFlags = Win32Imports.KEYEVENTF_KEYUP,
                    time = 0,
                    dwExtraInfo = UIntPtr.Zero
                }
            }
        };

        inputs[3] = new Win32Imports.INPUT
        {
            type = Win32Imports.INPUT_KEYBOARD,
            u = new Win32Imports.InputUnion
            {
                ki = new Win32Imports.KEYBDINPUT
                {
                    wVk = Win32Imports.VK_CONTROL,
                    wScan = 0,
                    dwFlags = Win32Imports.KEYEVENTF_KEYUP,
                    time = 0,
                    dwExtraInfo = UIntPtr.Zero
                }
            }
        };

        Win32Imports.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Win32Imports.INPUT>());
    }
}
