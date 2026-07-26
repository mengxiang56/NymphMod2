$ErrorActionPreference = "Stop"

$dotnetRoot = "D:\spire mod\.tools\dotnet9"
$env:DOTNET_ROOT = $dotnetRoot
$env:DOTNET_HOST_PATH = "$dotnetRoot\dotnet.exe"
$env:PATH = "$dotnetRoot;$env:PATH"
$godot = "D:\spire mod\.tools\godot-4.5.1-mono\Godot_v4.5.1-stable_mono_win64\Godot_v4.5.1-stable_mono_win64.exe"

& $godot --editor --path $PSScriptRoot
