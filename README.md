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

## Installation

Download `SyncTraySetup.exe` from the build artifacts and run it. The installer will:

- Ask whether Syncthing is already installed. If it is, you can browse for your
  `syncthing.exe` (defaulting to the standard location when present). If not, it
  installs Syncthing for you via `winget`.
- Install SyncTray to `C:\Program Files\SyncTray` by default (you can change this).
- Offer a checkbox to start SyncTray automatically when Windows starts.

The chosen Syncthing path is stored in the registry under
`HKLM\SOFTWARE\SyncTray\SyncthingPath` and read by the app at runtime.

### Building the installer locally

The installer is an [Inno Setup](https://jrsoftware.org/isinfo.php) script
(`installer/SyncTray.iss`). To build it:

```powershell
dotnet publish src/SyncTray/SyncTray.csproj -c Release -r win-x64 --self-contained true -o publish
iscc /DAppVersion=0.0.1 /DPublishDir=..\publish installer\SyncTray.iss
```

The resulting `SyncTraySetup.exe` is written to `installer/Output/`. CI also
produces both the published binary and the installer as build artifacts.
