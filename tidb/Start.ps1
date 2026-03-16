$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$PidFile = Join-Path $ScriptDir ".portforward.pids"

& (Join-Path $ScriptDir "Stop.ps1")

$pf1 = Start-Process -FilePath "kubectl" -ArgumentList @(
    "port-forward", "--address=0.0.0.0", "-n", "tidb", "service/basic-tidb", "14000:4000"
) -PassThru -NoNewWindow -RedirectStandardOutput (Join-Path $ScriptDir "pf14000.out") -RedirectStandardError (Join-Path $ScriptDir "pf14000.err")

$pf2 = Start-Process -FilePath "kubectl" -ArgumentList @(
    "port-forward", "--address=0.0.0.0", "-n", "tidb", "service/basic-grafana", "13000:3000"
) -PassThru -NoNewWindow -RedirectStandardOutput (Join-Path $ScriptDir "pf3000.out") -RedirectStandardError (Join-Path $ScriptDir "pf3000.err")

$pf3 = Start-Process -FilePath "kubectl" -ArgumentList @(
    "port-forward", "--address=0.0.0.0", "-n", "tidb", "service/basic-tidb-dashboard-exposed", "12333:12333"
) -PassThru -NoNewWindow -RedirectStandardOutput (Join-Path $ScriptDir "pf12333.out") -RedirectStandardError (Join-Path $ScriptDir "pf12333.err")

Start-Sleep -Seconds 1

foreach ($proc in @($pf1, $pf2, $pf3)) {
    if ($proc.HasExited) {
        throw "TiDB port-forward process exited immediately. Check pf*.err files in $ScriptDir"
    }
}

@($pf1.Id, $pf2.Id, $pf3.Id) | Set-Content -Path $PidFile -Encoding ascii
