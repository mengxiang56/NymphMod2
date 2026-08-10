param(
    [ValidateSet("all", "public", "public-beta")]
    [string]$Branch = "all",

    [switch]$SkipPckExport
)

$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
$artifactRoot = Join-Path $projectRoot "artifacts/workshop"
$dotnetRoot = "D:\spire mod\.tools\dotnet9"
$dotnet = Join-Path $dotnetRoot "dotnet.exe"
$project = Join-Path $projectRoot "Nymph.csproj"
$branches = if ($Branch -eq "all") { @("public", "public-beta") } else { @($Branch) }

if (-not (Test-Path -LiteralPath $dotnet)) {
    throw "Bundled .NET SDK was not found at '$dotnet'."
}

$env:DOTNET_ROOT = $dotnetRoot
$env:DOTNET_CLI_HOME = "D:\spire mod\.tools\dotnet-cli-home"
$env:DOTNET_HOST_PATH = $dotnet
$env:PATH = "$dotnetRoot;$env:PATH"

foreach ($currentBranch in $branches) {
    $workspaceDir = Join-Path $artifactRoot $currentBranch
    $contentDir = Join-Path $workspaceDir "content"
    $workspaceFullPath = [System.IO.Path]::GetFullPath($workspaceDir)
    $artifactFullPath = [System.IO.Path]::GetFullPath($artifactRoot)

    if (-not $workspaceFullPath.StartsWith($artifactFullPath, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to clean an output directory outside '$artifactFullPath'."
    }

    if ([System.IO.Directory]::Exists("\\?\$workspaceFullPath")) {
        [System.IO.Directory]::Delete("\\?\$workspaceFullPath", $true)
    }
    New-Item -ItemType Directory -Force -Path $contentDir | Out-Null

    $runPckExport = if ($SkipPckExport) { "false" } else { "true" }
    $buildArgs = @(
        "build",
        $project,
        "-c", "Release",
        "/p:GameBranch=$currentBranch",
        "/p:ModOutputDir=$contentDir",
        "/p:CopyModOnBuild=true",
        "/p:RunPckExport=$runPckExport",
        "/p:RitsuLibAutoCopy=false"
    )

    & $dotnet @buildArgs
    if ($LASTEXITCODE -ne 0) {
        throw "The '$currentBranch' build failed."
    }

    $workshopTemplate = Join-Path $projectRoot "workshop/$currentBranch/workshop.json"
    Copy-Item -Force -LiteralPath $workshopTemplate -Destination (Join-Path $workspaceDir "workshop.json")

    foreach ($sharedFile in @("mod_id.txt", "image.png")) {
        $sharedPath = Join-Path $projectRoot "workshop/$sharedFile"
        if (Test-Path -LiteralPath $sharedPath) {
            Copy-Item -Force -LiteralPath $sharedPath -Destination (Join-Path $workspaceDir $sharedFile)
        }
    }

    $requiredFiles = @("Nymph.dll", "Nymph.json")
    if (-not $SkipPckExport) {
        $requiredFiles += "Nymph.pck"
    }
    foreach ($requiredFile in $requiredFiles) {
        $requiredPath = Join-Path $contentDir $requiredFile
        if (-not (Test-Path -LiteralPath $requiredPath)) {
            throw "The '$currentBranch' package is missing '$requiredFile'."
        }
    }

    $manifest = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $contentDir "Nymph.json") | ConvertFrom-Json
    $expectedMinVersion = if ($currentBranch -eq "public") { "0.107.1" } else { "0.110.1" }
    if ($manifest.min_game_version -ne $expectedMinVersion) {
        throw "The '$currentBranch' manifest has min_game_version '$($manifest.min_game_version)', expected '$expectedMinVersion'."
    }
    $ritsuDependency = $manifest.dependencies | Where-Object { $_.id -eq "STS2-RitsuLib" } | Select-Object -First 1
    if ($null -eq $ritsuDependency -or $ritsuDependency.version -ne "0.5.11") {
        throw "The '$currentBranch' manifest must depend on STS2-RitsuLib 0.5.11."
    }

    $workshopConfig = Get-Content -Raw -Encoding UTF8 -LiteralPath (Join-Path $workspaceDir "workshop.json") | ConvertFrom-Json
    if ($workshopConfig.minBranch -ne $currentBranch -or $workshopConfig.maxBranch -ne $currentBranch) {
        throw "The '$currentBranch' workshop configuration does not target that branch exactly."
    }

    Write-Host "Prepared $currentBranch workshop workspace: $workspaceDir"
}
