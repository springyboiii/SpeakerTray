$vmDir='C:\Program Files (x86)\VB\Voicemeeter'
Add-Type -TypeDefinition @"
using System; using System.Runtime.InteropServices;
public static class V {
 [DllImport(@"$vmDir\VoicemeeterRemote64.dll")] public static extern int VBVMR_Login();
 [DllImport(@"$vmDir\VoicemeeterRemote64.dll")] public static extern int VBVMR_Logout();
 [DllImport(@"$vmDir\VoicemeeterRemote64.dll", CharSet=CharSet.Ansi)] public static extern int VBVMR_SetParameters(string s);
}
"@
Add-Type -AssemblyName System.Speech
# build click track: 8 clicks, 600ms apart, 44.1kHz 16-bit mono
$sr=44100; $n=[int]($sr*4.8); $data=New-Object int16[] $n
for($c=0;$c -lt 8;$c++){ $s=[int]($c*0.6*$sr); for($i=0;$i -lt 220;$i++){ $data[$s+$i]=[int16](28000*[math]::Sin(2*[math]::PI*2000*$i/$sr)*[math]::Exp(-$i/40)) } }
$ms=New-Object IO.MemoryStream; $w=New-Object IO.BinaryWriter $ms
$w.Write([Text.Encoding]::ASCII.GetBytes('RIFF')); $w.Write([int](36+$n*2)); $w.Write([Text.Encoding]::ASCII.GetBytes('WAVEfmt '))
$w.Write([int]16); $w.Write([int16]1); $w.Write([int16]1); $w.Write([int]$sr); $w.Write([int]($sr*2)); $w.Write([int16]2); $w.Write([int16]16)
$w.Write([Text.Encoding]::ASCII.GetBytes('data')); $w.Write([int]($n*2)); foreach($v in $data){$w.Write($v)}
$tts=New-Object System.Speech.Synthesis.SpeechSynthesizer; $tts.Rate=0
[void][V]::VBVMR_Login(); Start-Sleep 1
$tests=@(@(150,0),@(100,0),@(50,0),@(0,0),@(0,50),@(0,100),@(0,150))
for($t=0;$t -lt $tests.Count;$t++){
  [void][V]::VBVMR_SetParameters("Option.delay[0]=$($tests[$t][0]);Option.delay[1]=$($tests[$t][1])")
  Start-Sleep -Milliseconds 500
  $tts.Speak("Test $($t+1)"); Start-Sleep -Milliseconds 700
  $ms.Position=0; $p=New-Object System.Media.SoundPlayer $ms; $p.PlaySync(); Start-Sleep 2
}
[void][V]::VBVMR_SetParameters("Option.delay[0]=0;Option.delay[1]=0")
[void][V]::VBVMR_Logout(); "done"
