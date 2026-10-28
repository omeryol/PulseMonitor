# Kod Haritası

Konu → dosya. Bir sorun olduğunda önce buraya bakılır. Yeni dosya eklenince bu tabloya da eklenir;
`tools/check.ps1` haritada olmayan dosyayı yakalar.

## Giriş ve ortak altyapı
| Dosya | Ne yapar |
|---|---|
| `App.xaml` | Uygulama kaynakları (tema, metinler, BoolToVisibility) |
| `App.xaml.cs` | Başlatma: parçaları oluşturur ve bağlar, `--exit`, kapanışta temizler, hata yakalayıcılar |
| `Core/AppPaths.cs` | Diskteki tüm yollar (`%LOCALAPPDATA%\PulseMonitor`) |
| `Core/Log.cs` | Dosya günlüğü (`logs/pulsemonitor.log`, 1 MB'ta devreder) |
| `Core/SingleInstance.cs` | Tek kopya çalışma (mutex) ve `--exit` ile düzgün kapanma isteği (olay) |
| `Core/Strings.cs` | Koddan arayüz metni okuma |
| `Core/ThemeBrushes.cs` | Tema + palet + zemin fırçaları (`Brush.*`); tema seçimi (Otomatik/Koyu/Açık) |
| `Core/StyleResources.cs` | Renk dışı görünüm kaynakları: yazı boyutu/kalınlığı, boşluklar, mini grafik görünürlüğü |

## Ayarlar
| Dosya | Ne yapar |
|---|---|
| `Settings/AppSettings.cs` | Ayar alanları ve varsayılanları |
| `Settings/SettingsStore.cs` | JSON okuma/yazma; atomik yazma, bozuk dosyayı yedekleyip varsayılana dönme, `Changed` olayı |
| `Settings/StyleSettings.cs` | Görünüm ayarları (tasarım tezgâhı seçenekleri) ve varsayılanları |
| `Settings/StyleOptions.cs` | Görünüm seçenek kimlikleri, sınırlar, doğrulama (`Normalize`), renk ayrıştırma |
| `Settings/TaskbarItems.cs` | Öğe kimlikleri, varsayılan sıra/görünürlük, listeyi düzeltme; eski ShowX alanlarını ve ayrı kilit tuşu öğelerini taşıma |

## Görev çubuğu entegrasyonu
| Dosya | Ne yapar |
|---|---|
| `Taskbar/TaskbarLocator.cs` | Görev çubuğu, sistem tepsisi (`TrayNotifyWnd`) ve ekran konumunu okur |
| `Taskbar/TaskbarDocker.cs` | Pencereyi görev çubuğuna bağlar, tepsinin soluna yerleştirir, Explorer yeniden başlarsa tekrar bağlanır |
| `Taskbar/FullscreenDetector.cs` | Tam ekran uygulama algılama |

## Sistem tepsisi ve menü
| Dosya | Ne yapar |
|---|---|
| `Tray/TrayIcon.cs` | Tepsi simgesi (Shell_NotifyIcon), Explorer yeniden başlayınca simgeyi geri ekler |
| `Tray/AppMenu.cs` | Sağ tık menüsü (tepsi ve görev çubuğu penceresi ortak) |

## Windows ile ilgili işler
| Dosya | Ne yapar |
|---|---|
| `Platform/StartupRegistration.cs` | Windows ile başlat (HKCU Run) |
| `Platform/SystemActions.cs` | Görev Yöneticisi açma vb. |
| `Platform/SystemTheme.cs` | Görev çubuğu açık/koyu tema algılama |

## Ölçümler
| Dosya | Ne yapar |
|---|---|
| `Metrics/MetricsHub.cs` | Arka plan ölçüm döngüsü (1 sn), trafiği geçmişe ekler, dakikada bir kaydeder |
| `Metrics/MetricsSnapshot.cs` | Bir saniyelik ölçüm kaydı |
| `Metrics/CpuRamSampler.cs` | CPU ve RAM yükü |
| `Metrics/NetworkAdapters.cs` | Bağdaştırıcı listesi ve sayaçları (GetIfTable2); filtre/geri döngü atlanır |
| `Metrics/NetworkSampler.cs` | Ağ hızı: bağdaştırıcı başına sayaç farkı; otomatik = bağlı fiziksel bağdaştırıcılar |
| `Metrics/PdhCounter.cs` | Performans sayacı sarmalayıcısı (İngilizce yol, joker karakter desteği) |
| `Metrics/GpuSampler.cs` | GPU yükü (3D motor, GPU başına toplam, en yükseği) |
| `Metrics/DiskSampler.cs` | Disk etkinliği (100 − boşta süre) |
| `Metrics/KeyboardState.cs` | Caps / Num / Scroll Lock |
| `Metrics/TemperatureSampler.cs` | Sürücüsüz sıcaklık: GPU (D3DKMT), disk (NVMe IOCTL); CPU yok (sürücü gerekir) |
| `Metrics/PingMonitor.cs` | Ping (5 sn, yalnızca öğe açıkken), sunucu ayarı `PingHost` |
| `Metrics/IpAddressMonitor.cs` | Yerel IPv4 (seçili bağdaştırıcı ya da varsayılan ağ geçidi olan) ve dış IP (ipify, 15 dk, yalnızca açıkken) |

## Trafik geçmişi
| Dosya | Ne yapar |
|---|---|
| `Traffic/TrafficHistory.cs` | Günlük toplamlar (bellekte, iş parçacığı güvenli) |
| `Traffic/TrafficHistoryStore.cs` | `traffic-history.json` okuma/yazma (atomik, bozuksa yedek) |
| `Traffic/TrafficMonitorImporter.cs` | TrafficMonitor `history_traffic.dat` içe aktarma (ilk açılışta bir kez, yalnızca okur) |

## Hava durumu
| Dosya | Ne yapar |
|---|---|
| `Weather/WeatherService.cs` | 15 dk'da bir yenileme, hatada 2 dk sonra tekrar, uyanınca / ağ gelince yenileme |
| `Weather/OpenMeteoClient.cs` | Open-Meteo tahmin isteği ve ayrıştırma (anlık + 12 saat) |
| `Weather/LocationSearch.cs` | Konum araması: Open-Meteo (GeoNames) + Nominatim (OSM), birleştirme ve tekrarları ayıklama |
| `Weather/WeatherLocation.cs` | Seçili konum (ayarlarda saklanır), görünen ad |
| `Weather/WeatherReport.cs` | Rapor ve saatlik tahmin modeli |
| `Weather/WeatherCodes.cs` | WMO kodu → açıklama, simge adı, karakter, animasyon türü |
| `Weather/WeatherIcons.cs` | Seçili simge setinden simgeyi bulma: resim, şekil ya da film şeridi (Meteocons, Makin-Things); `WeatherAnimationKind` |
| `Weather/WeatherCache.cs` | `weather-cache.json` (açılışta ve internet yokken gösterilir) |
| `Core/Http.cs` | Paylaşılan HttpClient (User-Agent) |

## Uyarılar
| Dosya | Ne yapar |
|---|---|
| `Alerts/AlertMonitor.cs` | Eşik denetimi (trafik günde bir, CPU 30 sn sürmeli, 30 dk tekrar aralığı) |
| `Settings/AlertSettings.cs` | Uyarı ayarları ve sınırları |

## Biçimlendirme
| Dosya | Ne yapar |
|---|---|
| `Formatting/ValueFormat.cs` | Hız, boyut, yüzde biçimleri ve sabit genişlik yer tutucuları |

## Arayüz
| Dosya | Ne yapar |
|---|---|
| `Views/TaskbarWindow.xaml` | Görev çubuğundaki pencere: sütunlar ve öğe şablonları (ölçüm satırı, kilit tuşu, hava) |
| `Views/TaskbarWindow.xaml.cs` | Ölçek (`SetScale`) |
| `Views/Controls/Sparkline.cs` | Ağ mini grafiği (doğrudan çizim) |
| `Views/Controls/SpriteIcon.cs` | Animasyonlu hava simgesi: film şeridi PNG'sini 10 kare/sn oynatır, kapalıysa sabit hâli |
| `Views/Controls/WeatherAnimation.cs` | Hava kartı animasyonu (yağmur, kar, sis, şimşek; 15 kare/sn) |
| `Views/Controls/ChartConverters.cs` | Grafik dönüştürücüleri: `Negate`, `ToGridLength` |
| `Views/Controls/IsNotNullConverter.cs` | Şablon tetikleyicileri için null denetimi |
| `Views/Settings/StylePanel.xaml` | Ayarlar → Görünüm sekmesi |
| `Views/Settings/StylePanel.xaml.cs` | (boş kod arkası) |
| `Views/Settings/WeatherStylePanel.xaml` | Ayarlar → Hava durumu görünümü (simge seti, birim, hissedilen, renk, animasyon) |
| `Views/Settings/WeatherStylePanel.xaml.cs` | (boş kod arkası) |
| `Views/Settings/ItemsSettingsPanel.xaml` | Ayarlar: öğe listesi (göster/gizle, yukarı/aşağı) |
| `Views/Settings/ItemsSettingsPanel.xaml.cs` | (boş kod arkası) |
| `Views/Controls/WeatherItem.xaml` | Görev çubuğu hava öğesi (simge + sıcaklık + yer adı, ipucunda ayrıntı) |
| `Views/Controls/WeatherItem.xaml.cs` | (boş kod arkası) |
| `Views/Settings/WeatherSettingsPanel.xaml` | Ayarlar: hava durumu bölümü (arama, seçim, görünen ad, kaynak satırı) |
| `Views/Settings/WeatherSettingsPanel.xaml.cs` | (boş kod arkası) |
| `Views/AppWindows.cs` | Ayarlar ve trafik geçmişi pencereleri (tek kopya) |
| `Views/HistoryWindow.xaml` | Trafik geçmişi: ay/yıl çubuk grafiği ve özet |
| `Views/HistoryWindow.xaml.cs` | (boş kod arkası) |
| `Views/Settings/AlertsPanel.xaml` | Ayarlar → Uyarılar |
| `Views/Settings/AlertsPanel.xaml.cs` | (boş kod arkası) |
| `Views/SettingsWindow.xaml` | Ayarlar penceresi: sekmeler (Göstergeler · Görünüm · Hava durumu · Genel) |
| `Views/SettingsWindow.xaml.cs` | Kapat düğmesi |
| `ViewModels/ObservableObject.cs` | Bildirimli özellik tabanı |
| `ViewModels/MetricLine.cs` | `MetricLine` (etiket yazı/simge + değer, renk kuralı, yük rengi), `KeyboardViewModel` + `KeyIndicator`, `ITallItem` |
| `ViewModels/TaskbarLayout.cs` | Görünür öğeleri iki satırlık sütunlara dizme (`TaskbarColumn`) |
| `ViewModels/TaskbarViewModel.cs` | Görev çubuğu öğeleri, sıraya göre sütun kurma, değer güncelleme |
| `ViewModels/ItemsSettingsViewModel.cs` | Ayarlar: öğe sırası ve görünürlüğü (`ItemRow`) |
| `ViewModels/SettingsViewModel.cs` | Ayarlar penceresi (her değişiklik hemen kaydedilir), bağdaştırıcı listesi |
| `ViewModels/WeatherViewModel.cs` | Hava öğesi: simge, sıcaklık + hissedilen, renk, animasyon, eski veri soluklaştırma |
| `ViewModels/WeatherTooltip.cs` | Hava ipucu metni |
| `ViewModels/SpeedHistory.cs` | Mini grafik için 24 sn hız geçmişi, otomatik ölçek |
| `ViewModels/StyleSettingsViewModel.cs` | Ayarlar: görünüm seçenekleri, varsayılana dönme |
| `ViewModels/Choice.cs` | Açılır liste seçimi (`Option`) |
| `ViewModels/ColorRow.cs` | Özel renk satırı (#RRGGBB + önizleme) |
| `ViewModels/WeatherSettingsViewModel.cs` | Konum arama/seçme, `LocationOption` |
| `ViewModels/RelayCommand.cs` | Basit komut |
| `ViewModels/HistoryViewModel.cs` | Geçmiş penceresi: dönem, çubuklar (`HistoryBar`), değer çizgileri (`GridLine`), ortalama, özet kartları |
| `ViewModels/HistoryRows.cs` | Günlük kayıtlardan ay/yıl satırları (hafta sonu, bugün) |
| `ViewModels/AlertsSettingsViewModel.cs` | Ayarlar → Uyarılar, deneme bildirimi |
| `Resources/Theme.xaml` | Temaya bağlı sabit renkler (yazı, yük, sıcaklık, zemin), yazı tipi |
| `Resources/Palettes.xaml` | Öğe paletleri: Canlı, Yumuşak, Tek vurgu (koyu/açık) |
| `Resources/LabelIcons.xaml` | Etiket ve ok simgeleri (çizgi) |
| `Resources/WeatherIcons.xaml` | Hava simgeleri: Renkli çizim (kendi) |
| `Resources/WeatherIconsRealistic.xaml` | Hava simgeleri: Gerçekçi 3D (Fluent Emoji, `Assets/Weather/*.png`) |
| `Resources/WeatherIconsFluent.xaml` | Hava simgeleri: Çizgi ve Dolu (Fluent UI System Icons; `tools/fetch_icons.ps1` üretir) |
| `Resources/Strings.tr.xaml` | Arayüz metinleri (Türkçe) |
| `Resources/Strings.Style.tr.xaml` | Görünüm ayarlarının metinleri (`Style.*`) |

## Windows API
| Dosya | Ne yapar |
|---|---|
| `Interop/NativeTypes.cs` | RECT, MONITORINFO, MEMORYSTATUSEX, NOTIFYICONDATA |
| `Interop/User32.cs` | Pencere, ekran, mesaj, klavye işlevleri |
| `Interop/Shell32.cs` | Tepsi simgesi, tam ekran durumu |
| `Interop/Kernel32.cs` | CPU süreleri, bellek durumu |
| `Interop/IpHlpApi.cs` | GetIfTable2, MIB_IF_ROW2 (1352 bayt) |
| `Interop/Pdh.cs` | Performans sayacı işlevleri |
| `Interop/Gdi32.cs` | D3DKMT: GPU listesi ve performans verisi (sıcaklık) |
| `Interop/StorageIo.cs` | Disk sorgusu: IOCTL_STORAGE_QUERY_PROPERTY (sıcaklık), erişim hakkı istemeden açma |

## Araçlar
| Dosya | Ne yapar |
|---|---|
| `tools/check.ps1` | Bekçi: her dosya haritada mı, satır sınırı, kişisel veri taraması (genel kalıplar + git dışı `tools/private-words.txt`) |
| `tools/render_icons.ps1` | Meteocons + Makin-Things SVG'lerini görünmez Edge ile film şeridi PNG'ye çevirir (`Assets/WeatherAnim`) |
| `tools/fetch_icons.ps1` | Microsoft MIT simgelerini indirir, PNG'leri 64 px'e küçültür, SVG'leri XAML şekline çevirir |
| `tools/publish.ps1` | Release derler, `%LOCALAPPDATA%\Programs\PulseMonitor`'a kurar, çalışanı düzgün kapatıp yenisini başlatır |

## Veri dosyaları (`%LOCALAPPDATA%\PulseMonitor`)
| Dosya | İçerik |
|---|---|
| `settings.json` | Ayarlar |
| `traffic-history.json` | Günlük trafik `{ "Days": { "yyyy-MM-dd": [indirme, yükleme] } }` (bayt) |
| `weather-cache.json` | Son hava raporu (yalnızca aynı konum için kullanılır) |
| `logs/pulsemonitor.log` | Günlük |

## Sorun → nereye bak
| Belirti | Dosya |
|---|---|
| Pencere yanlış yerde / tepsiyle çakışıyor | `Taskbar/TaskbarDocker.cs` → `Place`, `Taskbar/TaskbarLocator.cs` |
| Pencere kayboldu | `pulsemonitor.log`, `TaskbarDocker.Refresh` (tam ekran / otomatik gizleme) |
| Tepsi simgesi yok | `Tray/TrayIcon.cs`, günlükte "Tepsi simgesi" uyarısı |
| Menü hemen kapanıyor | `Tray/AppMenu.cs` → `Show` (önce `AnchorWindow` öne alınmalı) |
| Ayarlar sıfırlandı | `settings.bad-*.json`, `SettingsStore.Load` |
| Yazı açık temada okunmuyor | Ayarlar → Görünüm → tema, `Platform/SystemTheme.cs`, `Core/ThemeBrushes.cs`, `Resources/Theme.xaml` |
| Görünüm ayarı uygulanmıyor | `App.ApplyAppearance`, `Core/StyleResources.cs` / `ThemeBrushes.cs`, şablondaki DynamicResource anahtarı |
| Hava simgesi boş | `Weather/WeatherIcons.cs` anahtarı, `Resources/WeatherIcons*.xaml`, `Assets/Weather`, `Assets/WeatherAnim` (csproj Resource), günlükte "Hava simgesi yüklenemedi" |
| Açılışta hemen kapanıyor | Günlükte "Beklenmeyen hata" (`App` static ctor), çoğunlukla XAML kaynağında yinelenen anahtar |
| Ağ hızı sıfır / yanlış | Ayarlarda seçili bağdaştırıcı, `Metrics/NetworkSampler.cs`, günlükte "MIB_IF_ROW2" |
| Trafik iki kat sayılıyor | `Metrics/NetworkAdapters.cs` (filtre arabirimleri atlanıyor mu), seçili bağdaştırıcı |
| GPU/Disk "% --" | Günlükte "Performans sayacı açılamadı", `Metrics/PdhCounter.cs` |
| Geçmiş kayboldu | `traffic-history.json.bad-*`, `Traffic/TrafficHistoryStore.cs` |
| Hava durumu "--°" | Günlükte "Hava durumu alınamadı", `WeatherService.RefreshAsync`, ayarlardaki koordinat |
| Konum bulunamıyor | `Weather/LocationSearch.cs` (ülke filtresi TR, iki kaynak), günlükte "konum araması" |
| Değer değişince yazı kayıyor | `Views/TaskbarWindow.xaml` MetricLine şablonu (yer tutucu, SharedSizeGroup), `ValueFormat.*Placeholder` |
| Sıcaklık "--" | `Metrics/TemperatureSampler.cs`: GPU WDDM 2.4+ ister; disk yalnızca NVMe (SATA diskler bildirmez) |
| Ping "--" | Ayarlar → Genel → ping sunucusu, günlükte "Ping başarısız" |
| Bildirim gelmiyor | Ayarlar → Uyarılar → deneme bildirimi; Windows odaklanma yardımı (rahatsız etmeyin) açık mı; `TrayIcon.ShowNotification` |
| IP "--" ya da yanlış | `Metrics/IpAddressMonitor.cs` → `FindLocalIp` (seçili bağdaştırıcı), günlükte "Dış IP alınamadı" |
| Öğe sırası / dizilim yanlış | `ViewModels/TaskbarLayout.cs`, `Settings/TaskbarItems.cs` (Normalize), `settings.json` → `Items` |
