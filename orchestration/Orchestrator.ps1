[CmdletBinding()]
param(
    [switch]$Start,
    [switch]$Check,
    [switch]$Build,
    [switch]$Run,
    [switch]$Stop
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# -----------------------
# Configuration (tweak as needed)
# -----------------------
$HealthMaxRetries = 20
$HealthConnectTimeoutSeconds = 2
$HealthRetryDelaySeconds = 3
$BuildConfiguration = 'Release'

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Split-Path -Parent $ScriptDir
$NsciProjectPath = Join-Path $RepoRoot 'DatabaseCompatibilityIndex\NewSqlCompatibility\NSCI.csproj'
$NsciConfigPath = Join-Path $RepoRoot 'DatabaseCompatibilityIndex\NewSqlCompatibility\appsettings.json'

# Default behavior: start, check, build, run if no switches are provided.
if (-not ($Start -or $Check -or $Build -or $Run -or $Stop)) {
    $Start = $true
    $Check = $true
    $Build = $true
    $Run = $true
}

function Write-Info {
    param([string]$Message)
    Write-Host "[INFO] $Message"
}

function Write-Warn {
    param([string]$Message)
    Write-Host "[WARN] $Message" -ForegroundColor Yellow
}

function Write-ErrorText {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

class HealthCheckConfig {
    [string]$HostName
    [int]$Port
}

class DatabaseInstanceConfig {
    [string]$Id
    [string]$DisplayName
    [string]$Version
    [bool]$Enabled
    [string]$ConnectionString
    [HealthCheckConfig]$HealthCheck
}

class DatabaseConfig {
    [string]$Name
    [string]$StartupType
    [string]$DatabaseType
    [bool]$Enabled
    [DatabaseInstanceConfig[]]$Instances
}

class DatabaseEntry {
    [string]$Name
    [string]$Folder
    [string]$ConfigPath
    [string]$StartScript
    [string]$StopScript
    [DatabaseConfig]$Config
}

function ConvertTo-DatabaseConfig {
    param(
        [string]$ConfigPath
    )

    $raw = Get-Content -Path $ConfigPath -Raw | ConvertFrom-Json

    $dbConfig = [DatabaseConfig]::new()
    $dbConfig.Name = [string]$raw.name
    $dbConfig.StartupType = [string]$raw.startupType
    $dbConfig.DatabaseType = [string]$raw.databaseType
    $dbConfig.Enabled = [bool]$raw.enabled

    $typedInstances = @()
    foreach ($rawInstance in @($raw.instances)) {
        $typedInstance = [DatabaseInstanceConfig]::new()
        $typedInstance.Id = [string]$rawInstance.id
        $typedInstance.DisplayName = [string]$rawInstance.displayName
        $typedInstance.Version = [string]$rawInstance.version
        $typedInstance.Enabled = [bool]$rawInstance.enabled
        $typedInstance.ConnectionString = [string]$rawInstance.connectionString

        $typedHealth = [HealthCheckConfig]::new()
        $typedHealth.HostName = [string]$rawInstance.healthCheck.PSObject.Properties['host'].Value
        $typedHealth.Port = [int]$rawInstance.healthCheck.PSObject.Properties['port'].Value
        $typedInstance.HealthCheck = $typedHealth

        $typedInstances += $typedInstance
    }

    $dbConfig.Instances = $typedInstances
    return $dbConfig
}

function Get-DatabaseEntries {
    param(
        [string]$Root
    )

    if (-not (Test-Path -Path $Root -PathType Container)) {
        throw "Database root folder not found: $Root"
    }

    $entries = @()
    $dirs = Get-ChildItem -Path $Root -Directory

    foreach ($dir in $dirs) {
        $configPath = Join-Path $dir.FullName 'db.config.json'
        $startPath = Join-Path $dir.FullName 'Start.ps1'
        $stopPath = Join-Path $dir.FullName 'Stop.ps1'

        if ((Test-Path $configPath) -and (Test-Path $startPath) -and (Test-Path $stopPath)) {
            $config = ConvertTo-DatabaseConfig -ConfigPath $configPath
            $entry = [DatabaseEntry]::new()
            $entry.Name = $config.Name
            $entry.Folder = $dir.FullName
            $entry.ConfigPath = $configPath
            $entry.StartScript = $startPath
            $entry.StopScript = $stopPath
            $entry.Config = $config
            $entries += $entry
        }
    }

    return $entries
}

function Assert-DatabaseConfig {
    param(
        [DatabaseConfig]$Config,
        [string]$ConfigPath
    )

    if (-not $Config.name) {
        throw "Missing required field 'name' in $ConfigPath"
    }

    if (-not $Config.startupType) {
        throw "Missing required field 'startupType' in $ConfigPath"
    }

    if (-not $Config.databaseType) {
        throw "Missing required field 'databaseType' in $ConfigPath"
    }

    if ($Config.startupType -ne 'docker-compose' -and $Config.startupType -ne 'kubernetes') {
        throw "Invalid startupType '$($Config.startupType)' in $ConfigPath"
    }

    if ($Config.databaseType -ne 'PostgreSql' -and $Config.databaseType -ne 'MySql') {
        throw "Invalid databaseType '$($Config.databaseType)' in $ConfigPath"
    }

    if ($null -eq $Config.enabled) {
        throw "Missing required field 'enabled' in $ConfigPath"
    }

    if ($null -eq $Config.instances -or $Config.instances.Count -eq 0) {
        throw "Field 'instances' must contain at least one instance in $ConfigPath"
    }

    foreach ($instance in $Config.instances) {
        if (-not $instance.Id) {
            throw "Missing required instance field 'id' in $ConfigPath"
        }

        if (-not $instance.DisplayName) {
            throw "Missing required instance field 'displayName' for instance '$($instance.Id)' in $ConfigPath"
        }

        if (-not $instance.Version) {
            throw "Missing required instance field 'version' for instance '$($instance.Id)' in $ConfigPath"
        }

        if ($null -eq $instance.Enabled) {
            throw "Missing required instance field 'enabled' for instance '$($instance.Id)' in $ConfigPath"
        }

        if (-not $instance.ConnectionString) {
            throw "Missing required instance field 'connectionString' for instance '$($instance.Id)' in $ConfigPath"
        }

        if ($null -eq $instance.HealthCheck) {
            throw "Missing required instance field 'healthCheck' for instance '$($instance.Id)' in $ConfigPath"
        }

        $instanceHost = $instance.HealthCheck.HostName
        $instancePort = $instance.HealthCheck.Port

        if (-not $instanceHost) {
            throw "Missing required healthCheck field 'host' for instance '$($instance.Id)' in $ConfigPath"
        }

        if ($null -eq $instancePort) {
            throw "Missing required healthCheck field 'port' for instance '$($instance.Id)' in $ConfigPath"
        }
    }
}

function Test-TcpEndpoint {
    param(
        [string]$EndpointAddress,
        [int]$Port,
        [int]$ConnectTimeoutSeconds
    )

    try {
        $client = New-Object System.Net.Sockets.TcpClient
        $iar = $client.BeginConnect($EndpointAddress, $Port, $null, $null)
        $ok = $iar.AsyncWaitHandle.WaitOne([TimeSpan]::FromSeconds($ConnectTimeoutSeconds))

        if (-not $ok) {
            $client.Close()
            return $false
        }

        $client.EndConnect($iar) | Out-Null
        $client.Close()
        return $true
    }
    catch {
        return $false
    }
}

function Wait-ForHealth {
    param(
        [DatabaseEntry]$DbEntry
    )

    if (-not $DbEntry.Config.enabled) {
        Write-Info "Skipping disabled database '$($DbEntry.Name)'."
        return
    }

    $instances = $DbEntry.Config.Instances | Where-Object { $_.Enabled }

    foreach ($instance in $instances) {
        $endpointAddress = $instance.HealthCheck.HostName
        $endpointPort = $instance.HealthCheck.Port
        $endpointText = "${endpointAddress}:${endpointPort}"

        Write-Info "Waiting for '$($DbEntry.Name)' instance '$($instance.DisplayName)' at $endpointText"

        $ok = $false
        for ($attempt = 1; $attempt -le $HealthMaxRetries; $attempt++) {
            if (Test-TcpEndpoint -EndpointAddress $endpointAddress -Port $endpointPort -ConnectTimeoutSeconds $HealthConnectTimeoutSeconds) {
                $ok = $true
                break
            }

            Write-Info "Retry $attempt/$HealthMaxRetries failed for '$($DbEntry.Name)' / '$($instance.DisplayName)'"

            if ($attempt -lt $HealthMaxRetries) {
                Start-Sleep -Seconds $HealthRetryDelaySeconds
            }
        }

        if (-not $ok) {
            throw "Health check failed for '$($DbEntry.Name)' instance '$($instance.DisplayName)' at $endpointText"
        }

        Write-Info "Health check OK for '$($DbEntry.Name)' / '$($instance.DisplayName)'"
    }
}

function Invoke-DatabaseAction {
    param(
        [DatabaseEntry]$DbEntry,
        [ValidateSet('start', 'stop')]
        [string]$Action
    )

    if ($Action -eq 'start') {
        if (-not $DbEntry.Config.enabled) {
            Write-Info "Skipping disabled database '$($DbEntry.Name)'."
            return
        }

        Write-Info "Starting '$($DbEntry.Name)'"
        & $DbEntry.StartScript
        if ($LASTEXITCODE -ne 0) {
            throw "Start script failed for '$($DbEntry.Name)'."
        }
        return
    }

    Write-Info "Stopping '$($DbEntry.Name)'"
    & $DbEntry.StopScript
    if ($LASTEXITCODE -ne 0) {
        throw "Stop script failed for '$($DbEntry.Name)'."
    }
}

function Invoke-NsciBuild {
    param(
        [string]$ProjectPath,
        [string]$Configuration
    )

    if (-not (Test-Path $ProjectPath)) {
        throw "NSCI project not found at $ProjectPath"
    }

    Write-Info "Running dotnet restore"
    & dotnet restore $ProjectPath
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet restore failed"
    }

    Write-Info "Running dotnet build ($Configuration)"
    & dotnet build $ProjectPath --configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed"
    }
}

