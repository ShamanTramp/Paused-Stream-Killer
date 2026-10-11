# Paused Stream Killer

![Paused Stream Killer illustration](assets/paused-stream-killer-cover.jpg)

Paused Stream Killer is a Jellyfin plugin that automatically stops playback sessions that have remained paused beyond a configurable timeout. It helps release server resources while leaving ordinary active playback untouched.

## Features

- Checks active Jellyfin playback sessions once per minute.
- Uses a 30-minute pause timeout by default; the timeout can be changed in the plugin settings.
- Monitors video playback by default, with optional support for audio-only sessions.
- Can optionally include SyncPlay sessions. When enabled, only the paused client receives a stop command.
- Resets its timer when playback resumes, the session disappears, or the server/plugin restarts.
- Logs stop requests and errors.

## Installation

In Jellyfin, open **Dashboard → Plugins → Repositories** and add this manifest:

```text
https://raw.githubusercontent.com/ShamanTramp/Paused-Stream-Killer/main/manifest.json
```

Save the repository, open the plugin catalog, and install **Paused Stream Killer**. Restart Jellyfin when the installation finishes. You can also download the package from [GitHub Releases](https://github.com/ShamanTramp/Paused-Stream-Killer/releases).

## Configuration

Open **Dashboard → Plugins → Paused Stream Killer** to configure the plugin:

| Setting | Default | Description |
| --- | --- | --- |
| Automatic stopping | Enabled | Turns pause monitoring and stop requests on or off. |
| Pause timeout | 30 minutes | Minimum pause duration before a stop request. The plugin checks sessions once per minute. |
| Include audio playback | Disabled | Applies the timeout to audio-only playback. |
| Include SyncPlay sessions | Disabled | Applies the timeout to SyncPlay sessions. Only the paused client is stopped. |

The timer starts when the plugin first detects a session as paused. If Jellyfin or the plugin restarts while playback is paused, the timer starts again after monitoring resumes.

## Build and test locally

```sh
dotnet restore Jellyfin.Plugin.PausedStreamKiller.sln
dotnet build Jellyfin.Plugin.PausedStreamKiller.sln -c Release --no-restore -m:1
dotnet test Jellyfin.Plugin.PausedStreamKiller.sln -c Release --no-build --no-restore -m:1
```

The plugin targets Jellyfin 10.11.11 and .NET 9. Its GitHub Actions workflows are manually triggered.

## License

GPL-3.0-only. See [LICENSE](LICENSE).# Paused-Stream-Killer
