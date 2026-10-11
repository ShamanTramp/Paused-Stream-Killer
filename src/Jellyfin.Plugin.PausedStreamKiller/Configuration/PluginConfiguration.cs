using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.PausedStreamKiller.Configuration;

/// <summary>Settings for automatically stopping long-paused playback sessions.</summary>
public sealed class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>Gets or sets whether automatic stopping is enabled.</summary>
    public bool EnableAutomaticStop { get; set; } = true;

    /// <summary>Gets or sets how many minutes a playback session may remain paused.</summary>
    public int PauseTimeoutMinutes { get; set; } = 30;

    /// <summary>Gets or sets whether audio playback sessions are included.</summary>
    public bool IncludeAudioPlayback { get; set; }

    /// <summary>Gets or sets whether SyncPlay sessions are included.</summary>
    public bool IncludeSyncPlaySessions { get; set; }
}

