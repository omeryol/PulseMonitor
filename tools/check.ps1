# Project guard: every source file must be listed in docs/KOD-HARITASI.md
# and must stay within the size limit (warn > 200 lines, fail > 300 lines).
# The map is a local, git-ignored file; when it is absent (CI, fresh clone) the map check is skipped.
# Usage: powershell -ExecutionPolicy Bypass -File tools\check.ps1
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$mapFile = Join-Path $root 'docs\KOD-HARITASI.md'
$map = if (Test-Path $mapFile) { Get-Content $mapFile -Raw -Encoding UTF8 } else { $null }
if (-not $map) { Write-Host 'map not found: map check skipped' -ForegroundColor Yellow }
$failed = $false

$files = Get-ChildItem $root -Recurse -File -Include *.cs, *.xaml |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }

foreach ($f in $files) {
    $rel = $f.FullName.Substring($root.Length + 1).Replace('\', '/')
    $lines = (Get-Content $f.FullName -Encoding UTF8 | Measure-Object -Line).Lines

    if ($map -and -not $map.Contains($rel)) {
        Write-Host "MISSING IN MAP: $rel" -ForegroundColor Red
        $failed = $true
    }
    if ($lines -gt 300) {
        Write-Host "TOO LONG ($lines lines, limit 300): $rel" -ForegroundColor Red
        $failed = $true
    } elseif ($lines -gt 200) {
        Write-Host "WARNING ($lines lines, target 200): $rel" -ForegroundColor Yellow
    }
}

# Personal data guard: the repository may be published, so these must never appear in tracked text files.
# Patterns are kept generic (paths, private IP ranges, private links); add personal words to tools/private-words.txt
# (that file is git-ignored and never published).
$patterns = @('C:\\Users\\', '\b192\.168\.\d+\.\d+\b', '\b10\.\d+\.\d+\.\d+\b', 'claude\.ai/artifact/', '@gmail\.com')
$wordsFile = Join-Path $PSScriptRoot 'private-words.txt'
if (Test-Path $wordsFile) { $patterns += Get-Content $wordsFile -Encoding UTF8 | Where-Object { $_.Trim() } | ForEach-Object { [regex]::Escape($_.Trim()) } }
$textFiles = Get-ChildItem $root -Recurse -File -Include *.cs, *.xaml, *.md, *.ps1, *.json, *.csproj, *.manifest |
    Where-Object { $_.FullName -notmatch '\\(bin|obj|\.git)\\' -and $_.Name -ne 'check.ps1' -and $_.Name -ne 'private-words.txt' }
foreach ($f in $textFiles) {
    $hit = Select-String -Path $f.FullName -Pattern $patterns -Encoding UTF8 | Select-Object -First 1
    if ($hit) {
        Write-Host "PERSONAL DATA? $($f.FullName.Substring($root.Length + 1)):$($hit.LineNumber)" -ForegroundColor Red
        $failed = $true
    }
}

# Language guard: every lang/*.json must be valid JSON with string values, and use only keys that exist in tr.json
# (the source language). Missing keys are allowed (English, then Turkish is shown) but reported.
$langDir = Join-Path $root 'lang'
$keysOf = { param($path) (Get-Content $path -Raw -Encoding UTF8 | ConvertFrom-Json).PSObject.Properties | Where-Object { $_.Name -notlike '_*' } }
$baseKeys = @(& $keysOf (Join-Path $langDir 'tr.json') | ForEach-Object Name)
foreach ($f in Get-ChildItem $langDir -Filter *.json) {
    try { $props = @(& $keysOf $f.FullName) } catch { Write-Host "INVALID JSON: lang/$($f.Name)" -ForegroundColor Red; $failed = $true; continue }
    $extra = @($props | Where-Object { $baseKeys -notcontains $_.Name } | ForEach-Object Name)
    $notText = @($props | Where-Object { $_.Value -isnot [string] } | ForEach-Object Name)
    $missing = @($baseKeys | Where-Object { ($props | ForEach-Object Name) -notcontains $_ })
    if ($extra) { Write-Host "UNKNOWN KEYS in lang/$($f.Name): $($extra -join ', ')" -ForegroundColor Red; $failed = $true }
    if ($notText) { Write-Host "NON-TEXT VALUES in lang/$($f.Name): $($notText -join ', ')" -ForegroundColor Red; $failed = $true }
    if ($missing) { Write-Host "lang/$($f.Name): $($missing.Count) untranslated ($(($missing | Select-Object -First 5) -join ', ')...)" -ForegroundColor Yellow }
}

if ($failed) { Write-Host 'check: FAILED' -ForegroundColor Red; exit 1 }
Write-Host "check: OK ($($files.Count) files)" -ForegroundColor Green
