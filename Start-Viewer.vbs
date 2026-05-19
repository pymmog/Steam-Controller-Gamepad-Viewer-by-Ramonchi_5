Set shell = CreateObject("WScript.Shell")
Set fso = CreateObject("Scripting.FileSystemObject")

root = fso.GetParentFolderName(WScript.ScriptFullName)
launcher = fso.BuildPath(root, "Launch-Viewer.vbs")

If Not fso.FileExists(launcher) Then
  WScript.Echo "Launch-Viewer.vbs was not found."
  WScript.Quit 1
End If

shell.Run "wscript.exe """ & launcher & """", 0, False
