namespace Jellyfin.Plugin.PausedStreamKiller.Core;

/// <summary>Runtime options that control which paused sessions can be stopped.</summary>
public sealed record PausedPlaybackOptions(
    bool IsEnabled,
    int TimeoutMinutes,
    bool IncludeAudio,
    bool IncludeSyncPlay);

