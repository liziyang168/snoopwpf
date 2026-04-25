namespace Snoop.Core.Tests;

using System;
using System.Windows;

public static class TestWindowHelper
{
    public static TestWindow CreateTestWindow(object content)
    {
        return new TestWindow
        {
            Content = content,
            WindowState = WindowState.Minimized,
            ShowInTaskbar = false,
            ShowActivated = false
        };
    }

    public sealed class TestWindow : Window, IDisposable
    {
        public void Dispose()
        {
            this.Close();
        }
    }
}
