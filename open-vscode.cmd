@echo off
set "DOTNET_ROOT=D:\spire mod\.tools\dotnet9"
set "DOTNET_CLI_HOME=D:\spire mod\.tools\dotnet-cli-home"
set "DOTNET_HOST_PATH=D:\spire mod\.tools\dotnet9\dotnet.exe"
set "PATH=D:\spire mod\.tools\dotnet9;%PATH%"
code --new-window "%~dp0"
