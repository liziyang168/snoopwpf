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

using System.Diagnostics;
using System.Runtime.InteropServices;

public static class ConsoleHelper
{
    /// <summary>
    /// Allocate a console if application started from within windows GUI.
    /// Detects the presence of an existing console associated with the application and
    /// attaches itself to it if available.
    /// </summary>
    public static void AttachConsoleToParentProcessOrAllocateNewOne()
    {
        if (NativeMethods.AttachConsole(NativeMethods.ATTACH_PARENT_PROCESS) == false
            && Marshal.GetLastWin32Error() == NativeMethods.ERROR_ACCESS_DENIED)
        {
            // A console was not allocated, so we need to make one.
            if (NativeMethods.FreeConsole() == false)
            {
                Trace.WriteLine("Console could not be freed.");
            }
            else
            {
                Trace.WriteLine("Console freed.");
            }

            if (NativeMethods.AttachConsole(NativeMethods.ATTACH_PARENT_PROCESS) == false)
            {
                Trace.WriteLine($"Could not attach to parent process console. Error = {Marshal.GetLastWin32Error()}");
            }
            else
            {
                Trace.WriteLine("Console attached to parent process.");
            }
        }
        else
        {
            Trace.WriteLine("Console attached to parent process or process is a standalone console application.");
        }
    }
}

public partial class NativeMethods
{
    /// <summary>
    /// allocates a new console for the calling process.
    /// </summary>
    /// <returns>If the function succeeds, the return value is nonzero.
    /// If the function fails, the return value is zero.
    /// To get extended error information, call Marshal.GetLastWin32Error.</returns>
    [DllImport("kernel32", SetLastError = true)]
    public static extern bool AllocConsole();

    /// <summary>
    /// Detaches the calling process from its console
    /// </summary>
    /// <returns>If the function succeeds, the return value is nonzero.
    /// If the function fails, the return value is zero.
    /// To get extended error information, call Marshal.GetLastWin32Error.</returns>
    [DllImport("kernel32", SetLastError = true)]
    public static extern bool FreeConsole();

    /// <summary>
    /// Attaches the calling process to the console of the specified process.
    /// </summary>
    /// <param name="dwProcessId">[in] Identifier of the process, usually will be ATTACH_PARENT_PROCESS</param>
    /// <returns>If the function succeeds, the return value is nonzero.
    /// If the function fails, the return value is zero.
    /// To get extended error information, call Marshal.GetLastWin32Error.</returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool AttachConsole(uint dwProcessId);

    /// <summary>Identifies the console of the parent of the current process as the console to be attached.
    /// always pass this with AttachConsole in .NET for stability reasons and mainly because
    /// I have NOT tested interprocess attaching in .NET so don't blame me if it doesn't work! </summary>
    public const uint ATTACH_PARENT_PROCESS = 0x0ffffffff;

    public const int ERROR_ACCESS_DENIED = 5;
}