# Paused Stream Killer

![Paused Stream Killer illustration](assets/paused-stream-killer-cover.jpg)

Paused Stream Killer is a Jellyfin plugin that stops playback after it has remained paused for a configurable amount of time.

## Features

- Checks playback sessions once per minute and stops sessions that exceed the configured pause duration.
- Defaults to a 30-minute pause limit and video playback only.
- Can optionally include audio playback and SyncPlay sessions. Both options are disabled by default.
- When SyncPlay is enabled, only the paused client session receives the stop command.
- Starts tracking a paused session when the plugin starts or first observes it. A plugin or server restart therefore starts a new timeout for sessions already paused.
- Logs stopped sessions and errors without logging authentication details.

## Installation

After a release is published, add this manifest in **Dashboard → Plugins → Repositories**:

```text
https://raw.githubusercontent.com/ShamanTramp/Paused-Stream-Killer/main/manifest.json
```

Save the repository, open the plugin catalog, install **Paused Stream Killer**, then restart Jellyfin. The [0.1.1.0 plugin package](https://raw.githubusercontent.com/ShamanTramp/Paused-Stream-Killer/main/packages/paused-stream-killer-0.1.1.0.zip) is also available directly.

## Configuration

Open **Dashboard → Plugins → Paused Stream Killer**:

| Option | Default | Description |
| --- | --- | --- |
| Automatic stopping | Enabled | Enables or disables monitoring and stop commands. |
| Pause timeout | 30 minutes | Minimum time a session must remain paused before the plugin sends a stop command. The plugin checks once per minute. |
| Include audio playback | Disabled | Applies the timeout to audio-only playback sessions. |
| Include SyncPlay sessions | Disabled | Applies the timeout to SyncPlay sessions. When enabled, only the paused client receives a stop command. |

The pause timer starts when the plugin first observes the session in a paused state. If the server or plugin restarts while playback is paused, the timeout starts again when the plugin resumes monitoring.

## Compatibility

Targets Jellyfin 10.11.11 and .NET 9.

## License

GPL-3.0-only. See [LICENSE](LICENSE).

