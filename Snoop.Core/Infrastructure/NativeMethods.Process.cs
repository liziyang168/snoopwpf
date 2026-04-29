namespace Snoop.Infrastructure;

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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.Win32.SafeHandles;

public static partial class NativeMethods
{
    [DllImport("user32.dll")]
    public static extern int GetWindowThreadProcessId(IntPtr hwnd, out int processId);

      private enum ImageFileMachine : ushort
    {
        I386 = 0x14C,
        AMD64 = 0x8664,
        ARM = 0x1c0,
        ARM64 = 0xAA64,
    }

    [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWow64Process([In] IntPtr process, [Out] out bool wow64Process);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool IsWow64Process2(IntPtr process, out ImageFileMachine processMachine, out ImageFileMachine nativeMachine);

    public static string GetArchitectureWithoutException(Process process)
    {
        try
        {
            return GetArchitecture(process);
        }
        catch (Exception exception)
        {
            LogHelper.WriteError(exception);
            return Environment.Is64BitOperatingSystem
                ? "x64"
                : "x86";
        }
    }

    public static string GetArchitecture(Process process)
    {
        using (var processHandle = OpenProcess(process, ProcessAccessFlags.QueryLimitedInformation))
        {
            if (processHandle.IsInvalid)
            {
                throw new Exception("Could not query process information.");
            }

            try
            {
                if (IsWow64Process2(processHandle.DangerousGetHandle(), out var processMachine, out var nativeMachine) == false)
                {
                    throw new Win32Exception();
                }

                var arch = processMachine == 0
                    ? nativeMachine
                    : processMachine;

                switch (arch)
                {
                    case ImageFileMachine.I386:
                        return "x86";

                    case ImageFileMachine.AMD64:
                        return "x64";

                    case ImageFileMachine.ARM:
                        return "ARM";

                    case ImageFileMachine.ARM64:
                        return "ARM64";

                    default:
                        return "x86";
                }
            }
            catch (EntryPointNotFoundException)
            {
                if (IsWow64Process(processHandle.DangerousGetHandle(), out var isWow64) == false)
                {
                    throw new Win32Exception();
                }

                switch (isWow64)
                {
                    case true when Environment.Is64BitOperatingSystem:
                        return "x86";

                    case false when Environment.Is64BitOperatingSystem:
                        return "x64";

                    default:
                        return "x86";
                }
            }
        }
    }

    // see https://msdn.microsoft.com/en-us/library/windows/desktop/ms684139%28v=vs.85%29.aspx
    private static bool IsWow64Process(Process process)
    {
        if (Environment.Is64BitOperatingSystem == false)
        {
            return false;
        }

        // if this method is not available in your version of .NET, use GetNativeSystemInfo via P/Invoke instead
        using (var processHandle = OpenProcess(process, ProcessAccessFlags.QueryLimitedInformation))
        {
            if (processHandle.IsInvalid)
            {
                throw new Exception("Could not query process information.");
            }

            if (IsWow64Process(processHandle.DangerousGetHandle(), out var isWow64) == false)
            {
                throw new Win32Exception();
            }

            return isWow64 == false;
        }
    }

    [Flags]
    public enum ProcessAccessFlags : uint
    {
        All = 0x001F0FFF,
        Terminate = 0x00000001,
        CreateThread = 0x00000002,
        VirtualMemoryOperation = 0x00000008,
        VirtualMemoryRead = 0x00000010,
        VirtualMemoryWrite = 0x00000020,
        DuplicateHandle = 0x00000040,
        CreateProcess = 0x000000080,
        SetQuota = 0x00000100,
        SetInformation = 0x00000200,
        QueryInformation = 0x00000400,
        QueryLimitedInformation = 0x00001000,
        Synchronize = 0x00100000
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [SuppressUnmanagedCodeSecurity]
    public static extern ProcessHandle OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, int processId);

    public static ProcessHandle OpenProcess(Process proc, ProcessAccessFlags flags)
    {
        return OpenProcess(flags, false, proc.Id);
    }

    [DebuggerDisplay("{" + nameof(szModule) + "}")]
    [StructLayout(LayoutKind.Sequential)]
    public struct MODULEENTRY32
    {
        public uint dwSize;
        public uint th32ModuleID;
        public uint th32ProcessID;
        public uint GlblcntUsage;
        public uint ProccntUsage;
        public readonly IntPtr modBaseAddr;
        public uint modBaseSize;
        public readonly IntPtr hModule;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szModule;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szExePath;
    }

    /// <summary>
    /// Similar to System.Diagnostics.WinProcessManager.GetModuleInfos,
    /// except that we include 32 bit modules when Snoop runs in 64 bit mode.
    /// See http://blogs.msdn.com/b/jasonz/archive/2007/05/11/code-sample-is-your-process-using-the-silverlight-clr.aspx
    /// </summary>
    public static IEnumerable<MODULEENTRY32> GetModulesFromWindowHandle(IntPtr windowHandle)
    {
        GetWindowThreadProcessId(windowHandle, out var processId);

        return GetModules(processId);
    }

    /// <summary>
    /// Similar to System.Diagnostics.WinProcessManager.GetModuleInfos,
    /// except that we include 32 bit modules when Snoop runs in 64 bit mode.
    /// See http://blogs.msdn.com/b/jasonz/archive/2007/05/11/code-sample-is-your-process-using-the-silverlight-clr.aspx
    /// </summary>
    public static IEnumerable<MODULEENTRY32> GetModules(Process process)
    {
        return GetModules(process.Id);
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern ToolHelpHandle CreateToolhelp32Snapshot(SnapshotFlags dwFlags, int th32ProcessID);

    [DllImport("kernel32.dll")]
    public static extern bool Module32First(ToolHelpHandle hSnapshot, ref MODULEENTRY32 lpme);

    [DllImport("kernel32.dll")]
    public static extern bool Module32Next(ToolHelpHandle hSnapshot, ref MODULEENTRY32 lpme);

    public class ToolHelpHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private ToolHelpHandle()
            : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return CloseHandle(this.handle);
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool CloseHandle(IntPtr hHandle);

    [Flags]
    public enum SnapshotFlags : uint
    {
        HeapList = 0x00000001,
        Process = 0x00000002,
        Thread = 0x00000004,
        Module = 0x00000008,
        Module32 = 0x00000010,
        Inherit = 0x80000000,
        All = 0x0000001F
    }

    /// <summary>
    /// Similar to System.Diagnostics.WinProcessManager.GetModuleInfos,
    /// except that we include 32 bit modules when Snoop runs in 64 bit mode.
    /// See http://blogs.msdn.com/b/jasonz/archive/2007/05/11/code-sample-is-your-process-using-the-silverlight-clr.aspx
    /// </summary>
    public static IEnumerable<MODULEENTRY32> GetModules(int processId)
    {
        var me32 = default(MODULEENTRY32);
        var hModuleSnap = CreateToolhelp32Snapshot(SnapshotFlags.Module | SnapshotFlags.Module32, processId);

        if (hModuleSnap.IsInvalid)
        {
            yield break;
        }

        using (hModuleSnap)
        {
            me32.dwSize = (uint)Marshal.SizeOf(me32);

            if (Module32First(hModuleSnap, ref me32))
            {
                do
                {
                    yield return me32;
                }
                while (Module32Next(hModuleSnap, ref me32));
            }
        }
    }

    public class ProcessHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private ProcessHandle()
            : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return CloseHandle(this.handle);
        }
    }
}