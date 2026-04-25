namespace Snoop.Core.Tests.Infrastructure;

using System.Windows.Controls;
using NUnit.Framework;
using Snoop.Infrastructure;

[TestFixture]
public class WhenLoadedTests
{
    [Test]
    public void WhenLoaded_ShouldExecuteImmediately_IfAlreadyLoaded()
    {
        // Arrange
        var button = new Button();
        var wasExecuted = false;
        using var window = TestWindowHelper.CreateTestWindow(button);
        window.Show();
        UITestHelper.DoEvents();

        Assert.That(button.IsLoaded, Is.True);

        // Act
        button.WhenLoaded(_ => wasExecuted = true);

        // Assert
        Assert.That(wasExecuted, Is.True);
    }

    [Test]
    public void WhenLoaded_ShouldExecuteOnLoadedEvent_IfNotAlreadyLoaded()
    {
        // Arrange
        var button = new Button();
        var wasExecuted = false;

        // Act
        button.WhenLoaded(_ => wasExecuted = true);

        // Assert before loaded
        Assert.That(button.IsLoaded, Is.False);
        Assert.That(wasExecuted, Is.False);

        using var window = TestWindowHelper.CreateTestWindow(button);
        window.Show();
        UITestHelper.DoEvents();

        // Assert after loaded
        Assert.That(button.IsLoaded, Is.True);
        Assert.That(wasExecuted, Is.True);
    }

    [Test]
    public void WhenLoaded_ShouldExecuteOnlyOnce()
    {
        // Arrange
        var button = new Button();
        var executionCount = 0;

        button.WhenLoaded(_ => executionCount++);

        using var window = TestWindowHelper.CreateTestWindow(button);
        window.Show();
        UITestHelper.DoEvents();

        Assert.That(executionCount, Is.EqualTo(1));

        // Remove and add back to tree to trigger Loaded again if possible
        window.Content = null;
        UITestHelper.DoEvents();
        window.Content = button;
        UITestHelper.DoEvents();

        // Assert it was only executed once
        Assert.That(executionCount, Is.EqualTo(1));
    }
}
