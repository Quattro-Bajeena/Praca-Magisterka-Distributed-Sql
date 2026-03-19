$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$PidFile = Join-Path $ScriptDir ".portforward.pids"

& (Join-Path $ScriptDir "Stop.ps1")

# Port-forward PolarDB-X service to local port 56750.
# Assumes the quick-start XStore/PolarDBXCluster is already deployed without a namespace (default).
$pf1 = Start-Process -FilePath "kubectl" -ArgumentList @(
    "port-forward", "--address=0.0.0.0", "svc/quick-start", "56750:3306"
) -PassThru -NoNewWindow `
  -RedirectStandardOutput (Join-Path $ScriptDir "pf56750.out") `
  -RedirectStandardError  (Join-Path $ScriptDir "pf56750.err")

Start-Sleep -Seconds 1

if ($pf1.HasExited) {
    throw "PolarDB-X port-forward process exited immediately. Check pf56750.err in $ScriptDir"
}

@($pf1.Id) | Set-Content -Path $PidFile -Encoding ascii
