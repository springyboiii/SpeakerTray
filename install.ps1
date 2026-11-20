# Installs SpeakerTray to %LOCALAPPDATA%\SpeakerTray, adds a Startup shortcut and launches it.
# Usage: powershell -ExecutionPolicy Bypass -File install.ps1 [-Uninstall]
param([switch]$Uninstall)
$dst = Join-Path $env:LOCALAPPDATA 'SpeakerTray'
$lnk = Join-Path ([Environment]::GetFolderPath('Startup')) 'SpeakerTray.lnk'

# stop a running copy
Get-CimInstance Win32_Process -Filter "Name='powershell.exe'" |
    Where-Object { $_.CommandLine -match 'SpeakerTray\.ps1' -and $_.ProcessId -ne $PID } |
    ForEach-Object { Stop-Process -Id $_.ProcessId -Force }

if ($Uninstall) {
    Remove-Item $lnk -ErrorAction SilentlyContinue
    Remove-Item $dst -Recurse -Force -ErrorAction SilentlyContinue
    'SpeakerTray removed.'
    return
}

New-Item -ItemType Directory -Force $dst | Out-Null
Copy-Item (Join-Path $PSScriptRoot 'SpeakerTray.ps1'), (Join-Path $PSScriptRoot 'SpeakerTray.vbs') $dst -Force

$s = (New-Object -ComObject WScript.Shell).CreateShortcut($lnk)
$s.TargetPath = 'wscript.exe'
$s.Arguments = '"' + (Join-Path $dst 'SpeakerTray.vbs') + '"'
$s.IconLocation = "$env:WINDIR\System32\SndVol.exe,0"
$s.Save()

Start-Process wscript.exe -ArgumentList ('"' + (Join-Path $dst 'SpeakerTray.vbs') + '"')
"SpeakerTray installed to $dst and added to Startup."
