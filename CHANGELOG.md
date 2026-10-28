# Değişiklik Günlüğü

## [0.7.1] - 2026-10-29

### Değişti
- Trafik geçmişi grafiği yenilendi: özet kartları (indirme, yükleme, ortalama, en yoğun), değer çizgileri ve eksen etiketleri, renk geçişli yuvarlak çubuklar, kesikli ortalama çizgisi, hafta sonu gölgesi, üzerine gelince öne çıkan çubuk.
- Depo açık yayına hazır: kişisel veriler (konum, donanım, özel bağlantı) metinlerden çıkarıldı; `tools/check.ps1` kişisel veri taraması yapıyor.

### Kaldırıldı
- Geçmiş penceresindeki "TrafficMonitor'dan içe aktarılan günler dahildir" notu.

## [0.7.0] - 2026-10-29

5. aşama: ekstralar.

### Eklendi
- Hava açıklaması görev çubuğunda (Güneşli, Sisli…): yer adının yanında / yerine / gösterme.
- GPU sıcaklığı (D3DKMT, Görev Yöneticisi'nin kaynağı) ve disk sıcaklığı (NVMe'nin kendi raporu): sürücü kurmadan, yönetici yetkisi olmadan; antivirüsün tepki vereceği bir çekirdek sürücüsü yok. 5 sn'de bir, yalnızca açıkken. Renk: GPU 70/85 °C, disk 55/70 °C.
- Ping öğesi (varsayılan 1.1.1.1, Ayarlar → Genel'den değişir); 100/200 ms üstü turuncu/kırmızı.
- Uyarı bildirimleri (Ayarlar → Uyarılar): günlük trafik sınırı, CPU (30 sn), RAM, GPU ve disk sıcaklığı; deneme bildirimi düğmesi.
- Trafik geçmişi penceresi: ay ve yıl görünümü, indirme/yükleme çubukları, toplam, ortalama, en yoğun gün/ay. Menüde ve Ayarlar → Genel'de.

### Bilinen
- CPU sıcaklığı yok: çoğu masaüstü Intel/AMD sistemde sürücüsüz kaynak yok (ACPI termal bölge, üretici WMI); çekirdek sürücüsü bilerek kullanılmıyor.
- SATA diskler sıcaklık bildirmiyor (yalnızca NVMe).

## [0.6.0] - 2026-10-29

4. aşama: görsel tasarım (tasarım tezgâhında seçilen görünüm varsayılan).

### Eklendi
- Görünüm ayarları (Ayarlar → Görünüm): tema (Otomatik/Koyu/Açık), palet (Canlı/Yumuşak/Tek vurgu/Özel renkler, #RRGGBB), rengin yeri (değer/etiket/ikisi), yüke göre renk, yazı boyutu, değer kalınlığı, ağ okları (5 biçim), etiketler (yazı/simge/yok), ağ mini grafiği (yok/arkasında/yanında), klavye biçimi, arka plan (yok/tek zemin/sütun kartları), saydamlık, sütun arası, etiket–değer arası, pencere boyutu (% 70–110), ayırıcı çizgi, varsayılana dönme.
- Animasyonlu hava simgeleri: Meteocons (dolu, düz, çizgi) ve Makin-Things (MIT). SVG'ler `tools/render_icons.ps1` ile görünmez Edge'de film şeridi PNG'ye çevrildi (36 simge, toplam ~0,9 MB), programda 10 kare/sn oynatılır; animasyon kapalıyken sabit hâli. Bu setlerde kart animasyonu kapanır. Maliyet: tek çekirdeğin ~% 4'ü.
- Hava durumu görünümü: 5 simge seti — Renkli çizim, Gerçekçi 3D (Microsoft Fluent Emoji), Düz çizgi ve Monokrom (Microsoft Fluent UI System Icons), yazı karakteri; °C/°F; derecenin yanında hissedilen; sıcaklığa göre renk; kart içinde animasyon (yağmur, kar, sis, şimşek; 15 kare/sn).
- Ayarlar penceresi sekmeli: Göstergeler · Görünüm · Hava durumu · Genel; boyutlandırılabilir.

### Değişti
- Varsayılan görünüm: canlı palet, renk etikette ve değerde, ▲▼ oklar, 11 pt yarı kalın, değerin arkasında mini grafik, renkli klavye, yarı saydam sütun kartları, renkli hava simgeleri.
- Uygulama kaynaklarında bir hata olursa (ör. yinelenen anahtar) açılış çöküşü de günlüğe yazılır.

### Ölçüm
- Kararlı durumda tek çekirdeğin % 1,2'si; hava animasyonu açıkken % 2,5. Özel bellek ~60 MB.

## [0.5.0] - 2026-10-28

### Eklendi
- Yerel IP öğesi: ölçülen bağdaştırıcının IPv4 adresi (otomatikte internete çıkan, varsayılan ağ geçidi olan bağdaştırıcı). Ağ değişince hemen güncellenir. Varsayılan açık; eski ayarlarda listenin sonuna eklenir.
- Dış IP öğesi (VPN kontrolü için): api.ipify.org'dan 15 dakikada bir ve ağ değişince. Varsayılan kapalı; kapalıyken hiç istek gönderilmez.

## [0.4.0] - 2026-10-28

### Eklendi
- Öğelerin yeri değiştirilebilir: ayarlarda tek liste (göster/gizle, ▲ Yukarı / ▼ Aşağı); listede üstteki öğe solda.
- Her ölçüm ayrı öğe: yükleme, indirme, CPU, RAM, GPU, disk ayrı ayrı açılıp kapatılır.

### Değişti
- Görev çubuğu dizilimi TrafficMonitor gibi: öğeler sırayla iki satırlık sütunlara yerleşir, gizlenen öğenin yerini sonraki doldurur; hava durumu ve klavye kendi sütununu tek başına kaplar.
- Klavye kilit tuşları tek anahtar; CAPS / NUM / SCRL tek sütunda üst üste (sığması için 10 pt yazı, pencere yüksekliği 44 px).
- Ayar dosyası şeması 2: eski `ShowX` alanları ilk açılışta yeni `Items` listesine taşınır; geçici ayrı Caps/Num/Scroll öğeleri tek Klavye öğesinde birleştirilir.

## [0.3.0] - 2026-10-28

3. aşama: hava durumu.

### Eklendi
- Hava durumu (Open-Meteo, ücretsiz ve anahtarsız): görev çubuğunda simge + sıcaklık + yer adı; ipucunda açıklama, hissedilen, nem, rüzgâr ve sonraki 6 saat.
- Köy / mahalle / kasaba / ilçe araması: Open-Meteo (GeoNames) ve OpenStreetMap (Nominatim) birlikte; aynı yer tek sonuç. Seçilen noktanın koordinatı kullanılır.
- Görünen adı değiştirme (ör. uzun mahalle adı yerine kısa ad).
- 15 dakikada bir yenileme; hata olursa 2 dakikada bir yeniden deneme; uykudan uyanınca ve internet gelince hemen yenileme. Menüde "Hava durumunu yenile".
- Önbellek: açılışta son rapor hemen gösterilir; 1 saatten eski veri soluk, 12 saatten eskisi gösterilmez.
- Lisans gereği kaynak satırı (Open-Meteo, OpenStreetMap, GeoNames) ve `THIRD-PARTY-NOTICES.md`.

## [0.2.0] - 2026-10-28

2. aşama: veriler.

### Eklendi
- Ağ hızı (↑ yükleme, ↓ indirme): Windows bağdaştırıcı sayaçlarından (GetIfTable2), bağdaştırıcı başına fark. "Otomatik" yalnızca bağlı fiziksel bağdaştırıcıları toplar; filtre arabirimleri, sanal ve geri döngü bağdaştırıcıları çift sayımı önlemek için atlanır. Windows istatistikleriyle karşılaştırılarak doğrulandı.
- Günlük trafik geçmişi (`traffic-history.json`): dakikada bir, oturum kapanışında ve çıkışta atomik yazılır.
- TrafficMonitor geçmişinin içe aktarılması (ilk açılışta bir kez; TrafficMonitor dosyasına yalnızca okumak için dokunulur). Aynı gün iki kaynakta varsa büyük değer alınır.
- GPU kullanımı (GPU başına 3D motor toplamı, en yükseği) ve disk etkinliği; Türkçe Windows'ta da çalışan İngilizce sayaç yolları. Yalnızca gösterge açıkken ölçülür.
- Klavye kilit tuşları göstergesi (CAPS / NUM / SCRL; açıkken parlak).
- Ayarlar penceresi: göstergeler, ağ bağdaştırıcısı seçimi, bugünkü trafik, Windows ile başlat, tam ekranda gizle, tepsiye uzaklık. Değişiklikler hemen uygulanır. Menüden ve tepsi simgesine çift tıklayarak açılır.
- `PulseMonitor.exe --exit`: çalışan kopyayı veri kaybı olmadan kapatır.
- `tools/publish.ps1`: `%LOCALAPPDATA%\Programs\PulseMonitor` klasörüne kurulum; başlangıç kaydını kurulu sürüme taşır.

### Değişti
- Ölçümler arka plan iş parçacığında yapılıyor; arayüz takılmaz.
- İşlemciyle çizim: özel bellek 89 → 51 MB, iş parçacığı 38 → 19.

## [0.1.0] - 2026-10-28

Sıfırdan yeniden yazım (eski çalışma tamamen kaldırıldı). 1. aşama: iskelet.

### Eklendi
- Görev çubuğu penceresi: görev çubuğuna bağlı (sahibi `Shell_TrayWnd`), sistem tepsisinin hemen soluna otomatik yerleşir; tepsi genişleyip daralınca takip eder.
- Tam ekran uygulamada (oyun, video, sunum) gizlenme; menüden açılıp kapatılabilir.
- Otomatik gizlenen görev çubuğu ekran dışına kayınca pencere de gizlenir.
- Explorer yeniden başladığında görev çubuğuna ve tepsiye yeniden bağlanma.
- Sistem tepsisi simgesi ve ortak sağ tık menüsü (Görev Yöneticisi, Windows ile başlat, Tam ekranda gizle, Çıkış).
- Çift tıklama: Görev Yöneticisi.
- Tek kopya çalışma, dosya günlüğü, bozuk ayar dosyasına karşı yedekleyip varsayılana dönme.
- Görev çubuğu açık/koyu temasına göre yazı rengi.
- Yüksek DPI (Per-Monitor V2) desteği.
- Geçici içerik: CPU ve RAM yükü (`% 12` biçiminde).
- Titremesiz gösterim: rakamlar sabit genişlikte, değer sütunu en geniş hâline (`% 100`) sabit; değer değişince pencere boyutu ve konumu değişmez.
