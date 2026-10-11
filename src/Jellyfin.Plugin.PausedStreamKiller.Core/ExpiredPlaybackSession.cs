namespace Jellyfin.Plugin.PausedStreamKiller.Core;

/// <summary>A paused playback session that exceeded the configured timeout.</summary>
public sealed record ExpiredPlaybackSession(
    string SessionId,
    string? ItemId,
    string? ItemName,
    DateTimeOffset PausedSince);

