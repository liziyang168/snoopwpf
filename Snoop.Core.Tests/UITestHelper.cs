namespace Snoop.Core.Tests;

using System.Windows.Threading;

public static class UITestHelper
{
    public static void DoEvents()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
            new DispatcherOperationCallback(
                delegate
                {
                    frame.Continue = false;
                    return null;
                }), null);
        Dispatcher.PushFrame(frame);
    }
}
