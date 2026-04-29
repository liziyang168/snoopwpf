// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedMember.Global
// ReSharper disable IdentifierTypo
#pragma warning disable CA1008
#pragma warning disable CA1028
#pragma warning disable CA1045
#pragma warning disable CA1051
#pragma warning disable CA1401
#pragma warning disable CA1806
#pragma warning disable CA1815
#pragma warning disable CA1819
#pragma warning disable CA2101

namespace Snoop.Infrastructure;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Serialization;
using global::Windows.Win32;
using global::Windows.Win32.Foundation;

public static partial class NativeMethods
{
    public static IntPtr[] TopLevelWindows
    {
        get
        {
            var windowList = new List<IntPtr>();
            var handle = GCHandle.Alloc(windowList);
            try
            {
                EnumWindows(EnumWindowsCallback, (IntPtr)handle);
            }
            finally
            {
                handle.Free();
            }

            return windowList.ToArray();
        }
    }

    public static Dictionary<int, IList<IntPtr>> GetProcessesAndWindows()
    {
        var map = new Dictionary<int, IList<IntPtr>>();
        var rootWindows = TopLevelWindows;

        foreach (var rootWindow in rootWindows)
        {
            GetWindowThreadProcessId(rootWindow, out var processId);

            if (map.TryGetValue(processId, out var windows) == false)
            {
                windows = new List<IntPtr>();
                map.Add(processId, windows);
            }

            windows.Add(rootWindow);
        }

        return map;
    }

    public static List<IntPtr> GetRootWindowsOfProcess(int pid)
    {
        var rootWindows = TopLevelWindows;
        return GetRootWindowsOfProcess(pid, rootWindows);
    }

    public static List<IntPtr> GetRootWindowsOfProcess(int pid, IntPtr[] rootWindows)
    {
        var dsProcRootWindows = new List<IntPtr>();

        foreach (var hWnd in rootWindows)
        {
            GetWindowThreadProcessId(hWnd, out var processId);
            if (processId == pid)
            {
                dsProcRootWindows.Add(hWnd);
            }
        }

        return dsProcRootWindows;
    }

    private delegate bool EnumWindowsCallBackDelegate(IntPtr hwnd, IntPtr lParam);

    private static bool EnumWindowsCallback(IntPtr hwnd, IntPtr lParam)
    {
        var target = ((GCHandle)lParam).Target;

        if (target is not List<IntPtr> intPtrs)
        {
            return false;
        }

        intPtrs.Add(hwnd);

        return true;
    }

    public static bool IsProcessElevated(Process process)
    {
        using (var processHandle = OpenProcess(process, ProcessAccessFlags.QueryInformation))
        {
            if (processHandle.IsInvalid)
            {
                var error = Marshal.GetLastWin32Error();

                return error == ERROR_ACCESS_DENIED;
            }

            return false;
        }
    }

    [DllImport("user32.dll")]
    private static extern int EnumWindows(EnumWindowsCallBackDelegate callback, IntPtr lParam);

    [DllImport("Kernel32.dll")]
    public static extern int GetProcessId(ProcessHandle processHandle);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindowVisible(IntPtr hwnd);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern int GetClassName(IntPtr hwnd, StringBuilder className, int maxCount);

