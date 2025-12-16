#Requires -Version 5.1
<#
.SYNOPSIS
Builds, tests, packs, and installs/updates the git-forest .NET global tool from a local feed.

.DESCRIPTION
- Verifies the git working tree is clean (unless -SkipGitCleanCheck)
- Builds and tests the solution
- Packs the .NET global tool into ./artifacts/packages
- Updates the globally installed tool from that local package feed

This repository uses MinVer (git tags) to compute the package version.
If -Version is not provided, the script will infer the version from the newest produced .nupkg.

.PARAMETER Version
Optional explicit package version to install/update (e.g. 0.2.1 or 0.2.0-alpha.1).

.PARAMETER Configuration
Build configuration (Debug/Release). Defaults to Release.

.PARAMETER SkipGitCleanCheck
Skip the check for uncommitted git changes.

.PARAMETER SkipBuild
Skip dotnet build.

.PARAMETER SkipTests
Skip dotnet test.

.PARAMETER SkipPack
Skip dotnet pack.

.PARAMETER NoToolUpdate
Do not run dotnet tool update.

.PARAMETER Clean
Delete existing nupkgs in artifacts/packages before packing.

.EXAMPLE
./build.ps1

.EXAMPLE
./build.ps1 -SkipGitCleanCheck

.EXAMPLE
./build.ps1 -Version 0.2.1 -SkipGitCleanCheck
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$Version,

    [Parameter(Mandatory = $false)]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [Parameter(Mandatory = $false)]
    [switch]$SkipGitCleanCheck,

    [Parameter(Mandatory = $false)]
    [switch]$SkipBuild,

    [Parameter(Mandatory = $false)]
    [switch]$SkipTests,

    [Parameter(Mandatory = $false)]
    [switch]$SkipPack,

    [Parameter(Mandatory = $false)]
    [switch]$NoToolUpdate,

    [Parameter(Mandatory = $false)]
    [switch]$Clean
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$InformationPreference = 'Continue'

function Assert-CommandAvailable {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    $cmd = $null
    $cmd = Get-Command -Name $Name -ErrorAction SilentlyContinue
    if ($null -eq $cmd) {
        throw "Required command '$Name' was not found on PATH."
    }
}

function Invoke-External {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,

        [Parameter(Mandatory = $false)]
        [string[]]$Arguments = @()
    )

    Write-Information ("`n> {0} {1}" -f $FilePath, ($Arguments -join ' '))
    & $FilePath @Arguments
    $exitCode = $LASTEXITCODE
    if ($exitCode -ne 0) {
        throw ("Command failed with exit code {0}: {1} {2}" -f $exitCode, $FilePath, ($Arguments -join ' '))
    }
}

$repoRoot = $null
$repoRoot = (Resolve-Path -LiteralPath $PSScriptRoot).Path

$artifactsDir = $null
$packagesDir = $null
$artifactsDir = Join-Path -Path $repoRoot -ChildPath 'artifacts'
$packagesDir = Join-Path -Path $artifactsDir -ChildPath 'packages'

$solutionPath = $null
$cliProjectPath = $null
$solutionPath = Join-Path -Path $repoRoot -ChildPath 'GitForest.sln'
$cliProjectPath = Join-Path -Path $repoRoot -ChildPath 'src/GitForest.Cli/GitForest.Cli.csproj'

Assert-CommandAvailable -Name 'git'
Assert-CommandAvailable -Name 'dotnet'

Push-Location -LiteralPath $repoRoot
try {
    if (-not $SkipGitCleanCheck) {
        $porcelain = $null
        $porcelain = & git status --porcelain
        $exitCode = $LASTEXITCODE
        if ($exitCode -ne 0) {
            throw "git status failed with exit code $exitCode"
        }

        if ($null -ne $porcelain -and $porcelain.Trim().Length -gt 0) {
            Write-Warning "Working tree is not clean:"
            Write-Warning $porcelain
            throw "Refusing to build with uncommitted changes. Commit/stash them or rerun with -SkipGitCleanCheck."
        }
    }

    New-Item -Path $packagesDir -ItemType Directory -Force | Out-Null

    if ($Clean) {
        $existing = $null
        $existing = Get-ChildItem -LiteralPath $packagesDir -Filter '*.nupkg' -File -ErrorAction SilentlyContinue
        if ($null -ne $existing -and $existing.Count -gt 0) {
            Remove-Item -LiteralPath $existing.FullName -Force
        }
    }

    if (-not $SkipBuild) {
        Invoke-External -FilePath 'dotnet' -Arguments @(
            'build',
            $solutionPath,
            '--configuration', $Configuration,
            '--nologo'
        )
    }

    if (-not $SkipTests) {
        Invoke-External -FilePath 'dotnet' -Arguments @(
            'test',
            $solutionPath,
            '--configuration', $Configuration,
            '--nologo'
        )
    }

    if (-not $SkipPack) {
        $packArgs = @(
            'pack',
            $cliProjectPath,
            '--configuration', $Configuration,
            '--output', $packagesDir,
            '--nologo'
        )

        if (-not $SkipBuild) {
            $packArgs += '--no-build'
        }

        Invoke-External -FilePath 'dotnet' -Arguments $packArgs
    }

    $toolVersion = $null
    if ($null -ne $Version -and $Version.Trim().Length -gt 0) {
        $toolVersion = $Version.Trim()
    }
    else {
        $nupkgs = $null
        $nupkgs = Get-ChildItem -LiteralPath $packagesDir -Filter 'git-forest.*.nupkg' -File -ErrorAction SilentlyContinue |
            Sort-Object -Property LastWriteTime -Descending

        if ($null -ne $nupkgs -and $nupkgs.Count -gt 0) {
            $latest = $null
            $latest = $nupkgs[0]
            $m = $null
            $m = [regex]::Match($latest.BaseName, '^git-forest\.(?<version>.+)$')
            if ($null -ne $m -and $m.Success -and $m.Groups['version'].Value.Trim().Length -gt 0) {
                $toolVersion = $m.Groups['version'].Value.Trim()
            }
        }
    }

    if (-not $NoToolUpdate) {
        $updateArgs = @('tool', 'update', '--global', 'git-forest', '--source', $packagesDir)
        if ($null -ne $toolVersion -and $toolVersion.Trim().Length -gt 0) {
            $updateArgs += @('--version', $toolVersion)
        }

        Invoke-External -FilePath 'dotnet' -Arguments $updateArgs
    }

    Write-Information "`nDone."
    Write-Information ("Packages: {0}" -f $packagesDir)
    if ($null -ne $toolVersion -and $toolVersion.Trim().Length -gt 0) {
        Write-Information ("Tool version: {0}" -f $toolVersion)
    }
}
finally {
    Pop-Location
}
