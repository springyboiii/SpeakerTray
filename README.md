# SpeakerTray

A tiny Windows system-tray widget for playing PC audio through **two speakers at once** (e.g. a soundbar + a Bluetooth speaker) using [Voicemeeter Banana](https://vb-audio.com/Voicemeeter/banana.htm), with quick output switching, per-device volume and a sync-delay control.

Built for a Xiaomi Soundbar 2.0 + Xiaomi Sound Party pair, but the device names are easy to change.

## Features

- Lives in the taskbar's **Show hidden icons** area (speaker icon)
- **Left-click** → panel with:
  - Output: *Both speakers* (via Voicemeeter) / *Soundbar only* / *Sound Party only* / *PC headphones* / *AirPods Pro*
  - Volume sliders: current output, soundbar, Sound Party
  - Sound Party delay (ms) to line up Bluetooth latency (remembered and re-applied after restarts)
  - Voicemeeter status + **Start / restart** button (fixes "no sound" after a crash)
  - **Back to default** button: closes Voicemeeter and switches Windows to the PC headphones, i.e. a plain setup (pick *AirPods Pro* or any other output after that). Pick *Both speakers* to turn the setup on again.
- **Right-click** → quick output switch, back to default, start/restart Voicemeeter, Sound settings, Exit
- Warns with a clickable notification if Voicemeeter stops while it is the output, and starts it at login when needed
- Single small C# exe (WinForms, Core Audio COM, Voicemeeter Remote API), compiled on install with the .NET Framework built into Windows; no extra runtimes

## Requirements

- Windows 10/11, Windows PowerShell 5.1
- [Voicemeeter Banana](https://vb-audio.com/Voicemeeter/banana.htm) installed in the default location (`C:\Program Files (x86)\VB\Voicemeeter`)

## Setup

1. Pair/connect both speakers to the PC.
2. Run the one-off routing script (sets Voicemeeter A1 = soundbar, A2 = second speaker, routes Windows audio to both, makes *Voicemeeter Input* the default device):
   ```powershell
   powershell -ExecutionPolicy Bypass -File tools\setup-dual-speakers.ps1 -PartyDelayMs 100
   ```
3. Install the tray widget (builds `%LOCALAPPDATA%\SpeakerTray\SpeakerTray.exe`, registers a logon scheduled task with a 15 s delay, launches it). Run it from a normal PowerShell window:
   ```powershell
   powershell -ExecutionPolicy Bypass -File install.ps1
   ```
   Remove it with `install.ps1 -Uninstall`.

### Finding the right delay

`tools\sync-test.ps1` plays 7 numbered click rounds with different delays (soundbar delayed 150/100/50 ms, none, then the second speaker delayed 50/100/150 ms). Pick the round that sounds like one clean click, then set that delay in the widget.

## Using other devices

Edit the constants at the top of the `TrayApp` class in `SpeakerTray.cs` — each is a substring of the Windows playback device name:

```csharp
public const string VMIN = "Voicemeeter Input (", BAR = "Xiaomi Soundbar",
    PARTY = "Headphones (Xiaomi Sound Party)", PC = "Speakers (Realtek";
```

Tip: for Bluetooth speakers, use the `Headphones (...)` / `Speakers (...)` endpoint, not the `Headset (... Hands-Free)` one.

## Files

| File | Purpose |
|---|---|
| `SpeakerTray.cs` | The tray app (C# source) |
| `install.ps1` | Build exe, install / uninstall, logon scheduled task |
| `tools/setup-dual-speakers.ps1` | One-off Voicemeeter routing + default device |
| `tools/speaker-volume.ps1` | Print (or set) playback device volumes |
| `tools/sync-test.ps1`, `tools/sync-one.ps1` | Delay sync listening tests |

## License

MIT