    public static string GetClassName(IntPtr hwnd)
    {
        // Pre-allocate 256 characters, since this is the maximum class name length.
        var className = new StringBuilder(256);

        //Get the window class name
        var result = GetClassName(hwnd, className, className.Capacity);

        return result != 0
            ? className.ToString()
            : string.Empty;
    }

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int maxCount);

    public static string GetText(IntPtr hWnd)
    {
        // Allocate correct string length first
        var length = GetWindowTextLength(hWnd);
        var sb = new StringBuilder(length + 1);
        GetWindowText(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }

    [DllImport("kernel32.dll")]
    public static extern void GetCurrentThreadStackLimits(out IntPtr lowLimit, out IntPtr highLimit);

    [DllImport("user32.dll")]
    public static extern IntPtr LoadImage(IntPtr hinst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

    public static IntPtr GetWindowUnderMouse()
    {
        var pt = default(POINT);
        if (GetCursorPos(ref pt))
        {
            return WindowFromPoint(pt);
        }

        return IntPtr.Zero;
    }

    //public static System.Windows.Rect GetWindowRect(IntPtr hwnd)
    //{
    //  RECT rect = new RECT();
    //  GetWindowRect(hwnd, out rect);
    //  return new System.Windows.Rect(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
    //}

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(ref POINT pt);

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(POINT Point);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    public static extern bool SetWindowPlacement(IntPtr hWnd, [In] ref WINDOWPLACEMENT lpwndpl);

    [DllImport("user32.dll")]
    public static extern bool GetWindowPlacement(IntPtr hWnd, out WINDOWPLACEMENT lpwndpl);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    public const int SW_SHOWNORMAL = 1;
    public const int SW_SHOWMINIMIZED = 2;
    public const int SW_SHOWMAXIMIZED = 3;

    public enum HookType
    {
        WH_JOURNALRECORD = 0,
        WH_JOURNALPLAYBACK = 1,
        WH_KEYBOARD = 2,
        WH_GETMESSAGE = 3,
        WH_CALLWNDPROC = 4,
        WH_CBT = 5,
        WH_SYSMSGFILTER = 6,
        WH_MOUSE = 7,
        WH_HARDWARE = 8,
        WH_DEBUG = 9,
        WH_SHELL = 10,
        WH_FOREGROUNDIDLE = 11,
        WH_CALLWNDPROCRET = 12,
        WH_KEYBOARD_LL = 13,
        WH_MOUSE_LL = 14
    }

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetWindowsHookEx(HookType hookType, UIntPtr lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetWindowsHookEx(HookType hookType, HookProc hookProc, IntPtr hMod, uint dwThreadId);

    public delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnhookWindowsHookEx(IntPtr hhk);

    /// <summary>
    /// Try to get the relative mouse position to the given handle in client coordinates.
    /// </summary>
    /// <param name="hWnd">The handle for this method.</param>
    /// <param name="point">The relative mouse position to the given handle.</param>
    public static unsafe bool TryGetRelativeMousePosition(IntPtr hWnd, out POINT point)
    {
        point = default;

        var returnValue = hWnd != IntPtr.Zero
                          && TryGetPhysicalCursorPos(out point);

        if (returnValue)
        {
            ScreenToClient(hWnd, ref point);
        }

        return returnValue;
    }

    public static bool TryGetPhysicalCursorPos(out POINT pt)
    {
        var returnValue = _GetPhysicalCursorPos(out pt);
        // Sometimes Win32 will fail this call, such as if you are
        // not running in the interactive desktop. For example,
        // a secure screen saver may be running.
        if (!returnValue)
        {
            System.Diagnostics.Debug.WriteLine("GetPhysicalCursorPos failed!");
            pt.X = 0;
            pt.Y = 0;
        }

        return returnValue;
    }

    [DllImport("user32.dll", CharSet = CharSet.None, SetLastError = true, EntryPoint = "ScreenToClient")]
    private static extern bool ScreenToClient(IntPtr hWnd, ref POINT point);

    [DllImport("user32.dll", EntryPoint = "GetPhysicalCursorPos", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
#pragma warning disable SA1300
    private static extern bool _GetPhysicalCursorPos(out POINT lpPoint);
#pragma warning restore SA1300
}

// RECT structure required by WINDOWPLACEMENT structure
[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct RECT
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;

    public RECT(int left, int top, int right, int bottom)
    {
        this.Left = left;
        this.Top = top;
        this.Right = right;
        this.Bottom = bottom;
    }

    public int Width => this.Right - this.Left;

    public int Height => this.Bottom - this.Top;
}

// POINT structure required by WINDOWPLACEMENT structure
[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct POINT
{
    public int X;
    public int Y;

    public POINT(int x, int y)
    {
        this.X = x;
        this.Y = y;
    }
}

// WINDOWPLACEMENT stores the position, size, and state of a window
[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct WINDOWPLACEMENT
{
    [XmlIgnore]
    public int Length;
    [XmlIgnore]
    public int Flags;
    public int ShowCmd;
    [XmlIgnore]
    public POINT MinPosition;
    [XmlIgnore]
    public POINT MaxPosition;
    public RECT NormalPosition;
}