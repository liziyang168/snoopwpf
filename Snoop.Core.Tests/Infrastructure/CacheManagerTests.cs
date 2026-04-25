namespace Snoop.Core.Tests.Infrastructure;

using NUnit.Framework;
using Snoop.Infrastructure;

[TestFixture]
public class CacheManagerTests
{
    private class CacheManagedStub : ICacheManaged
    {
        public bool IsActivated { get; private set; }

        public bool IsDisposed { get; private set; }

        public void Activate()
        {
            this.IsActivated = true;
        }

        public void Dispose()
        {
            this.IsDisposed = true;
        }
    }

    [Test]
    public void IncreaseUsageCount_ShouldActivateParticipants_WhenFirstUsage()
    {
        // Arrange
        var cacheManager = CacheManager.Instance;
        var participant = new CacheManagedStub();
        cacheManager.Participants.Add(participant);

        try
        {
            // Act
            cacheManager.IncreaseUsageCount();

            // Assert
            Assert.That(cacheManager.UsageCount, Is.GreaterThan(0));
            Assert.That(participant.IsActivated, Is.True);
        }
        finally
        {
            // Cleanup: Reset UsageCount and remove participant
            while (cacheManager.UsageCount > 0)
            {
                cacheManager.DecreaseUsageCount();
            }

            cacheManager.Participants.Remove(participant);
        }
    }

    [Test]
    public void DecreaseUsageCount_ShouldDisposeParticipants_WhenLastUsage()
    {
        // Arrange
        var cacheManager = CacheManager.Instance;
        var participant = new CacheManagedStub();
        cacheManager.Participants.Add(participant);

        try
        {
            cacheManager.IncreaseUsageCount();
            Assert.That(participant.IsActivated, Is.True);

            // Act
            cacheManager.DecreaseUsageCount();

            // Assert
            Assert.That(cacheManager.UsageCount, Is.EqualTo(0));
            Assert.That(participant.IsDisposed, Is.True);
        }
        finally
        {
            // Cleanup: ensure it's clean for other tests
            while (cacheManager.UsageCount > 0)
            {
                cacheManager.DecreaseUsageCount();
            }

            cacheManager.Participants.Remove(participant);
        }
    }

    [Test]
    public void UsageCount_ShouldWorkCorrectly_WithMultipleUsers()
    {
        // Arrange
        var cacheManager = CacheManager.Instance;
        var initialCount = cacheManager.UsageCount;
        var participant = new CacheManagedStub();
        cacheManager.Participants.Add(participant);

        try
        {
            // Act & Assert
            cacheManager.IncreaseUsageCount();
            Assert.That(cacheManager.UsageCount, Is.EqualTo(initialCount + 1));
            Assert.That(participant.IsActivated, Is.True);

            var wasDisposedAfterFirstIncrease = participant.IsDisposed;

            cacheManager.IncreaseUsageCount();
            Assert.That(cacheManager.UsageCount, Is.EqualTo(initialCount + 2));

            cacheManager.DecreaseUsageCount();
            Assert.That(cacheManager.UsageCount, Is.EqualTo(initialCount + 1));
            Assert.That(participant.IsDisposed, Is.EqualTo(wasDisposedAfterFirstIncrease)); // Should not be disposed yet

            cacheManager.DecreaseUsageCount();
            Assert.That(cacheManager.UsageCount, Is.EqualTo(initialCount));
            Assert.That(participant.IsDisposed, Is.True);
        }
        finally
        {
            // Cleanup
            while (cacheManager.UsageCount > initialCount)
            {
                cacheManager.DecreaseUsageCount();
            }

            cacheManager.Participants.Remove(participant);
        }
    }
}
