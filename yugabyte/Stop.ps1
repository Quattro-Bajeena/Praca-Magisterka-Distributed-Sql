$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$PidFile = Join-Path $ScriptDir ".portforward.pids"

if (Test-Path $PidFile) {
    foreach ($forwardPid in (Get-Content $PidFile)) {
        try { Stop-Process -Id $forwardPid -Force -ErrorAction SilentlyContinue } catch {}
    }
    Remove-Item $PidFile -Force
}

# Kill any remaining kubectl port-forward processes on port 5433
Get-NetTCPConnection -LocalPort 5433 -ErrorAction SilentlyContinue |
    Where-Object { $_.OwningProcess -ne 0 } |
    ForEach-Object {
        $proc = Get-Process -Id $_.OwningProcess -ErrorAction SilentlyContinue
        if ($proc -and $proc.ProcessName -eq 'kubectl') {
            Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
        }
    }
