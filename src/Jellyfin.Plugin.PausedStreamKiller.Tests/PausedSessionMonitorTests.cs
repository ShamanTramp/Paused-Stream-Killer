using Jellyfin.Plugin.PausedStreamKiller.Core;
using Xunit;

namespace Jellyfin.Plugin.PausedStreamKiller.Tests;

public sealed class PausedSessionMonitorTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 11, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void DoesNotStopSessionBeforeTimeout()
    {
        var monitor = new PausedSessionMonitor();
        var session = VideoSession();

        Assert.Empty(monitor.GetExpiredSessions([session], Options(timeout: 30), Start));
        Assert.Empty(monitor.GetExpiredSessions([session], Options(timeout: 30), Start.AddMinutes(29)));
    }

    [Fact]
    public void StopsSessionAtTimeoutBoundary()
    {
        var monitor = new PausedSessionMonitor();
        var session = VideoSession();
        monitor.GetExpiredSessions([session], Options(timeout: 30), Start);

        var expired = Assert.Single(monitor.GetExpiredSessions([session], Options(timeout: 30), Start.AddMinutes(30)));

        Assert.Equal(session.SessionId, expired.SessionId);
        Assert.Equal(session.ItemId, expired.ItemId);
        Assert.Equal(Start, expired.PausedSince);
    }

    [Fact]
    public void ResumeResetsPauseTimer()
    {
        var monitor = new PausedSessionMonitor();
        var paused = VideoSession();
        monitor.GetExpiredSessions([paused], Options(timeout: 15), Start);
        monitor.GetExpiredSessions([paused], Options(timeout: 15), Start.AddMinutes(10));

        Assert.Empty(monitor.GetExpiredSessions([paused with { IsPaused = false }], Options(timeout: 15), Start.AddMinutes(11)));
        Assert.Empty(monitor.GetExpiredSessions([paused], Options(timeout: 15), Start.AddMinutes(20)));
        Assert.Empty(monitor.GetExpiredSessions([paused], Options(timeout: 15), Start.AddMinutes(26)));
        Assert.Single(monitor.GetExpiredSessions([paused], Options(timeout: 15), Start.AddMinutes(35)));
    }

    [Fact]
    public void NewItemInSessionStartsFreshTimer()
    {
        var monitor = new PausedSessionMonitor();
        var original = VideoSession();
        monitor.GetExpiredSessions([original], Options(timeout: 15), Start);

        var differentItem = original with { ItemId = "different-item" };
        Assert.Empty(monitor.GetExpiredSessions([differentItem], Options(timeout: 15), Start.AddMinutes(20)));
        Assert.Single(monitor.GetExpiredSessions([differentItem], Options(timeout: 15), Start.AddMinutes(35)));
    }

    [Fact]
    public void MissingSessionStartsFreshTimerWhenItReturns()
    {
        var monitor = new PausedSessionMonitor();
        var session = VideoSession();
        monitor.GetExpiredSessions([session], Options(timeout: 15), Start);
        monitor.GetExpiredSessions([], Options(timeout: 15), Start.AddMinutes(10));

        Assert.Empty(monitor.GetExpiredSessions([session], Options(timeout: 15), Start.AddMinutes(20)));
        Assert.Single(monitor.GetExpiredSessions([session], Options(timeout: 15), Start.AddMinutes(35)));
    }

    [Fact]
    public void AudioIsExcludedByDefaultAndIncludedWhenConfigured()
    {
        var session = VideoSession() with { MediaType = PlaybackMediaType.Audio };
        var excludedMonitor = new PausedSessionMonitor();
        Assert.Empty(excludedMonitor.GetExpiredSessions([session], Options(timeout: 1), Start));
        Assert.Empty(excludedMonitor.GetExpiredSessions([session], Options(timeout: 1), Start.AddMinutes(2)));

        var includedMonitor = new PausedSessionMonitor();
        Assert.Empty(includedMonitor.GetExpiredSessions([session], Options(timeout: 1, includeAudio: true), Start));
        Assert.Single(includedMonitor.GetExpiredSessions([session], Options(timeout: 1, includeAudio: true), Start.AddMinutes(1)));
    }

    [Fact]
    public void SyncPlayIsExcludedByDefaultAndIncludedWhenConfigured()
    {
        var session = VideoSession() with { IsSyncPlay = true };
        var excludedMonitor = new PausedSessionMonitor();
        Assert.Empty(excludedMonitor.GetExpiredSessions([session], Options(timeout: 1), Start));
        Assert.Empty(excludedMonitor.GetExpiredSessions([session], Options(timeout: 1), Start.AddMinutes(2)));

        var includedMonitor = new PausedSessionMonitor();
        Assert.Empty(includedMonitor.GetExpiredSessions([session], Options(timeout: 1, includeSyncPlay: true), Start));
        Assert.Single(includedMonitor.GetExpiredSessions([session], Options(timeout: 1, includeSyncPlay: true), Start.AddMinutes(1)));
    }

    [Fact]
    public void DisabledMonitorClearsTrackedPause()
    {
        var monitor = new PausedSessionMonitor();
        var session = VideoSession();
        monitor.GetExpiredSessions([session], Options(timeout: 15), Start);
        monitor.GetExpiredSessions([session], Options(timeout: 15, enabled: false), Start.AddMinutes(10));

        Assert.Empty(monitor.GetExpiredSessions([session], Options(timeout: 15), Start.AddMinutes(20)));
        Assert.Single(monitor.GetExpiredSessions([session], Options(timeout: 15), Start.AddMinutes(35)));
    }

    [Fact]
    public void TimeoutIsClampedToSupportedRange()
    {
        var monitor = new PausedSessionMonitor();
        var session = VideoSession();
        monitor.GetExpiredSessions([session], Options(timeout: 0), Start);

        Assert.Single(monitor.GetExpiredSessions([session], Options(timeout: 0), Start.AddMinutes(1)));
    }

    [Fact]
    public void SuccessfulStopCanResetPauseTracking()
    {
        var monitor = new PausedSessionMonitor();
        var session = VideoSession();
        monitor.GetExpiredSessions([session], Options(timeout: 1), Start);
        monitor.ResetSession(session.SessionId);

        Assert.Empty(monitor.GetExpiredSessions([session], Options(timeout: 1), Start.AddMinutes(2)));
        Assert.Single(monitor.GetExpiredSessions([session], Options(timeout: 1), Start.AddMinutes(3)));
    }

    private static PlaybackSessionSnapshot VideoSession() =>
        new("session-1", "item-1", "Test episode", PlaybackMediaType.Video, true, false);

    private static PausedPlaybackOptions Options(
        int timeout,
        bool enabled = true,
        bool includeAudio = false,
        bool includeSyncPlay = false) =>
        new(enabled, timeout, includeAudio, includeSyncPlay);
}

