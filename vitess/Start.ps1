$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$PidFile = Join-Path $ScriptDir ".portforward.pids"

& (Join-Path $ScriptDir "Stop.ps1")

# Port-forward the vtgate service (MySQL protocol) in the 'example' namespace.
# Assumes the Vitess operator and initial cluster are already deployed.
$vtgateService = kubectl get service -n example --selector "planetscale.com/component=vtgate" -o name |
    Select-Object -First 1

if (-not $vtgateService) {
    throw "No vtgate service found in namespace 'example'. Is the Vitess cluster deployed?"
}

$pf1 = Start-Process -FilePath "kubectl" -ArgumentList @(
    "port-forward", "-n", "example", "--address=0.0.0.0", $vtgateService, "15306:3306"
) -PassThru -NoNewWindow `
  -RedirectStandardOutput (Join-Path $ScriptDir "pf15306.out") `
  -RedirectStandardError  (Join-Path $ScriptDir "pf15306.err")

Start-Sleep -Seconds 1

if ($pf1.HasExited) {
    throw "Vitess vtgate port-forward process exited immediately. Check pf15306.err in $ScriptDir"
}

@($pf1.Id) | Set-Content -Path $PidFile -Encoding ascii
