$ErrorActionPreference = "Stop"

$dotnetRoot = "D:\spire mod\.tools\dotnet9"
$env:DOTNET_ROOT = $dotnetRoot
$env:DOTNET_CLI_HOME = "D:\spire mod\.tools\dotnet-cli-home"
$env:DOTNET_HOST_PATH = "$dotnetRoot\dotnet.exe"
$env:PATH = "$dotnetRoot;$env:PATH"

& "$dotnetRoot\dotnet.exe" build "$PSScriptRoot\Nymph.csproj" @args
exit $LASTEXITCODE
