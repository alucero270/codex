$ErrorActionPreference = "Stop"

function Invoke-Step {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [Parameter(Mandatory = $true)]
        [scriptblock]$Action
    )

    Write-Host "==> $Name" -ForegroundColor Cyan
    & $Action
    Write-Host "OK: $Name" -ForegroundColor Green
}

function Assert-LastExitCode {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Message
    )

    if ($LASTEXITCODE -ne 0) {
        throw "$Message Exit code: $LASTEXITCODE."
    }
}

function Get-FreeTcpPort {
    $listener = [System.Net.Sockets.TcpListener]::new(
        [System.Net.IPAddress]::Loopback,
        0
    )
    $listener.Start()
    try {
        return ([System.Net.IPEndPoint]$listener.LocalEndpoint).Port
    }
    finally {
        $listener.Stop()
    }
}

function Restore-EnvironmentVariable {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [string]$Value
    )

    if ($null -eq $Value) {
        Remove-Item "Env:$Name" -ErrorAction SilentlyContinue
        return
    }

    Set-Item "Env:$Name" $Value
}

function Test-ApiReadiness {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot
    )

    $port = Get-FreeTcpPort
    $apiUrl = "http://127.0.0.1:$port"
    $stdoutLog = Join-Path $env:TEMP "strata-platform-api.stdout.log"
    $stderrLog = Join-Path $env:TEMP "strata-platform-api.stderr.log"

    Remove-Item $stdoutLog, $stderrLog -ErrorAction SilentlyContinue

    $savedEnv = @{
        ASPNETCORE_ENVIRONMENT = $env:ASPNETCORE_ENVIRONMENT
        ASPNETCORE_URLS = $env:ASPNETCORE_URLS
        Codex__DocsRootPath = $env:Codex__DocsRootPath
        ConnectionStrings__Default = $env:ConnectionStrings__Default
    }

    $env:ASPNETCORE_ENVIRONMENT = "Development"
    $env:ASPNETCORE_URLS = $apiUrl
    $env:Codex__DocsRootPath = Join-Path $RepoRoot "docs"
    $env:ConnectionStrings__Default = (
        "Host=localhost;Port=5432;Database=strata;Username=strata;Password=strata"
    )

    $processArgs = (
        "run --project src/Codex.Api/Codex.Api.csproj " +
        "--no-build --no-launch-profile"
    )

    $process = Start-Process dotnet `
        -ArgumentList $processArgs `
        -WorkingDirectory $RepoRoot `
        -RedirectStandardOutput $stdoutLog `
        -RedirectStandardError $stderrLog `
        -PassThru

    try {
        $deadline = (Get-Date).AddSeconds(45)
        $ready = $false

        while ((Get-Date) -lt $deadline) {
            Start-Sleep -Seconds 2

            if ($process.HasExited) {
                break
            }

            & curl.exe --fail --silent --show-error "$apiUrl/health" > $null
            if ($LASTEXITCODE -eq 0) {
                $ready = $true
                break
            }
        }

        if (-not $ready) {
            Write-Host "API readiness probe logs:" -ForegroundColor Yellow
            if (Test-Path $stdoutLog) {
                Get-Content $stdoutLog
            }
            if (Test-Path $stderrLog) {
                Get-Content $stderrLog
            }
            throw "API readiness probe did not succeed at $apiUrl/health."
        }
    }
    finally {
        if ($null -ne $process -and -not $process.HasExited) {
            Stop-Process -Id $process.Id -Force
        }

        foreach ($name in $savedEnv.Keys) {
            Restore-EnvironmentVariable -Name $name -Value $savedEnv[$name]
        }
    }
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$webRoot = Join-Path $repoRoot "src/Codex.Web"
$composeFile = Join-Path $repoRoot "ops/docker-compose.yml"
$envFile = Join-Path $repoRoot ".env"

Push-Location $repoRoot
try {
    Invoke-Step "Build .NET solution" {
        dotnet build Codex.slnx
        Assert-LastExitCode "dotnet build failed."
    }

    Invoke-Step "Install web dependencies" {
        Push-Location $webRoot
        try {
            npm install
            Assert-LastExitCode "npm install failed."
        }
        finally {
            Pop-Location
        }
    }

    Invoke-Step "Build web shell" {
        Push-Location $webRoot
        try {
            npm run build
            Assert-LastExitCode "npm run build failed."
        }
        finally {
            Pop-Location
        }
    }

    Invoke-Step "Validate Docker Compose config" {
        if (-not (Test-Path $envFile)) {
            throw "Expected repository-root .env for Compose validation."
        }

        docker compose -f $composeFile --env-file $envFile config > $null
        Assert-LastExitCode "docker compose config failed."
    }

    Invoke-Step "Probe API readiness" {
        Test-ApiReadiness -RepoRoot $repoRoot
    }

    Write-Host "Platform readiness validation passed." -ForegroundColor Green
}
finally {
    Pop-Location
}
