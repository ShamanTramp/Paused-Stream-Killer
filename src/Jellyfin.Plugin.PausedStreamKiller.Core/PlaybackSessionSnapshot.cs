namespace Jellyfin.Plugin.PausedStreamKiller.Core;

/// <summary>A minimal snapshot of a Jellyfin playback session.</summary>
public sealed record PlaybackSessionSnapshot(
    string SessionId,
    string? ItemId,
    string? ItemName,
    PlaybackMediaType MediaType,
    bool IsPaused,
    bool IsSyncPlay);

