param([string]$Download, [string]$Project)
# Downloads Microsoft Fluent Emoji 3D (MIT) and Fluent UI System Icons (MIT) weather icons,
# resizes PNGs to 64 px and converts SVG paths to a WPF ResourceDictionary.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
New-Item -ItemType Directory -Force $Download | Out-Null

$map = [ordered]@{
  ClearDay    = @{ Emoji = 'Sun';                           Sys = 'Weather Sunny' }
  ClearNight  = @{ Emoji = 'Crescent moon';                 Sys = 'Weather Moon' }
  PartlyDay   = @{ Emoji = 'Sun behind small cloud';        Sys = 'Weather Partly Cloudy Day' }
  PartlyNight = @{ Emoji = $null;                           Sys = 'Weather Partly Cloudy Night' }
  Cloudy      = @{ Emoji = 'Cloud';                         Sys = 'Weather Cloudy' }
  Fog         = @{ Emoji = 'Fog';                           Sys = 'Weather Fog' }
  Rain        = @{ Emoji = 'Cloud with rain';               Sys = 'Weather Rain' }
  Thunder     = @{ Emoji = 'Cloud with lightning and rain'; Sys = 'Weather Thunderstorm' }
  Snow        = @{ Emoji = 'Cloud with snow';               Sys = 'Weather Snow' }
}

function Get-File([string]$url, [string]$out) {
  & curl.exe -s -f -L -o $out $url
  if ($LASTEXITCODE -ne 0) { throw "download failed: $url" }
}

$pngDir = Join-Path $Project 'Assets\Weather'
New-Item -ItemType Directory -Force $pngDir | Out-Null
foreach ($k in $map.Keys) {
  $e = $map[$k].Emoji
  if (-not $e) { continue }
  $file = ($e.ToLower() -replace ' ', '_') + '_3d.png'
  $src = Join-Path $Download $file
  Get-File "https://raw.githubusercontent.com/microsoft/fluentui-emoji/main/assets/$([uri]::EscapeDataString($e))/3D/$file" $src
  $img = [System.Drawing.Image]::FromFile($src)
  $bmp = New-Object System.Drawing.Bitmap 64, 64
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $g.InterpolationMode = 'HighQualityBicubic'; $g.SmoothingMode = 'HighQuality'; $g.PixelOffsetMode = 'HighQuality'
  $g.DrawImage($img, 0, 0, 64, 64)
  $g.Dispose(); $img.Dispose()
  $bmp.Save((Join-Path $pngDir "$k.png"), [System.Drawing.Imaging.ImageFormat]::Png); $bmp.Dispose()
}

$sb = New-Object System.Text.StringBuilder
[void]$sb.AppendLine('<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"')
[void]$sb.AppendLine('                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">')
[void]$sb.AppendLine('')
[void]$sb.AppendLine('    <!-- Fluent UI System Icons (MIT, (c) Microsoft Corporation), 24x24. tools/fetch_icons.ps1 ile uretildi; elle degistirmeyin.')
[void]$sb.AppendLine('         Line = "regular" (cizgi), Mono = "filled" (dolu). Renk temadan gelir (WeatherItem). -->')
foreach ($variant in @(@{ Name = 'Line'; Suffix = 'regular' }, @{ Name = 'Mono'; Suffix = 'filled' })) {
  foreach ($k in $map.Keys) {
    $s = $map[$k].Sys
    $n = 'ic_fluent_' + ($s.ToLower() -replace ' ', '_') + "_24_$($variant.Suffix).svg"
    $src = Join-Path $Download $n
    Get-File "https://raw.githubusercontent.com/microsoft/fluentui-system-icons/main/assets/$([uri]::EscapeDataString($s))/SVG/$n" $src
    $svg = Get-Content $src -Raw
    $paths = [regex]::Matches($svg, '\sd="([^"]+)"') | ForEach-Object { $_.Groups[1].Value }
    if (-not $paths) { throw "no path in $n" }
    $data = 'F1 ' + ($paths -join ' ')
    [void]$sb.AppendLine("    <Geometry x:Key=`"WxGeo.$($variant.Name).$k`">$data</Geometry>")
  }
}
[void]$sb.AppendLine('</ResourceDictionary>')
[IO.File]::WriteAllText((Join-Path $Project 'Resources\WeatherIconsFluent.xaml'), $sb.ToString(), (New-Object Text.UTF8Encoding $false))

Get-File 'https://raw.githubusercontent.com/microsoft/fluentui-emoji/main/LICENSE' (Join-Path $Download 'LICENSE-fluentui-emoji.txt')
Get-File 'https://raw.githubusercontent.com/microsoft/fluentui-system-icons/main/LICENSE' (Join-Path $Download 'LICENSE-fluentui-system-icons.txt')
'OK'
