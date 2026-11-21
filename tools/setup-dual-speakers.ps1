# Routes Windows audio to Xiaomi Soundbar 2.0 (A1) and Xiaomi Sound Party (A2) via Voicemeeter Banana.
# Usage: powershell -ExecutionPolicy Bypass -File setup-dual-speakers.ps1 [-SoundbarDelayMs 0] [-PartyDelayMs 0]
param(
    [int]$SoundbarDelayMs = 0,
    [int]$PartyDelayMs = 100,
    [string]$SoundbarName = 'Speakers (Xiaomi Soundbar 2.0)',
    [string]$PartyName = 'Headphones (Xiaomi Sound Party)'
)
$ErrorActionPreference = 'Stop'
$vmDir = 'C:\Program Files (x86)\VB\Voicemeeter'

Add-Type -TypeDefinition @"
using System;
using System.Text;
using System.Runtime.InteropServices;
public static class VMR {
    [DllImport(@"$vmDir\VoicemeeterRemote64.dll")] public static extern int VBVMR_Login();
    [DllImport(@"$vmDir\VoicemeeterRemote64.dll")] public static extern int VBVMR_Logout();
    [DllImport(@"$vmDir\VoicemeeterRemote64.dll")] public static extern int VBVMR_RunVoicemeeter(int type);
    [DllImport(@"$vmDir\VoicemeeterRemote64.dll")] public static extern int VBVMR_IsParametersDirty();
    [DllImport(@"$vmDir\VoicemeeterRemote64.dll", CharSet = CharSet.Ansi)] public static extern int VBVMR_SetParameters(string script);
    [DllImport(@"$vmDir\VoicemeeterRemote64.dll", CharSet = CharSet.Ansi)] public static extern int VBVMR_GetParameterStringA(string name, StringBuilder value);
    [DllImport(@"$vmDir\VoicemeeterRemote64.dll", CharSet = CharSet.Ansi)] public static extern int VBVMR_GetParameterFloat(string name, ref float value);
}

[ComImport, Guid("f8679f50-850a-41cf-9c72-430f290290c8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IPolicyConfig {
    int GetMixFormat(); int GetDeviceFormat(); int ResetDeviceFormat(); int SetDeviceFormat();
    int GetProcessingPeriod(); int SetProcessingPeriod(); int GetShareMode(); int SetShareMode();
    int GetPropertyValue(); int SetPropertyValue();
    int SetDefaultEndpoint([MarshalAs(UnmanagedType.LPWStr)] string id, int role);
    int SetEndpointVisibility();
}
[ComImport, Guid("870af99c-171d-4f9e-af0d-e63df40c2bc9")] class PolicyConfigClient {}
public static class DefaultAudio {
    public static void Set(string id) {
        var pc = (IPolicyConfig)new PolicyConfigClient();
        for (int role = 0; role < 3; role++) Marshal.ThrowExceptionForHR(pc.SetDefaultEndpoint(id, role));
    }
}
"@

# 1. Start Voicemeeter Banana if not running
if (-not (Get-Process voicemeeterpro*, voicemeeterpro_x64 -ErrorAction SilentlyContinue)) {
    Start-Process "$vmDir\voicemeeterpro_x64.exe"
    Start-Sleep -Seconds 4
}
$r = [VMR]::VBVMR_Login()
if ($r -lt 0) { throw "Voicemeeter login failed ($r)" }
# wait until Voicemeeter answers parameter reads (it can take several seconds after launch)
$f = 0.0
for ($i = 0; $i -lt 40; $i++) {
    [void][VMR]::VBVMR_IsParametersDirty()
    if ([VMR]::VBVMR_GetParameterFloat('Strip[3].A1', [ref]$f) -eq 0) { break }
    Start-Sleep -Milliseconds 500
}

# 2. Outputs, routing, delays
#    Bus[0]=A1 soundbar, Bus[1]=A2 Sound Party; Strip[3]=Voicemeeter Input (VAIO, where Windows audio arrives)
$script = @(
    "Bus[0].device.wdm=`"$SoundbarName`"",
    "Bus[1].device.wdm=`"$PartyName`"",
    'Bus[2].device.wdm=""',
    'Strip[3].A1=1', 'Strip[3].A2=1', 'Strip[3].A3=0', 'Strip[3].B1=0', 'Strip[3].B2=0',
    'Strip[3].Gain=0', 'Bus[0].Mute=0', 'Bus[1].Mute=0', 'Bus[0].Gain=0', 'Bus[1].Gain=0',
    "Option.delay[0]=$SoundbarDelayMs",
    "Option.delay[1]=$PartyDelayMs"
) -join ';'
$r = [VMR]::VBVMR_SetParameters($script)
if ($r -ne 0) { Write-Warning "SetParameters returned $r" }
Start-Sleep -Seconds 2
[void][VMR]::VBVMR_IsParametersDirty()

foreach ($i in 0, 1) {
    $sb = New-Object System.Text.StringBuilder 512
    [void][VMR]::VBVMR_GetParameterStringA("Bus[$i].device.name", $sb)
    Write-Host ("A{0} -> {1}" -f ($i + 1), $sb.ToString())
}
[void][VMR]::VBVMR_Logout()

# 3. Make "Voicemeeter Input" the Windows default playback device
$base = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\MMDevices\Audio\Render'
$nameKey = '{a45c254e-df1c-4efd-8020-67d146a850e0},2'
$target = Get-ChildItem $base | Where-Object {
    $p = Get-ItemProperty "$($_.PSPath)\Properties" -ErrorAction SilentlyContinue
    $p.$nameKey -eq 'Voicemeeter Input' -and (Get-ItemProperty $_.PSPath).DeviceState -eq 1
} | Select-Object -First 1
if (-not $target) { throw 'Could not find active "Voicemeeter Input" playback device' }
[DefaultAudio]::Set('{0.0.0.00000000}.' + $target.PSChildName)
Write-Host 'Windows default playback -> Voicemeeter Input'
Write-Host "Delays: soundbar=$SoundbarDelayMs ms, Sound Party=$PartyDelayMs ms"
