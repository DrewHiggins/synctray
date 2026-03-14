SyncTray
========

I love Syncthing, but wanted a better solution to make it run nicer on Windows without needing to pop up a shell window.

So I had Claude write this tray app to manage the Syncthing process and expose its basic info in a simple UI.

## What It Does

SyncTray sits in your Windows system tray and manages a local Syncthing daemon. Right-clicking the tray icon gives you:

- **Device status** — see which remote devices are online (●), offline (○), or errored (⚠)
- **Folder status** — check if shared folders are up-to-date (✓) or have errors (!)
- **Open web portal** — launch the Syncthing web GUI
- **Copy device ID** — one-click clipboard copy
- **Quit** — gracefully shuts down Syncthing and exits

It auto-discovers your Syncthing config (`config.xml`), launches the daemon if it's not already running, and monitors it for crashes. Status updates come via the `/rest/events` long-poll API with a polling fallback every 20 seconds.

## Tech Stack

- **C# / .NET** — WPF app with `System.Windows.Forms.NotifyIcon` for tray integration
- **LibSyncthing** — internal library wrapping the Syncthing REST API
- **No external dependencies** — uses only built-in `System.Text.Json`, `System.Xml.Linq`, and `System.Net.Http`
