param(
    [Parameter(Mandatory = $true)]
    [string]$TemplatePath,

    [Parameter(Mandatory = $true)]
    [string]$OutputPath,

    [Parameter(Mandatory = $true)]
    [string]$MinGameVersion,

    [Parameter(Mandatory = $true)]
    [string]$RitsuLibVersion
)

$ErrorActionPreference = "Stop"

$manifest = Get-Content -Raw -Encoding UTF8 -LiteralPath $TemplatePath | ConvertFrom-Json
$manifest.min_game_version = $MinGameVersion

$ritsuDependency = $manifest.dependencies | Where-Object { $_.id -eq "STS2-RitsuLib" } | Select-Object -First 1
if ($null -eq $ritsuDependency) {
    throw "Nymph.json does not contain the STS2-RitsuLib dependency."
}

$ritsuDependency.version = $RitsuLibVersion
$outputDirectory = Split-Path -Parent $OutputPath
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
$json = $manifest | ConvertTo-Json -Depth 16
[System.IO.File]::WriteAllText(
    $OutputPath,
    $json + [Environment]::NewLine,
    [System.Text.UTF8Encoding]::new($false))
