<#
.SYNOPSIS
    Runs the end-to-end regression suite in a throwaway, isolated copy of the whole system.
.DESCRIPTION
    Starts the compose project eac-e2e, waits until it answers, runs the checks through the gateway and
    destroys the environment and its data. The development stack is never touched.
    Works on Windows PowerShell 5.1 and pwsh 7. Exit code 0 only if the stack was ready and no check failed.
.PARAMETER Assistant
    Also runs the real assistant check, which calls a paid service. The key comes from the ANTHROPIC_API_KEY
    environment variable or the root .env file. Without a key the check is reported as skipped.
.PARAMETER KeepEnvironment
    Doesn't destroy the environment at the end, to investigate a failure. Prints the command that destroys it.
.PARAMETER ReadyTimeoutSeconds
    Maximum time to wait for the environment to become ready.
.PARAMETER GatewayPort
    Host port published for the gateway.
#>
[CmdletBinding()]
param(
    [switch]$Assistant,
    [switch]$KeepEnvironment,
    [int]$ReadyTimeoutSeconds = 300,
    [int]$GatewayPort = 19000
)

$ErrorActionPreference = 'Continue'

$root = Split-Path -Parent $PSScriptRoot
$testProject = Join-Path $PSScriptRoot 'EmployeeAttendanceControl.e2e.test'
$resultsDirectory = Join-Path $PSScriptRoot 'TestResults'
$trxPath = Join-Path $resultsDirectory 'e2e.trx'
$baseUrl = "http://localhost:$GatewayPort"
$adminUser = 'admin'
$adminPassword = '1234'
$composeFiles = @('docker-compose.yml', 'docker-compose.override.yml', 'docker-compose.e2e.yml') | ForEach-Object { '-f'; (Join-Path $root $_) }
$composeText = 'docker compose -p eac-e2e -f docker-compose.yml -f docker-compose.override.yml -f docker-compose.e2e.yml'

# A plain function on purpose: an advanced one would read compose's -d and -v as -Debug and -Verbose
function Invoke-Compose {
    & docker compose -p eac-e2e --project-directory $root @composeFiles @args
}

function Get-KeyFromDotEnv {
    $dotEnv = Join-Path $root '.env'
    if (Test-Path $dotEnv) {
        foreach ($line in Get-Content $dotEnv) {
            if ($line -match '^\s*ANTHROPIC_API_KEY\s*=\s*(.+?)\s*$') {
                return $Matches[1].Trim('"', "'")
            }
        }
    }
    return $null
}

function Test-Probe {
    param([string]$Method, [string]$Path, [string]$Token, [string]$Body)
    $request = @{ Method = $Method; Uri = "$baseUrl$Path"; UseBasicParsing = $true; TimeoutSec = 10; ErrorAction = 'Stop' }
    if ($Token) { $request.Headers = @{ Authorization = "Bearer $Token" } }
    if ($Body) { $request.Body = $Body; $request.ContentType = 'application/json' }
    try {
        return Invoke-WebRequest @request
    }
    catch {
        return $null
    }
}

# Returns the probes that haven't succeeded when the time limit passes, or nothing if the stack is ready
function Wait-Ready {
    param([int]$TimeoutSeconds)
    $authentication = 'POST /Authentication (gateway, users, Cassandra)'
    $employees = 'GET /Employees (employees, SQL Server)'
    $attendances = 'GET /Attendances (attendances, Mongo)'
    $pending = [ordered]@{
        $authentication = @('ms.gateway.api', 'ms.users.api', 'ms.cassandra.db')
        $employees      = @('ms.employees.api', 'ms.sql.employees.db')
        $attendances    = @('ms.attendances.api', 'ms.mongo.attendances.db')
    }
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $body = @{ userName = $adminUser; password = $adminPassword } | ConvertTo-Json

    while ($true) {
        $login = Test-Probe -Method 'Post' -Path '/Authentication' -Body $body
        if ($login) {
            $token = ([string]$login.Content).Trim()
            $pending.Remove($authentication)
            if ($pending.Contains($employees) -and (Test-Probe -Method 'Get' -Path '/Employees' -Token $token)) {
                $pending.Remove($employees)
            }
            if ($pending.Contains($attendances) -and (Test-Probe -Method 'Get' -Path "/Attendances?userName=$adminUser" -Token $token)) {
                $pending.Remove($attendances)
            }
        }

        if ($pending.Count -eq 0) {
            return
        }
        if ((Get-Date) -ge $deadline) {
            return $pending.GetEnumerator() | ForEach-Object { [pscustomobject]@{ Probe = $_.Key; Services = $_.Value } }
        }
        Start-Sleep -Seconds 5
    }
}

