# Project guard: every source file must be listed in docs/KOD-HARITASI.md
# and must stay within the size limit (warn > 200 lines, fail > 300 lines).
# Usage: powershell -ExecutionPolicy Bypass -File tools\check.ps1
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$map = Get-Content (Join-Path $root 'docs\KOD-HARITASI.md') -Raw -Encoding UTF8
$failed = $false

$files = Get-ChildItem $root -Recurse -File -Include *.cs, *.xaml |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }

foreach ($f in $files) {
    $rel = $f.FullName.Substring($root.Length + 1).Replace('\', '/')
    $lines = (Get-Content $f.FullName -Encoding UTF8 | Measure-Object -Line).Lines

    if (-not $map.Contains($rel)) {
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

if ($failed) { Write-Host 'check: FAILED' -ForegroundColor Red; exit 1 }
Write-Host "check: OK ($($files.Count) files)" -ForegroundColor Green
