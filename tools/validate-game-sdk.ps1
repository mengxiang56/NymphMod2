param(
    [Parameter(Mandatory = $true)]
    [string]$ReleaseInfoPath,

    [Parameter(Mandatory = $true)]
    [string]$ExpectedVersion,

    [Parameter(Mandatory = $true)]
    [string]$ExpectedBranch
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $ReleaseInfoPath)) {
    throw "Could not find '$ReleaseInfoPath'. The $ExpectedBranch build must use a complete matching game installation."
}

$releaseInfo = Get-Content -Raw -Encoding UTF8 -LiteralPath $ReleaseInfoPath | ConvertFrom-Json
$actualVersion = ([string]$releaseInfo.version).TrimStart("v")
if ($actualVersion -ne $ExpectedVersion) {
    throw "The $ExpectedBranch build requires game SDK v$ExpectedVersion, but '$ReleaseInfoPath' reports v$actualVersion."
}
