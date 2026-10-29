# Takes README screenshots with demo data (no personal data): docs/screenshots/*.png
# The running copy is closed, the app is started with PULSEMONITOR_DATA_DIR pointing to a temporary demo folder
# (generic location, IP items off, generated traffic history), then the normal copy is restarted.
# Windows are captured with PrintWindow (nothing behind them is captured); the taskbar only around PulseMonitor.
# Usage: powershell -ExecutionPolicy Bypass -File tools\screenshots.ps1 [-Exe <path to PulseMonitor.exe>]
param([string]$Exe = "$env:LOCALAPPDATA\Programs\PulseMonitor\PulseMonitor.exe")
$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [Text.Encoding]::UTF8
$root = Split-Path -Parent $PSScriptRoot
$out = Join-Path $root 'docs\screenshots'
$demo = Join-Path $env:TEMP 'PulseMonitor-demo'
Add-Type -AssemblyName System.Drawing, UIAutomationClient, UIAutomationTypes
Add-Type -ReferencedAssemblies System.Drawing @'
using System; using System.Drawing; using System.Runtime.InteropServices;
public static class Shot {
  [StructLayout(LayoutKind.Sequential)] public struct R { public int L, T, Ri, B; }
  [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern IntPtr FindWindow(string c, string n);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out R r);
  [DllImport("user32.dll")] static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
  [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h, uint m, IntPtr w, IntPtr l);
  [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr v);
  public static void Window(IntPtr h, string file) {
    R r; GetWindowRect(h, out r);
    using (var bmp = new Bitmap(r.Ri - r.L, r.B - r.T)) using (var g = Graphics.FromImage(bmp)) {
      IntPtr hdc = g.GetHdc(); PrintWindow(h, hdc, 2); g.ReleaseHdc(hdc); bmp.Save(file);
    }
  }
  public static void Screen(IntPtr h, int padX, int padY, string file) {
    R r; GetWindowRect(h, out r);
    int x = r.L - padX, y = r.T - padY, w = r.Ri - r.L + 2 * padX, hh = r.B - r.T + 2 * padY;
    using (var bmp = new Bitmap(w, hh)) using (var g = Graphics.FromImage(bmp)) { g.CopyFromScreen(x, y, 0, 0, bmp.Size); bmp.Save(file); }
  }
}
'@
[Shot]::SetProcessDpiAwarenessContext([IntPtr]-4) | Out-Null
$A = [System.Windows.Automation.AutomationElement]

