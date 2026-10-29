# Üçüncü Taraf Bildirimleri

PulseMonitor kendi kodundan oluşur; başka projeden kod kopyalanmamıştır. Kullanılan veri servisleri ve
lisans gereği gösterilen kaynaklar aşağıdadır. Kaynak satırı ayarlar penceresinin hava durumu bölümünde de yer alır.

| Servis | Kullanım | Lisans / koşul |
|---|---|---|
| [Open-Meteo](https://open-meteo.com) | Hava tahmini ve konum araması | Veri CC BY 4.0; ücretsiz API ticari olmayan kullanım içindir, günlük 10 000 istek sınırı |
| [GeoNames](https://www.geonames.org) (Open-Meteo konum araması üzerinden) | Köy / kasaba / ilçe adları | CC BY 4.0 |
| [OpenStreetMap Nominatim](https://nominatim.org) | Mahalle ve köy araması | Veri © OpenStreetMap katkıcıları, ODbL; kullanım koşulu: en fazla saniyede 1 istek, tanımlayıcı User-Agent, toplu sorgu yok |
| [ipify](https://www.ipify.org) | Dış IP adresi (yalnızca "Dış IP" öğesi açıkken) | Ücretsiz, açık kaynak servis; kayıt tutmadığını beyan eder |

Kullanım hacmi: dış IP 15 dakikada bir ve ağ değişince; hava durumu 15 dakikada bir (günde ~100 istek); konum araması yalnızca kullanıcı "Ara"ya bastığında.

## Programla dağıtılan simgeler

| Kaynak | Nerede | Lisans |
|---|---|---|
| [Microsoft Fluent Emoji](https://github.com/microsoft/fluentui-emoji) (3D) | `Assets/Weather/*.png` — "Gerçekçi 3D" hava simgeleri, 64 px'e küçültüldü | MIT, Copyright (c) Microsoft Corporation |
| [Microsoft Fluent UI System Icons](https://github.com/microsoft/fluentui-system-icons) | `Resources/WeatherIconsFluent.xaml` — "Düz çizgi" (regular) ve "Monokrom" (filled) hava simgeleri, SVG yolları XAML'e çevrildi | MIT, Copyright (c) 2020 Microsoft Corporation |
| [Meteocons](https://github.com/basmilius/meteocons) (@meteocons/svg ve svg-static 0.1.0; fill, flat, line) | `Assets/WeatherAnim/Meteocons*/` — animasyonlu SVG'lerden üretilmiş film şeridi PNG'leri ve sabit PNG'ler | MIT, Copyright (c) 2020-present Bas Milius |
| [Makin-Things weather-icons](https://github.com/Makin-Things/weather-icons) (animated, static) | `Assets/WeatherAnim/MakinThings/` — aynı yöntemle | MIT, Copyright (c) 2019 Custom cards for Home Assistant |

Fluent dosyaları `tools/fetch_icons.ps1`, Meteocons ve Makin-Things dosyaları `tools/render_icons.ps1` ile yeniden üretilebilir.
Telif satırları ilgili depoların LICENSE dosyalarındaki gibidir.

MIT Lisansı (dört proje için aynı metin):

> Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
>
> The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
>
> THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

TrafficMonitor (MIT, © zhongyang219) yalnızca davranış referansıdır; kodu alınırsa telif ve lisans metni buraya eklenir.
