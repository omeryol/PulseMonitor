# Builds release files into artifacts\: PulseMonitor-Setup-<ver>.exe (Inno Setup) and PulseMonitor-<ver>-portable.zip.
# The app is framework-dependent (needs the .NET 10 Desktop Runtime; the installer fetches it when missing).
# Used locally and by .github/workflows/release.yml.
# Usage: powershell -ExecutionPolicy Bypass -File tools\build_release.ps1 [-Version 0.7.1]
param([string]$Version = '')
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.Encoding]::UTF8
$root = Split-Path -Parent $PSScriptRoot
$artifacts = Join-Path $root 'artifacts'
$app = Join-Path $artifacts 'app'

if (-not $Version) { $Version = ([xml](Get-Content (Join-Path $root 'PulseMonitor.csproj'))).Project.PropertyGroup.Version | Select-Object -First 1 }
Write-Host "PulseMonitor $Version"

& powershell -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'check.ps1')
if ($LASTEXITCODE -ne 0) { exit 1 }

if (Test-Path $artifacts) { Remove-Item $artifacts -Recurse -Force }
dotnet publish (Join-Path $root 'PulseMonitor.csproj') -c Release -r win-x64 --self-contained false `
  -p:PublishSingleFile=true -p:DebugType=none -p:Version=$Version -o $app --nologo -v q
if ($LASTEXITCODE -ne 0) { Write-Host 'build FAILED' -ForegroundColor Red; exit 1 }
Copy-Item (Join-Path $root 'LICENSE'), (Join-Path $root 'THIRD-PARTY-NOTICES.md') $app

$zip = Join-Path $artifacts "PulseMonitor-$Version-portable.zip"
Compress-Archive -Path (Join-Path $app '*') -DestinationPath $zip

$iscc = @((Get-Command iscc -ErrorAction SilentlyContinue).Source,
          "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
          "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
          "$env:ProgramFiles\Inno Setup 6\ISCC.exe") | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
if (-not $iscc) { Write-Host 'Inno Setup (ISCC.exe) not found' -ForegroundColor Red; exit 1 }
& $iscc /Q "/DAppVersion=$Version" "/DSourceDir=$app" (Join-Path $root 'installer\PulseMonitor.iss')
if ($LASTEXITCODE -ne 0) { Write-Host 'installer FAILED' -ForegroundColor Red; exit 1 }

Get-ChildItem $artifacts -File | ForEach-Object { '{0}  {1:N1} MB' -f $_.Name, ($_.Length / 1MB) }
Write-Host 'release: OK' -ForegroundColor Green
