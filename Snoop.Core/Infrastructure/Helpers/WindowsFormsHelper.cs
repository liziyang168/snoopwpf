namespace Snoop.Infrastructure.Helpers;

using System;
using System.Reflection;
using System.Windows;

public static class WindowsFormsHelper
{
    private static readonly MethodInfo? enableModelessKeyboardInterop = Type.GetType("System.Windows.Forms.Integration.ElementHost")?.GetMethod("EnableModelessKeyboardInterop", BindingFlags.Public | BindingFlags.Static);

    public static void EnableModelessKeyboardInterop(Window window) => enableModelessKeyboardInterop?.Invoke(null, new object[] { window });
}