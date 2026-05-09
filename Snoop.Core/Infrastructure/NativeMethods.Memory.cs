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
using System.Diagnostics;
using System.Runtime.InteropServices;

public static partial class NativeMethods
{
    [Flags]
    public enum AllocationType
    {
        Commit = 0x1000,
        Reserve = 0x2000,
        Decommit = 0x4000,
        Release = 0x8000,
        Reset = 0x80000,
        Physical = 0x400000,
        TopDown = 0x100000,
        WriteWatch = 0x200000,
        LargePages = 0x20000000
    }

    [Flags]
    public enum MemoryProtection
    {
        Execute = 0x10,
        ExecuteRead = 0x20,
        ExecuteReadWrite = 0x40,
        ExecuteWriteCopy = 0x80,
        NoAccess = 0x01,
        ReadOnly = 0x02,
        ReadWrite = 0x04,
        WriteCopy = 0x08,
        GuardModifierflag = 0x100,
        NoCacheModifierflag = 0x200,
        WriteCombineModifierflag = 0x400
    }

    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    public static extern IntPtr VirtualAllocEx(ProcessHandle hProcess, IntPtr lpAddress, uint dwSize, AllocationType flAllocationType, MemoryProtection flProtect);

    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    public static extern bool VirtualFreeEx(ProcessHandle hProcess, IntPtr lpAddress, int dwSize, AllocationType dwFreeType);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool WriteProcessMemory(ProcessHandle hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, uint nSize, out int lpNumberOfBytesWritten);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
    public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr CreateRemoteThread(ProcessHandle handle,
        IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress,
        IntPtr lpParameter, uint dwCreationFlags, out IntPtr lpThreadId);

    [DllImport("kernel32.dll")]
    public static extern bool GetExitCodeThread(IntPtr hThread, out IntPtr exitCode);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern WaitResult WaitForSingleObject(IntPtr handle, uint timeoutInMilliseconds = 0xFFFFFFFF);

    public enum WaitResult
    {
        WAIT_ABANDONED = 0x80,
        WAIT_OBJECT_0 = 0x00,
        WAIT_TIMEOUT = 0x102,
        WAIT_FAILED = -1
    }

        public static IntPtr GetRemoteProcAddress(Process targetProcess, string moduleName, string procName)
    {
        long functionOffsetFromBaseAddress = 0;

        foreach (ProcessModule? mod in Process.GetCurrentProcess().Modules)
        {
            if (mod?.ModuleName is null
                || mod.FileName is null)
            {
                continue;
            }

            if (mod.ModuleName.Equals(moduleName, StringComparison.OrdinalIgnoreCase)
                || mod.FileName.Equals(moduleName, StringComparison.OrdinalIgnoreCase))
            {
                LogHelper.WriteLine($"Checking module \"{moduleName}\" with base address \"{mod.BaseAddress}\" for procaddress of \"{procName}\"...");

                var procAddress = GetProcAddress(mod!.BaseAddress, procName).ToInt64();

                if (procAddress != 0)
                {
                    LogHelper.WriteLine($"Got proc address in foreign process with \"{procAddress}\".");
                    functionOffsetFromBaseAddress = procAddress - (long)mod.BaseAddress;
                }

                break;
            }
        }

        if (functionOffsetFromBaseAddress == 0)
        {
            throw new Exception($"Could not find local method handle for \"{procName}\" in module \"{moduleName}\".");
        }

        var remoteModuleHandle = GetRemoteModuleHandle(targetProcess, moduleName);
        return new IntPtr((long)remoteModuleHandle + functionOffsetFromBaseAddress);
    }

    public static IntPtr GetRemoteModuleHandle(Process targetProcess, string moduleName)
    {
        foreach (ProcessModule? mod in targetProcess.Modules)
        {
            if (mod?.ModuleName is null
                || mod?.FileName is null)
            {
                continue;
            }

            if (mod!.ModuleName.Equals(moduleName, StringComparison.OrdinalIgnoreCase)
                || mod!.FileName.Equals(moduleName, StringComparison.OrdinalIgnoreCase))
            {
                LogHelper.WriteLine($"Found module \"{moduleName}\" with base address \"{mod!.BaseAddress}\".");
                return mod!.BaseAddress;
            }
        }

        return IntPtr.Zero;
    }

    [DllImport("kernel32")]
    public static extern IntPtr LoadLibrary(string librayName);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool FreeLibrary(IntPtr hModule);
}