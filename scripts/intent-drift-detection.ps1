[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# Suppress noisy Orleans/.NET host logs by default. Use -Verbose to see them.
$logLevel = if ($VerbosePreference -ne 'SilentlyContinue') { 'Information' } else { 'Warning' }
Write-Verbose "Setting .NET log level to '$logLevel' (use -Verbose to see Orleans logs)."
$env:Logging__LogLevel__Default = $logLevel
$env:Logging__LogLevel__Microsoft = $logLevel
$env:Logging__LogLevel__Orleans = $logLevel
Set-Item -Path 'Env:Logging__LogLevel__Microsoft.Hosting.Lifetime' -Value $logLevel

$repoRoot = (Resolve-Path -LiteralPath (Join-Path -Path $PSScriptRoot -ChildPath '..')).Path
$planPath = Join-Path -Path $repoRoot -ChildPath 'config\plans\experimental\intent-drift-detection.yaml'

if (-not (Test-Path -LiteralPath $planPath -PathType Leaf))
{
    throw "Plan file not found: $planPath"
}

$gfCommand = Get-Command -Name 'gf' -ErrorAction SilentlyContinue
$useDotnetRun = $false
if ($null -eq $gfCommand)
{
    $useDotnetRun = $true
}

function Invoke-Gf
{
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Args
    )

    if ($useDotnetRun)
    {
        $cliProject = Join-Path -Path $repoRoot -ChildPath 'src\GitForest.Cli'
        & dotnet run --project $cliProject -- @Args
    }
    else
    {
        & gf @Args
    }

    $exitCode = $LASTEXITCODE
    if ($exitCode -ne 0)
    {
        $argsText = $Args -join ' '
        $prefix = if ($useDotnetRun) { 'dotnet run --project src/GitForest.Cli --' } else { 'gf' }
        throw "Command failed (exit code $exitCode): $prefix $argsText"
    }
}

function Invoke-GfCapture
{
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Args
    )

    $output = $null

    if ($useDotnetRun)
    {
        $cliProject = Join-Path -Path $repoRoot -ChildPath 'src\GitForest.Cli'
        $output = & dotnet run --project $cliProject -- @Args 2>&1
    }
    else
    {
        $output = & gf @Args 2>&1
    }

    return [pscustomobject]@{
        ExitCode = $LASTEXITCODE
        Lines    = @($output)
    }
}

function Get-ForestStatusJson
{
    $result = Invoke-GfCapture -Args @('status', '--json')

    $jsonLine = $null
    foreach ($line in $result.Lines)
    {
        $text = ($line | Out-String).Trim()
        if ($text.StartsWith('{'))
        {
            $jsonLine = $text
            break
        }
    }

    if ($null -eq $jsonLine)
    {
        $joined = ($result.Lines | ForEach-Object { ($_ | Out-String).TrimEnd() }) -join "`n"
        throw "Could not find JSON output from 'git-forest status --json'. Output was:`n$joined"
    }

    return ($jsonLine | ConvertFrom-Json)
}

function Ensure-OrleansAvailable
{
    param(
        [Parameter(Mandatory = $true)]
        [int]$TimeoutSeconds
    )

    $status = Get-ForestStatusJson

    $connection = $null
    if ($null -ne $status.PSObject.Properties['connection'])
    {
        $connection = $status.connection
    }

    if ($null -eq $connection)
    {
        return
    }

    $type = $null
    $available = $null
    $details = $null

    if ($null -ne $connection.PSObject.Properties['type']) { $type = $connection.type }
    if ($null -ne $connection.PSObject.Properties['available']) { $available = $connection.available }
    if ($null -ne $connection.PSObject.Properties['details']) { $details = $connection.details }

    if ($type -ne 'orleans')
    {
        return
    }

    if ($available -eq $true)
    {
        return
    }

    Write-Host "Orleans backend is unavailable ($details). Starting local AppHost via Aspire..."

    $aspireCommand = Get-Command -Name 'aspire' -ErrorAction SilentlyContinue
    if ($null -eq $aspireCommand)
    {
        Write-Error "Aspire CLI ('aspire') not found on PATH. Start the AppHost manually (recommended: 'aspire run') and re-run this script."
        exit 24
    }

    $appHostProject = Join-Path -Path $repoRoot -ChildPath 'src\GitForest.AppHost\GitForest.AppHost.csproj'
    if (-not (Test-Path -LiteralPath $appHostProject -PathType Leaf))
    {
        Write-Error "AppHost project not found at: $appHostProject"
        exit 24
    }

    $proc = Start-Process -FilePath 'aspire' -ArgumentList @('run', '--project', $appHostProject) -PassThru
    if ($null -eq $proc)
    {
        Write-Error 'Failed to start Aspire AppHost process.'
        exit 24
    }

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline)
    {
        Start-Sleep -Seconds 1

        $status = $null
        try
        {
            $status = Get-ForestStatusJson
        }
        catch
        {
            $status = $null
        }

        if ($null -ne $status -and $null -ne $status.PSObject.Properties['connection'])
        {
            $c = $status.connection
            if ($null -ne $c -and ($c.type -eq 'orleans') -and ($c.available -eq $true))
            {
                Write-Host 'Orleans backend is now reachable.'
                return
            }
        }
    }

    Write-Error "Timed out waiting for Orleans backend to become reachable. (You may need to keep AppHost running.)"
    exit 24
}

Write-Host "Installing plan: $planPath"
Invoke-Gf -Args @('plans', 'install', $planPath)

Ensure-OrleansAvailable -TimeoutSeconds 30

Write-Host 'Evolving all plants'
Invoke-Gf -Args @('evolve', '--all')
