$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

docker compose -f (Join-Path $ScriptDir "compose.yml") down -v
