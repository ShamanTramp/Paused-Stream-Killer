using Jellyfin.Data.Enums;
using Jellyfin.Plugin.PausedStreamKiller.Configuration;
using Jellyfin.Plugin.PausedStreamKiller.Core;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.SyncPlay;
using MediaBrowser.Controller.SyncPlay.Requests;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.PausedStreamKiller.Services;

/// <summary>Polls active playback sessions and stops sessions paused beyond the configured timeout.</summary>
public sealed class PausedStreamService(
    ISessionManager sessionManager,
    ISyncPlayManager syncPlayManager,
    PausedSessionMonitor monitor,
    ILogger<PausedStreamService> logger) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Paused Stream Killer is monitoring Jellyfin playback sessions every minute.");
        // Observe current sessions immediately so the timeout starts at plugin startup.
        await CheckSessionsAsync(stoppingToken).ConfigureAwait(false);

        while (!stoppingToken.IsCancellationRequested)
        {
            // Start each interval after the previous scan finishes so a scan does not run
            // fractionally before a pause timeout reaches its exact boundary.
            await Task.Delay(CheckInterval, stoppingToken).ConfigureAwait(false);
            await CheckSessionsAsync(stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task CheckSessionsAsync(CancellationToken cancellationToken)
    {
        var configuration = Plugin.Instance?.Configuration;
        var options = new PausedPlaybackOptions(
            configuration?.EnableAutomaticStop == true,
            configuration?.PauseTimeoutMinutes ?? 30,
            configuration?.IncludeAudioPlayback == true,
            configuration?.IncludeSyncPlaySessions == true);

        if (!options.IsEnabled)
        {
            monitor.GetExpiredSessions([], options, DateTimeOffset.UtcNow);
            return;
        }

        SessionInfo[] sessions;
        try
        {
            sessions = sessionManager.Sessions.ToArray();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not read Jellyfin playback sessions.");
            return;
        }

        var snapshots = new List<PlaybackSessionSnapshot>(sessions.Length);
        foreach (var session in sessions)
        {
            var item = session.NowPlayingItem;
            var mediaType = item?.MediaType switch
            {
                MediaType.Video => PlaybackMediaType.Video,
                MediaType.Audio => PlaybackMediaType.Audio,
                _ => PlaybackMediaType.Other
            };

            var isPaused = session.PlayState?.IsPaused == true && item is not null;
            var isSyncPlay = false;
            if (isPaused && !options.IncludeSyncPlay)
            {
                isSyncPlay = IsSyncPlaySession(session, cancellationToken);
            }

            snapshots.Add(new PlaybackSessionSnapshot(
                session.Id ?? string.Empty,
                item?.Id.ToString("N"),
                item?.Name,
                mediaType,
                isPaused,
                isSyncPlay));
        }

        var expiredSessions = monitor.GetExpiredSessions(snapshots, options, DateTimeOffset.UtcNow);
        if (expiredSessions.Count == 0)
        {
            return;
        }

        var sessionsById = sessions
            .Where(session => !string.IsNullOrWhiteSpace(session.Id))
            .GroupBy(session => session.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        foreach (var expired in expiredSessions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!sessionsById.TryGetValue(expired.SessionId, out var session)
                || session.PlayState?.IsPaused != true
                || !StringComparer.Ordinal.Equals(session.NowPlayingItem?.Id.ToString("N"), expired.ItemId))
            {
                logger.LogDebug("Skipped stopping session {SessionId} because its playback state changed during the check.", expired.SessionId);
                continue;
            }

            if (!options.IncludeSyncPlay
                && IsSyncPlaySession(session, cancellationToken))
            {
                logger.LogDebug("Skipped stopping session {SessionId} because it joined a SyncPlay group.", expired.SessionId);
                continue;
            }

            try
            {
                await sessionManager.SendPlaystateCommand(
                    session.Id,
                    session.Id,
                    new PlaystateRequest
                    {
                        Command = PlaystateCommand.Stop,
                        SeekPositionTicks = session.PlayState.PositionTicks
                    },
                    cancellationToken).ConfigureAwait(false);

                monitor.ResetSession(expired.SessionId);
                logger.LogInformation(
                    "Stopped playback '{ItemName}' for {UserName} on {DeviceName} after it remained paused for at least {TimeoutMinutes} minutes.",
                    expired.ItemName,
                    session.UserName,
                    session.DeviceName,
                    Math.Clamp(options.TimeoutMinutes, 1, 1440));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Could not stop paused playback for session {SessionId}.", expired.SessionId);
            }
        }
    }

    private bool IsSyncPlaySession(SessionInfo session, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var groups = syncPlayManager.ListGroups(session, new ListGroupsRequest());
            return groups.Any(group => group.Participants.Contains(session.Id, StringComparer.Ordinal));
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Could not determine whether session {SessionId} is in SyncPlay; leaving it untouched.", session.Id);
            return true;
        }
    }
}

