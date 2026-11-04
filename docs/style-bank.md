# PulseMonitor Stil Bankası

`Resources/StyleBank.xaml`, uygulamanın WPF bileşenleri için tek yeniden-kullanım noktasıdır. Yeni ekranlar renk, kenar yarıçapı veya boşluğu kendi içinde tanımlamak yerine bu anlamsal stilleri kullanır. Renkler `Resources/Theme.xaml` içindeki `Ui.*` belirteçlerinden gelir; bu nedenle açık/koyu tema ve marka güncellemeleri bileşenleri değiştirmeden uygulanır.

| İhtiyaç | Stil anahtarı |
| --- | --- |
| Pencere başlığı | `Ui.WindowTitle` |
| Bölüm başlığı | `Ui.SectionHeading` |
| Normal / ikincil metin | `Ui.Body` / `Ui.Caption` |
| Form etiketi | `Ui.FormLabel` |
| Ana / ikincil / ikon eylemi | `Ui.PrimaryButton` / `Ui.QuietButton` / `Ui.IconButton` |
| Metin alanı / seçim / seçenek / sayı değeri | `Ui.TextField` / `Ui.Select` / `Ui.Toggle` / `Ui.ValueSlider` |
| Kart / iç kart / bilgi kutusu | `Ui.Card` / `Ui.InsetCard` / `Ui.Notice` |
| Liste / durum etiketi / boş durum | `Ui.List` / `Ui.StatusPill` / `Ui.EmptyState` |
| Bölümler arası ayraç | `Ui.Divider` |

## Kullanım

```xml
<Border Style="{StaticResource Ui.Card}">
  <StackPanel>
    <TextBlock Text="Başlık" Style="{StaticResource Ui.SectionHeading}" />
    <TextBlock Text="Açıklama" Style="{StaticResource Ui.Caption}" />
    <Button Content="Kaydet" Style="{StaticResource Ui.PrimaryButton}" />
  </StackPanel>
</Border>
```

Bir bileşen için yeni görünüm gerekirse önce yeni bir `Ui.*` semantik stil eklenir; ekran içinde renk veya kontrol şablonu kopyalanmaz.

`TextBox`, `ComboBox`, `CheckBox`, `Slider`, `ListBox`, `DatePicker` ve `RadioButton` için uygulama-geneli varsayılan stiller de bu bankaya bağlıdır. İstisna gereken durumlarda açıkça ilgili `Ui.*` anahtarını kullanın.
