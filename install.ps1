# Builds SpeakerTray.exe from SpeakerTray.cs into %LOCALAPPDATA%\SpeakerTray,
# registers a logon scheduled task (15 s delay) and launches it.
# Usage: powershell -ExecutionPolicy Bypass -File install.ps1 [-Uninstall]
param([switch]$Uninstall)
$ErrorActionPreference = 'Stop'
$dst  = Join-Path $env:LOCALAPPDATA 'SpeakerTray'
$exe  = Join-Path $dst 'SpeakerTray.exe'
$task = 'SpeakerTray'

# stop running copies (exe, and the older PowerShell-hosted version)
Get-Process SpeakerTray -ErrorAction SilentlyContinue | Stop-Process -Force
Get-CimInstance Win32_Process -Filter "Name='powershell.exe'" |
    Where-Object { $_.CommandLine -match 'SpeakerTray\.ps1' -and $_.ProcessId -ne $PID } |
    ForEach-Object { Stop-Process -Id $_.ProcessId -Force }
Start-Sleep -Milliseconds 500

# clean up the older Startup-folder launcher
Remove-Item (Join-Path ([Environment]::GetFolderPath('Startup')) 'SpeakerTray.lnk') -ErrorAction SilentlyContinue
Remove-Item (Join-Path $dst 'SpeakerTray.vbs'), (Join-Path $dst 'SpeakerTray.ps1') -ErrorAction SilentlyContinue

if ($Uninstall) {
    Unregister-ScheduledTask -TaskName $task -Confirm:$false -ErrorAction SilentlyContinue
    Remove-Item $dst -Recurse -Force -ErrorAction SilentlyContinue
    'SpeakerTray removed.'
    return
}

New-Item -ItemType Directory -Force $dst | Out-Null
Add-Type -TypeDefinition (Get-Content (Join-Path $PSScriptRoot 'SpeakerTray.cs') -Raw) `
    -ReferencedAssemblies System.Windows.Forms, System.Drawing `
    -OutputAssembly $exe -OutputType WindowsApplication

$trigger = New-ScheduledTaskTrigger -AtLogOn -User "$env:USERDOMAIN\$env:USERNAME"
$trigger.Delay = 'PT15S'
$action  = New-ScheduledTaskAction -Execute $exe
$set     = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -ExecutionTimeLimit ([TimeSpan]::Zero)
$prin    = New-ScheduledTaskPrincipal -UserId "$env:USERDOMAIN\$env:USERNAME" -LogonType Interactive -RunLevel Limited
Register-ScheduledTask -TaskName $task -Action $action -Trigger $trigger -Settings $set -Principal $prin -Force | Out-Null

Start-Process $exe
"SpeakerTray built to $exe; starts 15 s after logon (scheduled task '$task')."
