namespace Jellyfin.Plugin.PausedStreamKiller.Core;

/// <summary>Media categories handled by the pause monitor.</summary>
public enum PlaybackMediaType
{
    /// <summary>A video playback session.</summary>
    Video,

    /// <summary>An audio playback session.</summary>
    Audio,

    /// <summary>A media category that is not supported by the monitor.</summary>
    Other
}

