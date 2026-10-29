# PulseMonitor

Hafif bir Windows 11 görev çubuğu monitörü (C# / WPF, .NET 10).
TrafficMonitor'un özellikleri hedeflenir; öncelik görsel tasarım ve hafifliktir.
Depo GitHub'da açık yayınlanabilir: **depoya kişisel veri girmez** — konum/köy adı, koordinat, IP, donanım modeli,
kullanıcı klasörü yolu, e-posta, özel bağlantı. Örneklerde genel yer adları (Kadıköy, Kayaköy) kullanılır.
Commit öncesi `tools/check.ps1` bu kalıpları da arar.

## Önce oku
- Konu → dosya: `docs/KOD-HARITASI.md` (depo geneli tarama yapma, önce haritaya bak).
- Değişiklikler: `CHANGELOG.md`.
- Günlük: `%LOCALAPPDATA%\PulseMonitor\logs\pulsemonitor.log`; ayarlar: `settings.json` aynı klasörde.

## Derleme ve doğrulama
```
dotnet build -c Debug && powershell -ExecutionPolicy Bypass -File tools\check.ps1
```
- `tools/check.ps1`: her `.cs` / `.xaml` dosyası haritada olmalı, 300 satırı geçmemeli (hedef 200).
- Commit doğrulamaya zincirlenir: `dotnet build ... && powershell ... check.ps1 && git commit ...`
- Derlemenin geçmesi test sayılmaz: program çalıştırılıp senaryolar gerçekten denenir.
- Kullanıcının kullandığı kopya kurulu olandır: `%LOCALAPPDATA%\Programs\PulseMonitor`.
  Kurulum/güncelleme: `powershell -ExecutionPolicy Bypass -File tools\publish.ps1` (check + Release + düzgün kapatma + başlatma).
- Sürüm: `tools\build_release.ps1` (kurulum + zip, `artifacts\`); yayın `git tag v<sürüm>` + push → GitHub Actions.
  README görüntüleri `tools\screenshots.ps1` (demo verisi; gerçek veriye dokunmaz).
- Debug denemesinden önce çalışan kopya düzgün kapatılır: `PulseMonitor.exe --exit`
  (tek kopya kuralı yüzünden yoksa Debug kopyası hemen kapanır). `Stop-Process -Force` son dakikanın trafiğini kaybettirir.
- Ölçüm doğruluğu Windows'un kendi sayaçlarıyla karşılaştırılarak test edilir; Türkçe Windows'ta
  `Get-Counter` yerelleştirilmiş sayaç adı ister (ör. `\FizikselDisk(_Total)\% Boşta Zaman`).

## Kurallar
- Tek sorumluluklu küçük dosyalar; yeni özellik kendi klasöründe/modülünde açılır.
- Arayüz metinleri yalnızca `Resources/Strings.tr.xaml` içinde; kodda sabit Türkçe metin yok
  (günlük mesajları hariç). Kesme işareti için tipografik ’ kullanılır.
- Renk ve yazı tipleri yalnızca `Resources/Theme.xaml` / `Resources/Palettes.xaml` içinde; görünüm ayarları
  şablonlara DynamicResource ile gelir (`Core/StyleResources.cs`, `Core/ThemeBrushes.cs`).
- Aynı ResourceDictionary ağacında anahtar iki kez kullanılamaz (açılışta çöker); simge renkleri `Wx.Color.*`.
- Test betikleri Türkçe karakter içeriyorsa ASCII yazılır (`[char]0x0131`); BOM'suz UTF-8 .ps1, PowerShell 5.1'de bozulur.
- Windows API çağrıları yalnızca `Interop/` altında tanımlanır.
- Hatalar sessizce yutulmaz; en azından `Log.Warn/Error` ile yazılır.
- Disk yazımı atomik (geçici dosya + taşıma); bozuk dosya yedeklenir, program çökmez.
- Ayarlara yeni alan eklenince varsayılanı eski dosyalarla uyumlu olmalı.
- API anahtarı ya da gizli bilgi koda yazılmaz.
- TrafficMonitor'dan (MIT) kod alınırsa `THIRD-PARTY-NOTICES.md` dosyasına telif ve lisans eklenir.

## Bilinen teknik notlar
- Görev çubuğu penceresi `WS_EX_NOACTIVATE`: tıklanınca odak çalmaz.
- Sağ tık menüsü açılmadan **önce** gizli tepsi penceresi öne alınır (`AppMenu.AnchorWindow`);
  menü açıldıktan sonra öne almak menüyü hemen kapatır.
- Konumlar fiziksel pikselle hesaplanır (`GetWindowRect`, `SetWindowPos`); WPF boyutu DPI ile çarpılır.

## Yol haritası
1. İskelet (tamam, 0.1.0)
2. Veriler (tamam, 0.2.0): ağ, geçmiş, GPU, disk, klavye, ayarlar penceresi, kurulum betiği
3. Hava durumu (tamam, 0.3.0): Open-Meteo + Nominatim, köy/mahalle araması, önbellek
4. Görsel tasarım (0.6.0): tasarım tezgâhı (tarayıcı taslağı) seçenekleri programda ayar;
   Meteocons + Makin-Things animasyonlu simgeleri (tools/render_icons.ps1). Atmosferik kart arka planı istenmedi.
5. Ekstralar (0.7.0): geçmiş penceresi, uyarılar, ping, GPU/NVMe sıcaklığı (sürücüsüz).
   Kural: çekirdek sürücüsü (WinRing0, PawnIO vb.) kullanılmaz — kullanıcı antivirüs uyarısı istemiyor.
   CPU sıcaklığı eklenmeyecek (kullanıcı kararı, 2026-10-29).
