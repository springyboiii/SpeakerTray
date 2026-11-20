# SpeakerTray

A tiny Windows system-tray widget for playing PC audio through **two speakers at once** (e.g. a soundbar + a Bluetooth speaker) using [Voicemeeter Banana](https://vb-audio.com/Voicemeeter/banana.htm), with quick output switching, per-device volume and a sync-delay control.

Built for a Xiaomi Soundbar 2.0 + Xiaomi Sound Party pair, but the device names are easy to change.

## Features

- Lives in the taskbar's **Show hidden icons** area (speaker icon)
- **Left-click** → panel with:
  - Output: *Both speakers* (via Voicemeeter) / *Soundbar only* / *Sound Party only* / *PC headphones*
  - Volume sliders: current output, soundbar, Sound Party
  - Sound Party delay (ms) to line up Bluetooth latency
- **Right-click** → quick output switch, open Voicemeeter, Sound settings, Exit
- No install of extra runtimes: pure PowerShell + inline C# (WinForms, Core Audio COM, Voicemeeter Remote API)

## Requirements

- Windows 10/11, Windows PowerShell 5.1
- [Voicemeeter Banana](https://vb-audio.com/Voicemeeter/banana.htm) installed in the default location (`C:\Program Files (x86)\VB\Voicemeeter`)

## Setup

1. Pair/connect both speakers to the PC.
2. Run the one-off routing script (sets Voicemeeter A1 = soundbar, A2 = second speaker, routes Windows audio to both, makes *Voicemeeter Input* the default device):
   ```powershell
   powershell -ExecutionPolicy Bypass -File tools\setup-dual-speakers.ps1 -PartyDelayMs 100
   ```
3. Install the tray widget (copies to `%LOCALAPPDATA%\SpeakerTray`, adds a Startup shortcut, launches it):
   ```powershell
   powershell -ExecutionPolicy Bypass -File install.ps1
   ```
   Remove it with `install.ps1 -Uninstall`.

### Finding the right delay

`tools\sync-test.ps1` plays 7 numbered click rounds with different delays (soundbar delayed 150/100/50 ms, none, then the second speaker delayed 50/100/150 ms). Pick the round that sounds like one clean click, then set that delay in the widget.

## Using other devices

Edit the constants at the top of the `TrayApp` class in `SpeakerTray.ps1` — each is a substring of the Windows playback device name:

```csharp
public const string VMIN = "Voicemeeter Input (", BAR = "Xiaomi Soundbar",
    PARTY = "Headphones (Xiaomi Sound Party)", PC = "Speakers (Realtek";
```

Tip: for Bluetooth speakers, use the `Headphones (...)` / `Speakers (...)` endpoint, not the `Headset (... Hands-Free)` one.

## Files

| File | Purpose |
|---|---|
| `SpeakerTray.ps1` | The tray app |
| `SpeakerTray.vbs` | Launches it without a console window |
| `install.ps1` | Install / uninstall + Startup shortcut |
| `tools/setup-dual-speakers.ps1` | One-off Voicemeeter routing + default device |
| `tools/speaker-volume.ps1` | Print (or set) playback device volumes |
| `tools/sync-test.ps1`, `tools/sync-one.ps1` | Delay sync listening tests |

## License

MIT
