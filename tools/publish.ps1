# Builds Release and installs PulseMonitor to %LOCALAPPDATA%\Programs\PulseMonitor, then starts it.
# The running copy is closed gracefully (--exit) so the traffic history is saved first.
# Usage: powershell -ExecutionPolicy Bypass -File tools\publish.ps1
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.Encoding]::UTF8
$root = Split-Path -Parent $PSScriptRoot
$dest = Join-Path $env:LOCALAPPDATA 'Programs\PulseMonitor'
$staging = Join-Path $env:TEMP 'PulseMonitor-publish'

& powershell -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'check.ps1')
if ($LASTEXITCODE -ne 0) { exit 1 }

if (Test-Path $staging) { Remove-Item $staging -Recurse -Force }
dotnet publish (Join-Path $root 'PulseMonitor.csproj') -c Release -o $staging --nologo -v q
if ($LASTEXITCODE -ne 0) { Write-Host 'publish: build FAILED' -ForegroundColor Red; exit 1 }

# Close the running copy (any install location) gracefully, force only as a last resort.
$running = Get-Process PulseMonitor -ErrorAction SilentlyContinue
if ($running) {
    Start-Process (Join-Path $staging 'PulseMonitor.exe') -ArgumentList '--exit' -Wait
    foreach ($p in $running) {
        if (-not $p.WaitForExit(5000)) { Write-Host 'Graceful exit timed out, stopping process.'; $p | Stop-Process -Force }
    }
}

New-Item -ItemType Directory -Force $dest | Out-Null
robocopy $staging $dest /MIR /NJH /NJS /NFL /NDL /NP | Out-Null
if ($LASTEXITCODE -ge 8) { Write-Host "publish: copy FAILED ($LASTEXITCODE)" -ForegroundColor Red; exit 1 }
Remove-Item $staging -Recurse -Force

$exe = Join-Path $dest 'PulseMonitor.exe'
Start-Process $exe
Write-Host "publish: OK -> $exe" -ForegroundColor Green

# Startup entry points to another copy (e.g. a Debug build)? Point it to the installed one.
$run = (Get-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -ErrorAction SilentlyContinue).PulseMonitor
if ($run -and $run -ne "`"$exe`"") {
    Set-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name PulseMonitor -Value "`"$exe`""
    Write-Host "Startup entry updated: $run -> `"$exe`""
}
