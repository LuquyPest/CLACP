using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace Clacp.Services;

public static class TypingService
{
    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_UNICODE = 0x0004;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint KEYEVENTF_SCANCODE = 0x0008;
    private const uint MAPVK_VK_TO_VSC = 0x00;
    private const ushort VK_TAB = 0x09;
    private const ushort VK_RETURN = 0x0D;
    private const ushort VK_SHIFT = 0x10;
    private const ushort VK_CONTROL = 0x11;
    private const ushort VK_MENU = 0x12;

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern short VkKeyScanEx(char ch, IntPtr dwhkl);

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKeyEx(uint uCode, uint uMapType, IntPtr dwhkl);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    private static readonly int InputSize = Marshal.SizeOf<INPUT>();

    public static void TypeText(string text, int delayMs = 8)
    {
        var threadId = GetWindowThreadProcessId(GetForegroundWindow(), out _);
        var layout = GetKeyboardLayout(threadId);

        foreach (var ch in text)
        {
            var inputs = ch switch
            {
                '\t' => BuildScanCodeKeyPair(VK_TAB, layout),
                '\n' or '\r' => BuildScanCodeKeyPair(VK_RETURN, layout),
                _ => BuildCharacterInputs(ch, layout),
            };

            SendInput((uint)inputs.Length, inputs, InputSize);

            if (delayMs > 0)
                Thread.Sleep(delayMs);
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetKeyboardLayout(uint idThread);

    private static INPUT[] BuildCharacterInputs(char ch, IntPtr layout)
    {
        var scanResult = VkKeyScanEx(ch, layout);

        // -1 means the character has no key on the active layout (e.g. an
        // unusual symbol) - fall back to a synthetic unicode packet for those.
        if (scanResult == -1)
            return BuildUnicodePair(ch);

        var vk = (ushort)(scanResult & 0xFF);
        var shiftState = (byte)(scanResult >> 8 & 0xFF);

        var shift = (shiftState & 0x01) != 0;
        var ctrl = (shiftState & 0x02) != 0;
        var alt = (shiftState & 0x04) != 0;

        var inputs = new List<INPUT>();

        if (shift) AddKeyDown(inputs, VK_SHIFT, layout);
        if (ctrl) AddKeyDown(inputs, VK_CONTROL, layout);
        if (alt) AddKeyDown(inputs, VK_MENU, layout);

        AddKeyDown(inputs, vk, layout);
        AddKeyUp(inputs, vk, layout);

        if (alt) AddKeyUp(inputs, VK_MENU, layout);
        if (ctrl) AddKeyUp(inputs, VK_CONTROL, layout);
        if (shift) AddKeyUp(inputs, VK_SHIFT, layout);

        return inputs.ToArray();
    }

    private static INPUT[] BuildScanCodeKeyPair(ushort vk, IntPtr layout)
    {
        var inputs = new List<INPUT>();
        AddKeyDown(inputs, vk, layout);
        AddKeyUp(inputs, vk, layout);
        return inputs.ToArray();
    }

    private static void AddKeyDown(List<INPUT> inputs, ushort vk, IntPtr layout)
        => inputs.Add(BuildScanCodeInput(vk, layout, keyUp: false));

    private static void AddKeyUp(List<INPUT> inputs, ushort vk, IntPtr layout)
        => inputs.Add(BuildScanCodeInput(vk, layout, keyUp: true));

    private static INPUT BuildScanCodeInput(ushort vk, IntPtr layout, bool keyUp)
    {
        var scanCode = (ushort)MapVirtualKeyEx(vk, MAPVK_VK_TO_VSC, layout);
        var flags = KEYEVENTF_SCANCODE | (keyUp ? KEYEVENTF_KEYUP : 0);

        return new INPUT
        {
            type = INPUT_KEYBOARD,
            U = new InputUnion { ki = new KEYBDINPUT { wVk = 0, wScan = scanCode, dwFlags = flags } },
        };
    }

    private static INPUT[] BuildUnicodePair(char ch)
    {
        return new[]
        {
            new INPUT { type = INPUT_KEYBOARD, U = new InputUnion { ki = new KEYBDINPUT { wScan = ch, dwFlags = KEYEVENTF_UNICODE } } },
            new INPUT { type = INPUT_KEYBOARD, U = new InputUnion { ki = new KEYBDINPUT { wScan = ch, dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP } } },
        };
    }
}
