Set shell = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")

root = fso.GetParentFolderName(WScript.ScriptFullName)
exe = fso.BuildPath(root, "publish\SteamControllerGamepadViewer.exe")

If Not fso.FileExists(exe) Then
  exe = fso.BuildPath(root, "SteamControllerGamepadViewer.exe")
End If

If Not fso.FileExists(exe) Then
  WScript.Echo "SteamControllerGamepadViewer.exe was not found. Use Run-SteamControllerViewer.cmd from a source checkout, or download a release build."
  WScript.Quit 1
End If

shell.Run """" & exe & """ --urls ""http://127.0.0.1:31337""", 0, False
