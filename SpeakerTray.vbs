' Starts SpeakerTray without a console window
Set fso = CreateObject("Scripting.FileSystemObject")
dir = fso.GetParentFolderName(WScript.ScriptFullName)
ps1 = dir & "\SpeakerTray.ps1"
If Not fso.FileExists(ps1) Then
    MsgBox "SpeakerTray: cannot find " & ps1, vbExclamation, "SpeakerTray"
    WScript.Quit 1
End If
CreateObject("WScript.Shell").Run "powershell.exe -NoProfile -STA -ExecutionPolicy Bypass -WindowStyle Hidden -File """ & ps1 & """", 0, False
