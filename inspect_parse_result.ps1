$asm = [Reflection.Assembly]::LoadFrom("$env:USERPROFILE\.nuget\packages\system.commandline\2.0.5\lib\net8.0\System.CommandLine.dll")

Write-Output "Assembly: $($asm.FullName)"

$type = $asm.GetType('System.CommandLine.Option')
if ($type -eq $null) {
    Write-Error "Failed to locate System.CommandLine.Option type."
    exit 1
}

Write-Output "Option members containing 'Argument':"
$type.GetMembers() | Where-Object { $_.Name -like '*Argument*' } | Select-Object -Unique Name | Sort-Object | ForEach-Object { Write-Output "  $_" }
