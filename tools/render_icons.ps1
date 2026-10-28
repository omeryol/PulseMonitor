param([string]$Download, [string]$Project, [string]$Only = '')
# Renders animated weather icon sets (Meteocons, Makin-Things; MIT) to PNG film strips with headless Edge.
# Each icon: <Key>.png = N frames side by side (48x48 each, 10 fps), <Key>.static.png = the static variant.
# Output: Assets/WeatherAnim/<Set>/. Edge runs with a throwaway profile (the user's profile is not touched).
# Usage: powershell -ExecutionPolicy Bypass -File tools\render_icons.ps1 -Download <temp dir> -Project .
$ErrorActionPreference = 'Stop'
$Size = 48; $Fps = 10; $MaxLoop = 6.0
$edge = (Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\msedge.exe').'(default)'
$edgeProfile = Join-Path $Download 'edge-profile'
New-Item -ItemType Directory -Force $Download, $edgeProfile | Out-Null

$meteo = 'https://cdn.jsdelivr.net/npm/@meteocons'
$makin = 'https://raw.githubusercontent.com/Makin-Things/weather-icons/main'
$meteoMap = [ordered]@{ ClearDay='clear-day'; ClearNight='clear-night'; PartlyDay='partly-cloudy-day'; PartlyNight='partly-cloudy-night';
  Cloudy='overcast'; Fog='fog'; Rain='rain'; Thunder='thunderstorms-rain'; Snow='snow' }
$makinMap = [ordered]@{ ClearDay='clear-day'; ClearNight='clear-night'; PartlyDay='cloudy-1-day'; PartlyNight='cloudy-1-night';
  Cloudy='cloudy'; Fog='fog'; Rain='rainy-2'; Thunder='thunderstorms'; Snow='snowy-2' }
$sets = [ordered]@{
  MeteoconsFill = @{ Zoom = 1.15; Map = $meteoMap; Anim = "$meteo/svg@0.1.0/fill/{0}.svg"; Static = "$meteo/svg-static@0.1.0/fill/{0}.svg" }
  MeteoconsFlat = @{ Zoom = 1.15; Map = $meteoMap; Anim = "$meteo/svg@0.1.0/flat/{0}.svg"; Static = "$meteo/svg-static@0.1.0/flat/{0}.svg" }
  MeteoconsLine = @{ Zoom = 1.15; Map = $meteoMap; Anim = "$meteo/svg@0.1.0/line/{0}.svg"; Static = "$meteo/svg-static@0.1.0/line/{0}.svg" }
  MakinThings   = @{ Zoom = 1.05; Map = $makinMap; Anim = "$makin/animated/{0}.svg"; Static = "$makin/static/{0}.svg" }
}

function Get-Svg([string]$url) {
  $text = (& curl.exe -s -f -L $url) -join "`n"
  if ($LASTEXITCODE -ne 0 -or $text -notmatch '<svg') { throw "download failed: $url" }
  $text = $text -replace '<\?xml[^>]*\?>', ''
  # Makin-Things SVGs have width/height (56x48) but no viewBox: add one so the icon scales into the cell
  # instead of being clipped.
  $root = [regex]::Match($text, '<svg\b[^>]*>').Value
  if ($root -and $root -notmatch 'viewBox') {
    $w = [regex]::Match($root, '\swidth="([\d.]+)').Groups[1].Value
    $h = [regex]::Match($root, '\sheight="([\d.]+)').Groups[1].Value
    if ($w -and $h) { $text = $text.Replace($root, $root.Replace('<svg', "<svg viewBox=`"0 0 $w $h`"")) }
  }
  return $text
}

# Loop length: least common multiple of the animation durations (0.5 s steps), else the longest, at most $MaxLoop.
function Get-Loop([string]$svg) {
  $d = [regex]::Matches($svg, '(?:dur="|animation-duration:\s*|animation:[^;]*?)([\d.]+)(ms|s)') |
    ForEach-Object { $v = [double]::Parse($_.Groups[1].Value, [Globalization.CultureInfo]::InvariantCulture); if ($_.Groups[2].Value -eq 'ms') { $v / 1000 } else { $v } } |
    Where-Object { $_ -gt 0 } | Sort-Object -Unique
  if (-not $d) { return 0 }
  $halves = $d | ForEach-Object { [int][math]::Round($_ * 2) } | Where-Object { $_ -gt 0 }
  $l = 1; foreach ($h in $halves) { $a = $l; $b = $h; while ($b) { $t = $b; $b = $a % $b; $a = $t }; $l = $l / $a * $h }
  $loop = $l / 2.0
  if ($loop -gt $MaxLoop) { $loop = [math]::Min($MaxLoop, ($d | Measure-Object -Maximum).Maximum) }
  return $loop
}

# Copy i of the SVG with unique ids, so masks/gradients of one frame do not resolve to another frame.
function Get-Copy([string]$svg, [int]$i) {
  $p = "f$i-"
  $s = [regex]::Replace($svg, '\bid="([^"]+)"', "id=`"$p`$1`"")
  $s = [regex]::Replace($s, 'url\(#([^)]+)\)', "url(#$p`$1)")
  $s = [regex]::Replace($s, '(xlink:href|href)="#([^"]+)"', "`$1=`"#$p`$2`"")
  return $s
}

# $zoom: the sets have wide padding inside their viewBox; scale up so the icon fills the 48 px cell.
function Render([string]$svg, [int]$frames, [string]$out, [double]$zoom) {
  $z = $zoom.ToString([Globalization.CultureInfo]::InvariantCulture)
  $cells = (0..($frames - 1) | ForEach-Object { "<div class='c' data-t='$($_ / $Fps)'>$(Get-Copy $svg $_)</div>" }) -join "`n"
  $html = @"
<!doctype html><html><head><meta charset="utf-8"><style>
html,body{margin:0;padding:0;background:transparent;overflow:hidden}
.row{display:flex;width:$($frames * $Size)px;height:${Size}px}
.c{width:${Size}px;height:${Size}px;flex:none;overflow:hidden}
.c svg{width:${Size}px;height:${Size}px;display:block;transform:scale($z);transform-origin:50% 50%}
</style></head><body><div class="row">$cells</div><script>
document.querySelectorAll('.c').forEach(function(c){
  var t = parseFloat(c.dataset.t), svg = c.querySelector('svg');
  if (svg.pauseAnimations) { svg.pauseAnimations(); svg.setCurrentTime(t); }
  if (c.getAnimations) c.getAnimations({subtree:true}).forEach(function(a){ a.pause(); a.currentTime = t * 1000; });
});
</script></body></html>
"@
  $file = Join-Path $Download 'frame.html'
  [IO.File]::WriteAllText($file, $html, (New-Object Text.UTF8Encoding $false))
  if (Test-Path $out) { Remove-Item $out }
  # A fresh profile per render: a profile left locked by the previous Edge would make the next one hang.
  $prof = Join-Path $edgeProfile ([guid]::NewGuid().ToString('N'))
  $edgeArgs = @('--headless=new', '--disable-gpu', '--log-level=3', '--disable-logging', '--hide-scrollbars', '--no-first-run', "--user-data-dir=$prof",
            '--default-background-color=00000000', '--force-device-scale-factor=1', '--virtual-time-budget=1500',
            "--window-size=$($frames * $Size),$Size", "--screenshot=$out", "file:///$($file -replace '\\','/')")
  # -Wait would also wait for Edge's background helpers (they keep running); wait for the main process only.
  $p = Start-Process $edge -ArgumentList $edgeArgs -PassThru -WindowStyle Hidden
  if (-not $p.WaitForExit(60000)) { $p.Kill() }
  Get-CimInstance Win32_Process -Filter "Name='msedge.exe'" |
    Where-Object { $_.CommandLine -like "*$prof*" } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
  Start-Sleep -Milliseconds 300
  Remove-Item $prof -Recurse -Force -ErrorAction SilentlyContinue
  if (-not (Test-Path $out)) { throw "render failed: $out" }
}

foreach ($setName in $sets.Keys) {
  $set = $sets[$setName]
  $dir = Join-Path $Project "Assets\WeatherAnim\$setName"
  New-Item -ItemType Directory -Force $dir | Out-Null
  foreach ($key in $set.Map.Keys) {
    if ($Only -and "$setName/$key" -notlike $Only) { continue }
    $name = $set.Map[$key]
    $anim = Get-Svg ($set.Anim -f $name)
    $loop = Get-Loop $anim
    $frames = if ($loop -gt 0) { [int][math]::Round($loop * $Fps) } else { 1 }
    Render $anim $frames (Join-Path $dir "$key.png") $set.Zoom
    Render (Get-Svg ($set.Static -f $name)) 1 (Join-Path $dir "$key.static.png") $set.Zoom
    "{0}/{1}: {2} kare ({3} sn)" -f $setName, $key, $frames, $loop
  }
}
foreach ($l in @(@('meteocons', 'https://raw.githubusercontent.com/basmilius/meteocons/main/LICENSE'),
                 @('makin-things', 'https://raw.githubusercontent.com/Makin-Things/weather-icons/main/LICENSE'))) {
  & curl.exe -s -f -L -o (Join-Path $Download "LICENSE-$($l[0]).txt") $l[1]
}
'OK'
