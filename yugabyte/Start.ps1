$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$PidFile = Join-Path $ScriptDir ".portforward.pids"

& (Join-Path $ScriptDir "Stop.ps1")

# Port-forward the YugabyteDB YSQL (PostgreSQL-compatible) port in the 'yb' namespace.
# Assumes the YugabyteDB cluster is already deployed via Helm.
$pf1 = Start-Process -FilePath "kubectl" -ArgumentList @(
    "port-forward", "-n", "yb", "--address=0.0.0.0", "svc/yb-tservers", "5433:5433"
) -PassThru -NoNewWindow `
  -RedirectStandardOutput (Join-Path $ScriptDir "pf5433.out") `
  -RedirectStandardError  (Join-Path $ScriptDir "pf5433.err")

Start-Sleep -Seconds 1

if ($pf1.HasExited) {
    throw "YugabyteDB port-forward process exited immediately. Check pf5433.err in $ScriptDir"
}

@($pf1.Id) | Set-Content -Path $PidFile -Encoding ascii
