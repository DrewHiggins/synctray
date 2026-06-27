<#
.SYNOPSIS
    Bumps the SyncTray application version using semantic versioning.

.DESCRIPTION
    Reads the current version from src/SyncTray/Constants.cs (the source of truth),
    increments it according to the requested -Type, and writes the new version to:
      - src/SyncTray/Constants.cs (used to show the version in the app)
      - .github/workflows/build.yml (the AppVersion env var passed to the installer)

    Any 'v' prefix and pre-release suffix (e.g. v0.0.1-int0) in Constants.cs are preserved.
    The build.yml AppVersion uses only the numeric MAJOR.MINOR.PATCH core.

.PARAMETER Type
    Which part of the version to bump:
      Major - increments MAJOR, resets MINOR and PATCH to 0
      Minor - increments MINOR, resets PATCH to 0
      Fix   - increments PATCH

.EXAMPLE
    ./scripts/BumpVersion.ps1 -Type Minor
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Major', 'Minor', 'Fix')]
    [string]$Type
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$constantsPath = Join-Path $repoRoot 'src/SyncTray/Constants.cs'
$workflowPath = Join-Path $repoRoot '.github/workflows/build.yml'

foreach ($path in @($constantsPath, $workflowPath)) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Expected file not found: $path"
    }
}

# Read the current version from Constants.cs (source of truth).
$constantsContent = Get-Content -LiteralPath $constantsPath -Raw
$versionRegex = 'Version\s*=\s*"(?<prefix>v?)(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)(?<suffix>[^"]*)"'
$match = [regex]::Match($constantsContent, $versionRegex)
if (-not $match.Success) {
    throw "Could not find a Version constant in $constantsPath"
}

$prefix = $match.Groups['prefix'].Value
$suffix = $match.Groups['suffix'].Value
$major = [int]$match.Groups['major'].Value
$minor = [int]$match.Groups['minor'].Value
$patch = [int]$match.Groups['patch'].Value

$oldCore = "$major.$minor.$patch"

switch ($Type) {
    'Major' { $major++; $minor = 0; $patch = 0 }
    'Minor' { $minor++; $patch = 0 }
    'Fix'   { $patch++ }
}

$newCore = "$major.$minor.$patch"
$newFullVersion = "$prefix$newCore$suffix"

# Update Constants.cs, preserving the prefix and pre-release suffix.
$newConstantsContent = [regex]::Replace(
    $constantsContent,
    $versionRegex,
    "Version = `"$newFullVersion`""
)
Set-Content -LiteralPath $constantsPath -Value $newConstantsContent -NoNewline

# Update the AppVersion env var in build.yml (numeric core only).
$workflowContent = Get-Content -LiteralPath $workflowPath -Raw
$workflowRegex = '(?<prelude>AppVersion:\s*)(?<version>\d+\.\d+\.\d+)'
if (-not [regex]::Match($workflowContent, $workflowRegex).Success) {
    throw "Could not find an 'AppVersion:' entry in $workflowPath"
}
$newWorkflowContent = [regex]::Replace($workflowContent, $workflowRegex, "`${prelude}$newCore")
Set-Content -LiteralPath $workflowPath -Value $newWorkflowContent -NoNewline

Write-Host "Bumped version ($Type): $oldCore -> $newCore"
Write-Host "  Constants.cs Version = $newFullVersion"
Write-Host "  build.yml AppVersion = $newCore"
