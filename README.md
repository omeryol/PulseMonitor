# PulseMonitor

Windows 11 görev çubuğu için hafif bir sistem monitörü: ağ hızı, CPU/RAM/GPU/disk, sıcaklıklar, ping, IP,
klavye kilit tuşları ve köy/mahalle düzeyinde hava durumu. [TrafficMonitor](https://github.com/zhongyang219/TrafficMonitor)'dan
esinlenilmiştir; C# / WPF (.NET 10) ile sıfırdan yazılmıştır. Arayüz Türkçedir.

## Özellikler

- **Görev çubuğuna gömülü**: sistem tepsisinin soluna kendiliğinden yerleşir, tepsi genişleyince kayar; tam ekran
  uygulamada gizlenir; Explorer yeniden başlarsa toparlanır; titremez (değer sütunları sabit genişlikte).
- **Göstergeler** (her biri ayrı açılır/kapanır, sırası ayarlanır): yükleme/indirme hızı (değerin arkasında mini grafik),
  CPU, RAM, GPU, disk etkinliği, GPU ve NVMe sıcaklığı, ping, yerel/dış IP, Caps/Num/Scroll Lock, hava durumu.
- **Hava durumu**: köy, mahalle, kasaba araması (Open-Meteo + OpenStreetMap); seçilen noktanın tahmini; hissedilen
  sıcaklık, açıklama, °C/°F; 9 simge seti, 4'ü animasyonlu (Meteocons, Makin-Things).
- **Görünüm**: tema, palet ve öğe başına renk, yüke göre renk, yazı boyutu/kalınlığı, ok ve etiket biçimi, sütun kartları,
  saydamlık, aralıklar, ölçek.
- **Trafik geçmişi**: günlük kayıt, ay/yıl grafiği; TrafficMonitor geçmişini (`history_traffic.dat`) bir kez içe aktarır.
- **Uyarılar**: günlük trafik sınırı, CPU, RAM, GPU/disk sıcaklığı için Windows bildirimi.
- **Hafif**: işlemciyle çizim, ~60 MB bellek, tek çekirdeğin ~% 1–4'ü.
- **Sürücüsüz**: çekirdek sürücüsü yoktur, yönetici yetkisi istemez; antivirüs uyarısına yol açacak bileşen içermez.
  Bu yüzden CPU sıcaklığı gösterilmez (masaüstü işlemcilerde sürücüsüz okunamıyor).

## Gereksinimler

- Windows 10 1809+ / Windows 11
- [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) (derlemek için .NET 10 SDK)

## Derleme ve kurulum

```powershell
dotnet build -c Debug
powershell -ExecutionPolicy Bypass -File tools\publish.ps1   # %LOCALAPPDATA%\Programs\PulseMonitor'a kurar ve başlatır
```

`PulseMonitor.exe --exit` çalışan kopyayı veriyi kaydederek kapatır. Kod düzeni: [docs/KOD-HARITASI.md](docs/KOD-HARITASI.md),
değişiklikler: [CHANGELOG.md](CHANGELOG.md).

## Gizlilik ve ağ istekleri

Ayarlar, trafik geçmişi ve önbellek yalnızca `%LOCALAPPDATA%\PulseMonitor` altında tutulur. Program şu adreslere bağlanır:

| Servis | Ne zaman |
|---|---|
| Open-Meteo (tahmin) | Hava durumu açıkken 15 dakikada bir |
| Open-Meteo + OpenStreetMap Nominatim (konum araması) | Yalnızca ayarlarda "Ara"ya basınca |
| api.ipify.org (dış IP) | Yalnızca "Dış IP" öğesi açıkken |
| Ping sunucusu (varsayılan 1.1.1.1) | Yalnızca "Ping" öğesi açıkken |

## Lisans

[MIT](LICENSE). Dahil edilen simgeler ve kullanılan veri servisleri: [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
