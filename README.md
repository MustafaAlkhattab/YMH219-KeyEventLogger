# Key Event Studio

YMH219 için Windows Forms klavye olayları laboratuvarı. Açık olan yakalama alanında tuş adını, zamanı, olay türünü ve değiştiricileri bir tabloda gösterir.

**Öğrenci** · Mustafa Alkhatta, 250541607

## Çalıştırma

Windows 10/11 ve .NET 10 SDK gerekir. Visual Studio'da `KeyEventLogger.csproj` açılıp F5 ile de çalıştırılabilir.

```powershell
git clone https://github.com/MustafaAlkhattab/YMH219-KeyEventLogger.git
cd YMH219-KeyEventLogger
dotnet run -c Release
```

## Kullanım

1. **Başlat** ile yakalama alanı odaklanır. Harf, F4, Home veya F11 gibi tuşları deneyin.
2. Varsayılan görünümde her fiziksel basma bir satırdır. Tuşu basılı tutmak yeni satır üretmez. Tuş bırakıldıktan sonra yeniden basılabilir.
3. **Bırakma olayını da göster** seçeneği ayrı bırakma olayını tabloya ekler. Toplam basma sayacı bu ayardan etkilenmez.
4. **Durdur** kaydı kapatır. **Temizle** tabloyu ve sayacı sıfırlar. **TXT kaydet** görünen kayıtları UTF-8 tablosuna yazar.

## Teknoloji ve sınıflar

C# / .NET 10, Windows Forms, yerel `KeyDown` ve `KeyUp` olayları, UTF-8 dosya yazımı. Harici NuGet paketi yoktur. `KeyPad` giriş odağını, `CaptureSession` basılı tuş kümesini ve kayıtları, `KeyWindow` arayüz ve dosya seçimini yönetir.

## Ekran görüntüleri

![Varsayılan tek basma görünümü](docs/01-presses.png)
![İsteğe bağlı bırakma olayı](docs/02-release.png)

## Sınırlar ve doğrulama

Yakalama yalnız bu uygulamada ve yakalama alanı odaktayken gerçekleşir. Odağı kaybedince kayıt durur. Tab, Windows kısayolları ve Alt+F4 normal işlevlerini sürdürür. Kayıtlar bellekte son 1000 satırla sınırlıdır. Dışa aktarma kullanıcı dosya seçtiğinde gerçekleşir.

Windows Release derlemesi, basılı tuş tekrarı, bırakma seçeneği ve UTF-8 TXT çıktısı kontrol edildi.
