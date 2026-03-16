$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$PidFile = Join-Path $ScriptDir ".portforward.pids"

$hadPidFile = Test-Path $PidFile

if ($hadPidFile) {
    $pids = Get-Content $PidFile | Where-Object { $_ -match "^[0-9]+$" }

    foreach ($pidText in $pids) {
        try {
            Stop-Process -Id ([int]$pidText) -Force -ErrorAction Stop
            Write-Host "Stopped process with PID $pidText."
        }
        catch {
            # Ignore missing processes.
            Write-Host "Process with PID $pidText not found, skipping."
        }
    }
}

foreach ($port in @(14000, 13000, 12333)) {
    $listeners = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue
    foreach ($listener in $listeners) {
        $ownerPid = $listener.OwningProcess
        try {
            $processInfo = Get-Process -Id $ownerPid -ErrorAction Stop
            if ($processInfo.ProcessName -eq "kubectl") {
                Stop-Process -Id $ownerPid -Force -ErrorAction Stop
                Write-Host "Stopped stale TiDB port-forward process on port $port (PID $ownerPid)."
            }
        }
        catch {
            # Ignore failures during fallback cleanup.
        }
    }
}

if ($hadPidFile -and (Test-Path $PidFile)) {
    Remove-Item $PidFile -Force
}