function Invoke-NsciRun {
    param(
        [string]$ProjectPath,
        [string]$Configuration,
        [string]$ConfigPath,
        [string]$DatabaseRoot
    )

    if (-not (Test-Path $ProjectPath)) {
        throw "NSCI project not found at $ProjectPath"
    }

    if (-not (Test-Path $ConfigPath)) {
        throw "NSCI configuration not found at $ConfigPath"
    }

    $absoluteDbRoot = [System.IO.Path]::GetFullPath($DatabaseRoot)
    $absoluteConfigPath = [System.IO.Path]::GetFullPath($ConfigPath)

    Write-Info "Running NSCI with config '$absoluteConfigPath' and db root '$absoluteDbRoot'"
    & dotnet run --project $ProjectPath --configuration $Configuration -- --config $absoluteConfigPath --db-root $absoluteDbRoot
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet run failed"
    }
}

try {
    $databaseEntries = Get-DatabaseEntries -Root $RepoRoot

    foreach ($entry in $databaseEntries) {
        Assert-DatabaseConfig -Config $entry.Config -ConfigPath $entry.ConfigPath
    }

    if ($Start) {
        foreach ($entry in $databaseEntries) {
            Invoke-DatabaseAction -DbEntry $entry -Action 'start'
            if ($Check) {
                Wait-ForHealth -DbEntry $entry
            }
        }
    }
    elseif ($Check) {
        foreach ($entry in $databaseEntries) {
            Wait-ForHealth -DbEntry $entry
        }
    }

    if ($Build) {
        Invoke-NsciBuild -ProjectPath $NsciProjectPath -Configuration $BuildConfiguration
    }

    if ($Run) {
        Invoke-NsciRun -ProjectPath $NsciProjectPath -Configuration $BuildConfiguration -ConfigPath $NsciConfigPath -DatabaseRoot $RepoRoot
    }

    if ($Stop) {
        foreach ($entry in $databaseEntries) {
            Invoke-DatabaseAction -DbEntry $entry -Action 'stop'
        }
    }

    Write-Host 'Done.'
}
catch {
    Write-ErrorText $_.Exception.Message
    Write-ErrorText $_.Exception.StackTrace
    exit 1
}
