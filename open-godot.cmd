@echo off
set "DOTNET_ROOT=D:\spire mod\.tools\dotnet9"
set "DOTNET_HOST_PATH=D:\spire mod\.tools\dotnet9\dotnet.exe"
set "PATH=D:\spire mod\.tools\dotnet9;%PATH%"
"D:\spire mod\.tools\godot-4.5.1-mono\Godot_v4.5.1-stable_mono_win64\Godot_v4.5.1-stable_mono_win64.exe" --editor --path "%~dp0"