function Write-Utf8([string]$path, [string]$text) { [IO.File]::WriteAllText($path, $text, (New-Object Text.UTF8Encoding $false)) }
function Find-Window([string]$title, [int]$seconds = 10) {
  for ($i = 0; $i -lt $seconds * 4; $i++) { $h = [Shot]::FindWindow([NullString]::Value, $title); if ($h -ne [IntPtr]::Zero) { return $h }; Start-Sleep -Milliseconds 250 }
  throw "window not found: $title"
}
# UI Automation element by name (and type); retries while the window is still loading.
function Find-Element($hwnd, [string]$name, $type) {
  $c = New-Object System.Windows.Automation.AndCondition (
    (New-Object System.Windows.Automation.PropertyCondition ($A::ControlTypeProperty), $type),
    (New-Object System.Windows.Automation.PropertyCondition ($A::NameProperty), $name))
  for ($i = 0; $i -lt 20; $i++) {
    try {
      $e = $A::FromHandle($hwnd).FindFirst([System.Windows.Automation.TreeScope]::Descendants, $c)
      if ($e) { return $e }
    } catch { }   # window still loading
    Start-Sleep -Milliseconds 250
  }
  throw "element not found: $name"
}
function Close-Window([string]$title) {
  $h = [Shot]::FindWindow([NullString]::Value, $title)
  if ($h -eq [IntPtr]::Zero) { return }
  [Shot]::PostMessage($h, 0x0010, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null
  for ($i = 0; $i -lt 20 -and [Shot]::FindWindow([NullString]::Value, $title) -ne [IntPtr]::Zero; $i++) { Start-Sleep -Milliseconds 250 }
}
function Select-Tab($hwnd, [string]$name) {
  (Find-Element $hwnd $name ([System.Windows.Automation.ControlType]::TabItem)).GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
  Start-Sleep -Milliseconds 800
}
function Start-Demo([hashtable]$style) {
  Start-Process $Exe -ArgumentList '--exit' -Wait; Start-Sleep 2
  $items = 'Weather','Keyboard','Upload','Download','Cpu','Ram','Gpu','Disk','GpuTemp','DiskTemp','Ping','LocalIp','PublicIp' |
    ForEach-Object { @{ Id = $_; Visible = ($_ -notin 'LocalIp', 'PublicIp') } }
  $settings = @{ SchemaVersion = 2; HideInFullscreen = $true; TrayGapPx = 6; Items = $items; Style = $style
    WeatherLocation = @{ Name = 'Kadıköy'; DisplayName = 'Kadıköy'; Context = 'İstanbul'; Latitude = 40.9903; Longitude = 29.0275; Source = 'Open-Meteo' } }
  Write-Utf8 (Join-Path $demo 'settings.json') ($settings | ConvertTo-Json -Depth 6)
  $env:PULSEMONITOR_DATA_DIR = $demo
  Start-Process $Exe
  $env:PULSEMONITOR_DATA_DIR = ''
  Find-Window 'PulseMonitor' | Out-Null
  Start-Sleep 9   # weather + a few seconds of network graph
}

# Demo data folder: generated traffic history (150 days); TrafficMonitor import is marked as done so it never runs.
if (Test-Path $demo) { Remove-Item $demo -Recurse -Force }
New-Item -ItemType Directory -Force $demo, $out | Out-Null
$rnd = New-Object Random 42; $days = [ordered]@{}; $gb = [math]::Pow(1024, 3)
for ($i = 150; $i -ge 0; $i--) {
  $d = (Get-Date).Date.AddDays(-$i); $weekend = $d.DayOfWeek -in 'Saturday', 'Sunday'
  if ($rnd.NextDouble() -lt 0.06) { continue }
  $down = [uint64]((1.2 + $rnd.NextDouble() * 4.5 + ($(if ($weekend) { 2.5 } else { 0 }))) * $gb)
  $up = [uint64]((0.2 + $rnd.NextDouble() * 1.4) * $gb)
  $days[$d.ToString('yyyy-MM-dd')] = @($down, $up)
}
Write-Utf8 (Join-Path $demo 'traffic-history.json') (@{ Version = 1; Days = $days } | ConvertTo-Json -Depth 4 -Compress)

try {
  Start-Demo @{ WeatherIcons = 'MeteoconsFill' }
  [Shot]::Screen((Find-Window 'PulseMonitor'), 6, 2, (Join-Path $out 'taskbar.png'))

  Start-Demo @{ Palette = 'Soft'; Background = 'None'; Labels = 'Icon'; Arrows = 'Icon'; WeatherIcons = 'MakinThings'; Sparkline = 'Side' }
  [Shot]::Screen((Find-Window 'PulseMonitor'), 6, 2, (Join-Path $out 'taskbar-alt.png'))

  Start-Demo @{ WeatherIcons = 'MeteoconsFill' }
  $tray = Find-Window 'PulseMonitor.Tray'
  [Shot]::PostMessage($tray, 0x8001, [IntPtr]::Zero, [IntPtr]0x203) | Out-Null   # tray double-click -> settings
  $settingsWin = Find-Window ('PulseMonitor Ayarlar' + [char]0x0131)
  Start-Sleep 1
  [Shot]::Window($settingsWin, (Join-Path $out 'settings-items.png'))
  Select-Tab $settingsWin ('G' + [char]0x00F6 + 'r' + [char]0x00FC + 'n' + [char]0x00FC + 'm'); [Shot]::Window($settingsWin, (Join-Path $out 'settings-style.png'))
  Select-Tab $settingsWin 'Hava durumu'; [Shot]::Window($settingsWin, (Join-Path $out 'settings-weather.png'))
  Select-Tab $settingsWin ('Uyar' + [char]0x0131 + 'lar'); [Shot]::Window($settingsWin, (Join-Path $out 'settings-alerts.png'))
  Close-Window ('PulseMonitor Ayarlar' + [char]0x0131)

  # History window: via the tray menu is mouse-driven, so open it through the settings window's button.
  [Shot]::PostMessage($tray, 0x8001, [IntPtr]::Zero, [IntPtr]0x203) | Out-Null
  $settingsWin = Find-Window ('PulseMonitor Ayarlar' + [char]0x0131)
  Select-Tab $settingsWin 'Genel'
  $btnName = 'Trafik ge' + [char]0x00E7 + 'mi' + [char]0x015F + 'i' + [char]0x2026
  (Find-Element $settingsWin $btnName ([System.Windows.Automation.ControlType]::Button)).GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
  $hist = Find-Window ('Trafik ge' + [char]0x00E7 + 'mi' + [char]0x015F + 'i')
  Close-Window ('PulseMonitor Ayarlar' + [char]0x0131)
  Start-Sleep 1
  [Shot]::Window($hist, (Join-Path $out 'history.png'))
  Close-Window ('Trafik ge' + [char]0x00E7 + 'mi' + [char]0x015F + 'i')
}
finally {
  Start-Process $Exe -ArgumentList '--exit' -Wait; Start-Sleep 2
  $env:PULSEMONITOR_DATA_DIR = ''
  Start-Process $Exe   # the normal copy with the user's own data
  Remove-Item $demo -Recurse -Force -ErrorAction SilentlyContinue
}
Get-ChildItem $out -File | ForEach-Object { '{0}  {1:N0} KB' -f $_.Name, ($_.Length / 1KB) }
