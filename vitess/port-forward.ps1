$vtctldService = kubectl get service -n example --selector "planetscale.com/component=vtctld" -o name | Select-Object -First 1
$vtgateService = kubectl get service -n example --selector "planetscale.com/component=vtgate" -o name | Select-Object -First 1
# $vtgateService = kubectl get service -n example --selector "planetscale.com/component=vtgate,!planetscale.com/cell" -o name | Select-Object -First 1
$vtadminService = kubectl get service -n example --selector "planetscale.com/component=vtadmin" -o name | Select-Object -First 1

$process1 = Start-Process -FilePath "kubectl" -ArgumentList @(
	"port-forward", "-n", "example", "--address", "0.0.0.0", $vtctldService, "15000:15999"
) -PassThru -NoNewWindow

$process2 = Start-Process -FilePath "kubectl" -ArgumentList @(
	"port-forward", "-n", "example", "--address", "0.0.0.0", $vtgateService, "15306:3306"
) -PassThru -NoNewWindow

$process3 = Start-Process -FilePath "kubectl" -ArgumentList @(
	"port-forward", "-n", "example", "--address", "0.0.0.0", $vtadminService, "14000:15000", "14001:15001"
) -PassThru -NoNewWindow

Start-Sleep -Seconds 2

Wait-Process -Id $process1.Id, $process2.Id, $process3.Id