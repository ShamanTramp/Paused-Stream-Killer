namespace Jellyfin.Plugin.PausedStreamKiller.Core;

/// <summary>Tracks pause durations and selects sessions that have exceeded their timeout.</summary>
public sealed class PausedSessionMonitor
{
    private readonly Dictionary<string, TrackedPause> _tracked = new(StringComparer.Ordinal);

    /// <summary>Gets sessions that have remained paused longer than the selected timeout.</summary>
    /// <param name="sessions">Current session snapshots.</param>
    /// <param name="options">Current plugin options.</param>
    /// <param name="now">Current UTC time, supplied by the caller for deterministic testing.</param>
    /// <returns>Eligible sessions whose pause duration reached the configured timeout.</returns>
    public IReadOnlyList<ExpiredPlaybackSession> GetExpiredSessions(
        IEnumerable<PlaybackSessionSnapshot> sessions,
        PausedPlaybackOptions options,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        ArgumentNullException.ThrowIfNull(options);

        if (!options.IsEnabled)
        {
            _tracked.Clear();
            return [];
        }

        var timeout = TimeSpan.FromMinutes(Math.Clamp(options.TimeoutMinutes, 1, 1440));
        var activeIds = new HashSet<string>(StringComparer.Ordinal);
        var expired = new List<ExpiredPlaybackSession>();

        foreach (var session in sessions)
        {
            if (string.IsNullOrWhiteSpace(session.SessionId)
                || !session.IsPaused
                || !IsIncluded(session, options))
            {
                continue;
            }

            activeIds.Add(session.SessionId);
            var pause = new TrackedPause(session.ItemId, now);
            if (_tracked.TryGetValue(session.SessionId, out var previous)
                && StringComparer.Ordinal.Equals(previous.ItemId, session.ItemId))
            {
                pause = previous;
                if (now < pause.StartedAt)
                {
                    // A backward system clock adjustment restarts the timer safely.
                    pause = new TrackedPause(session.ItemId, now);
                }
            }

            _tracked[session.SessionId] = pause;
            if (now - pause.StartedAt >= timeout)
            {
                expired.Add(new ExpiredPlaybackSession(session.SessionId, session.ItemId, session.ItemName, pause.StartedAt));
            }
        }

        foreach (var staleId in _tracked.Keys.Where(id => !activeIds.Contains(id)).ToArray())
        {
            _tracked.Remove(staleId);
        }

        return expired;
    }

    /// <summary>Resets pause tracking after a stop command was successfully sent.</summary>
    /// <param name="sessionId">The session identifier to reset.</param>
    public void ResetSession(string sessionId)
    {
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            _tracked.Remove(sessionId);
        }
    }

    private static bool IsIncluded(PlaybackSessionSnapshot session, PausedPlaybackOptions options) =>
        session.MediaType switch
        {
            PlaybackMediaType.Video => !session.IsSyncPlay || options.IncludeSyncPlay,
            PlaybackMediaType.Audio => options.IncludeAudio && (!session.IsSyncPlay || options.IncludeSyncPlay),
            _ => false
        };

    private sealed record TrackedPause(string? ItemId, DateTimeOffset StartedAt);
}

