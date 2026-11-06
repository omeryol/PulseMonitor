# Repository hygiene guard: the repository is public, so only files needed to build, install,
# license and present the program may be tracked. Fails on unknown top-level paths, working notes,
# temporary/diagnostic leftovers, binaries and oversized files.
# Usage: powershell -ExecutionPolicy Bypass -File tools\check_tracked.ps1 [-Files a,b,c]
param([string[]]$Files)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
if (-not $Files) {
    if (-not (Test-Path (Join-Path $root '.git'))) { Write-Host 'not a git checkout: tracked-file check skipped' -ForegroundColor Yellow; exit 0 }
    $Files = @(git -C $root ls-files)
    if ($LASTEXITCODE -ne 0) { Write-Host 'git ls-files failed' -ForegroundColor Red; exit 1 }
}

# Adding a new top-level folder or file is a decision for the owner, not for an agent.
$allowedTop = @('.github', '.gitignore', 'Alerts', 'App.xaml', 'App.xaml.cs', 'Assets', 'Core', 'Formatting',
    'Interop', 'LICENSE', 'Metrics', 'Platform', 'PulseMonitor.csproj', 'PulseMonitor.slnx', 'README.md',
    'README.tr.md', 'Resources', 'Settings', 'THIRD-PARTY-NOTICES.md', 'Taskbar', 'Traffic', 'Tray',
    'ViewModels', 'Views', 'Weather', 'app.manifest', 'docs', 'installer', 'lang', 'release-notes', 'tests', 'tools')
# Working notes, agent rules, drafts and leftovers that must stay local.
$forbidden = @('(^|/)(AGENTS|GEMINI|CLAUDE|CHANGELOG|KOD-HARITASI)\.md$', '(^|/)private-words\.txt$',
    '\.(log|tmp|bak|orig|rej|zip|7z|exe|dll|sys|pdb|user|suo)$', '(^|/)(scratch|probe|diagnostic|temp|tmp)([-_][^/]*)?/',
    '(^|/|[-_.])(draft|taslak|deneme|yedek|kopya)([-_.]|$)', ' - (copy|kopya)\.', '(^|/)\.env', '(^|/)settings\.json$', '\.dat$')
$maxBytes = 2MB
$failed = $false
foreach ($f in $Files) {
    $top = ($f -split '/')[0]
    if ($allowedTop -notcontains $top) { Write-Host "UNEXPECTED TOP-LEVEL PATH: $f" -ForegroundColor Red; $failed = $true; continue }
    foreach ($p in $forbidden) {
        if ($f -match $p) { Write-Host "MUST NOT BE TRACKED: $f" -ForegroundColor Red; $failed = $true; break }
    }
    $full = Join-Path $root $f
    if ((Test-Path $full) -and (Get-Item $full).Length -gt $maxBytes) { Write-Host "FILE TOO LARGE (> 2 MB): $f" -ForegroundColor Red; $failed = $true }
}
if ($failed) { exit 1 }
Write-Host "tracked files: OK ($($Files.Count))" -ForegroundColor Green