function Write-Report {
    if (-not (Test-Path $trxPath)) {
        Write-Host 'No test results were produced.' -ForegroundColor Red
        return
    }

    [xml]$trx = Get-Content -Raw -Path $trxPath
    $counters = $trx.TestRun.ResultSummary.Counters
    $results = @($trx.TestRun.Results.UnitTestResult)
    $skipped = @($results | Where-Object { $_.outcome -eq 'NotExecuted' })
    $failed = @($results | Where-Object { $_.outcome -eq 'Failed' })

    Write-Host ''
    Write-Host "E2E result: total $($counters.total), passed $($counters.passed), failed $($counters.failed), skipped $($skipped.Count)"
    foreach ($test in $skipped) {
        Write-Host "  SKIPPED $($test.testName): $($test.Output.ErrorInfo.Message)" -ForegroundColor Yellow
    }
    foreach ($test in $failed) {
        Write-Host "  FAILED  $($test.testName)" -ForegroundColor Red
        Write-Host "          $($test.Output.ErrorInfo.Message)" -ForegroundColor Red
    }
}

# Stops at the first problem, leaving $exitCode at 1. A return at script level would end the script with exit code 0.
function Invoke-Suite {
    & docker version --format '{{.Server.Version}}' *> $null
    if ($LASTEXITCODE -ne 0) {
        Write-Host 'Docker is not available: the suite did not run.' -ForegroundColor Red
        return
    }

    Write-Host '==> Removing leftovers of previous runs'
    Invoke-Compose down -v --remove-orphans

    Write-Host '==> Building and starting the environment'
    Invoke-Compose up -d --build
    if ($LASTEXITCODE -ne 0) {
        Write-Host 'The environment could not be built or started. No checks were run.' -ForegroundColor Red
        Invoke-Compose ps -a
        return
    }

    Write-Host "==> Waiting for the environment (up to $ReadyTimeoutSeconds s)"
    $notReady = @(Wait-Ready -TimeoutSeconds $ReadyTimeoutSeconds)
    if ($notReady.Count -gt 0) {
        Write-Host 'The environment was not ready in time. No checks were run. Not ready:' -ForegroundColor Red
        $notReady | ForEach-Object { Write-Host "  - $($_.Probe)" -ForegroundColor Red }
        Invoke-Compose ps -a
        $notReady | ForEach-Object { $_.Services } | Sort-Object -Unique | ForEach-Object {
            Write-Host "--- Last log lines of $_"
            Invoke-Compose logs --tail 50 $_
        }
        return
    }

    Write-Host '==> Running the checks'
    if (Test-Path $trxPath) { Remove-Item $trxPath -Force }

    # The tests receive only a flag: the key itself never reaches them
    $env:ANTHROPIC_API_KEY = $null
    $env:E2E_BASE_URL = $baseUrl
    $env:E2E_ADMIN_USER = $adminUser
    $env:E2E_ADMIN_PASSWORD = $adminPassword
    $env:E2E_ASSISTANT = if ($Assistant) { 'true' } else { $null }
    $env:E2E_ASSISTANT_KEY_PROVIDED = if ($key) { 'true' } else { $null }

    & dotnet test $testProject -p:RunE2E=true --nologo --results-directory $resultsDirectory --logger 'trx;LogFileName=e2e.trx' --logger 'console;verbosity=normal'
    $testExitCode = $LASTEXITCODE

    Write-Report
    if ($testExitCode -eq 0) { $script:exitCode = 0 }
}

$exitCode = 1
$originalKey = $env:ANTHROPIC_API_KEY
$originalPort = $env:E2E_GATEWAY_PORT
$env:E2E_GATEWAY_PORT = "$GatewayPort"

# The key is available only when the assistant check is requested. A value set here wins over the root .env file.
$key = $null
if ($Assistant) {
    $key = if ($originalKey) { $originalKey } else { Get-KeyFromDotEnv }
}
$env:ANTHROPIC_API_KEY = if ($key) { $key } else { 'e2e-assistant-disabled' }

try {
    Invoke-Suite
}
finally {
    # compose interpolates this variable in every command, and the key is no longer needed
    $env:ANTHROPIC_API_KEY = 'e2e-assistant-disabled'

    if ($KeepEnvironment) {
        Write-Host ''
        Write-Host 'The environment was kept. Destroy it from the repository root with:'
        Write-Host "  $composeText down -v --remove-orphans"
    }
    else {
        Write-Host '==> Destroying the environment'
        Invoke-Compose down -v --remove-orphans
    }

    $env:ANTHROPIC_API_KEY = $originalKey
    $env:E2E_GATEWAY_PORT = $originalPort
}

Write-Host ''
if ($exitCode -eq 0) {
    Write-Host 'E2E regression suite PASSED' -ForegroundColor Green
}
else {
    Write-Host 'E2E regression suite FAILED' -ForegroundColor Red
}
exit $exitCode
