param(
    [switch]$Start,
    [switch]$Check,
    [switch]$Build,
    [switch]$Run,
    [switch]$Stop
)

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

$ErrorActionPreference = "Stop"

# Manual tuning variables (edit directly in this file).
$HealthMaxRetries = 20
$HealthConnectTimeoutSeconds = 2
$HealthRetryDelaySeconds = 3
$BuildConfiguration = "Release"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Split-Path -Parent $ScriptDir
$NsciProjectPath = Join-Path $RepoRoot "DatabaseCompatibilityIndex\NewSqlCompatibility\NSCI.csproj"

if (-not ($Start -or $Check -or $Build -or $Run -or $Stop)) {
    $Start = $true
    $Check = $true
    $Build = $true
    $Run = $true
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
        $typedHealth.HostName = [string]$rawInstance.healthCheck.PSObject.Properties["host"].Value
        $typedHealth.Port = [int]$rawInstance.healthCheck.PSObject.Properties["port"].Value
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

    $entries = @()
    $dirs = Get-ChildItem -Path $Root -Directory

    foreach ($dir in $dirs) {
        $configPath = Join-Path $dir.FullName "db.config.json"
        $startPath = Join-Path $dir.FullName "Start.ps1"
        $stopPath = Join-Path $dir.FullName "Stop.ps1"

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

    if ($Config.startupType -ne "docker-compose" -and $Config.startupType -ne "kubernetes") {
        throw "Invalid startupType '$($Config.startupType)' in $ConfigPath"
    }

    if ($Config.databaseType -ne "PostgreSql" -and $Config.databaseType -ne "MySql") {
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

function Assert-Health {
    param(
        [DatabaseEntry]$DbEntry
    )

    if (-not $DbEntry.Config.enabled) {
        Write-Host "[CHECK] Skipping disabled database '$($DbEntry.Name)'."
        return
    }

    $instances = @($DbEntry.Config.Instances | Where-Object { $_.Enabled -eq $true })

    foreach ($instance in $instances) {
        $endpointAddress = $instance.HealthCheck.HostName
        $endpointPort = $instance.HealthCheck.Port
        $endpointText = $endpointAddress + ":" + $endpointPort
        $ok = $false

        Write-Host ("[CHECK] Waiting for {0} / {1} at {2}" -f $DbEntry.Name, $instance.DisplayName, $endpointText)

        for ($attempt = 1; $attempt -le $HealthMaxRetries; $attempt++) {
            if (Test-TcpEndpoint -EndpointAddress $endpointAddress -Port $endpointPort -ConnectTimeoutSeconds $HealthConnectTimeoutSeconds) {
                $ok = $true
                break
            }

            Write-Host ("[CHECK] Retry {0}/{1} failed for {2} / {3} at {4}" -f $attempt, $HealthMaxRetries, $DbEntry.Name, $instance.DisplayName, $endpointText)

            if ($attempt -lt $HealthMaxRetries) {
                Start-Sleep -Seconds $HealthRetryDelaySeconds
            }
        }

        if (-not $ok) {
            throw ("Health check failed for '{0}' instance '{1}' at {2}." -f $DbEntry.Name, $instance.DisplayName, $endpointText)
        }

        Write-Host ("[CHECK] OK: {0} / {1} at {2}" -f $DbEntry.Name, $instance.DisplayName, $endpointText)
    }
}

function Invoke-DatabaseScript {
    param(
        [DatabaseEntry]$DbEntry,
        [ValidateSet("start", "stop")]
        [string]$Action
    )

    if ($Action -eq "start") {
        if (-not $DbEntry.Config.enabled) {
            Write-Host "[START] Skipping disabled database '$($DbEntry.Name)'."
            return
        }

        Write-Host "[START] $($DbEntry.Name)"
        & $DbEntry.StartScript
        if ($LASTEXITCODE -ne 0) {
            throw "Start script failed for '$($DbEntry.Name)'."
        }
        return
    }

    Write-Host "[STOP] $($DbEntry.Name)"
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

    Write-Host "[BUILD] dotnet restore"
    & dotnet restore $ProjectPath
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet restore failed"
    }

    Write-Host "[BUILD] dotnet build ($Configuration)"
    & dotnet build $ProjectPath --configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed"
    }
}

function Invoke-NsciRun {
    param(
        [string]$ProjectPath,
        [string]$Configuration,
        [string]$DatabaseRoot
    )

    if (-not (Test-Path $ProjectPath)) {
        throw "NSCI project not found at $ProjectPath"
    }

    $absoluteDbRoot = [System.IO.Path]::GetFullPath($DatabaseRoot)
    Write-Host "[RUN] dotnet run with --db-root $absoluteDbRoot"

    & dotnet run --project $ProjectPath --configuration $Configuration -- --db-root $absoluteDbRoot
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet run failed"
    }
}

$databaseEntries = Get-DatabaseEntries -Root $RepoRoot

foreach ($entry in $databaseEntries) {
    Assert-DatabaseConfig -Config $entry.Config -ConfigPath $entry.ConfigPath
}

if ($Start) {
    if ($Check) {
        foreach ($entry in $databaseEntries) {
            Invoke-DatabaseScript -DbEntry $entry -Action "start"
            Assert-Health -DbEntry $entry
        }
    }
    else {
        foreach ($entry in $databaseEntries) {
            Invoke-DatabaseScript -DbEntry $entry -Action "start"
        }
    }
}
elseif ($Check) {
    foreach ($entry in $databaseEntries) {
        Assert-Health -DbEntry $entry
    }
}

if ($Build) {
    Invoke-NsciBuild -ProjectPath $NsciProjectPath -Configuration $BuildConfiguration
}

if ($Run) {
    Invoke-NsciRun -ProjectPath $NsciProjectPath -Configuration $BuildConfiguration -DatabaseRoot $RepoRoot
}

if ($Stop) {
    foreach ($entry in $databaseEntries) {
        Invoke-DatabaseScript -DbEntry $entry -Action "stop"
    }
}

Write-Host "Done."
