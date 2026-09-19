# VoiceDictation (by TripleSS)

**VoiceDictation**, Windows üzerinde çalışan; herhangi bir metin alanında (Discord, Not Defteri, Visual Studio, Word, WhatsApp, tarayıcılar vb.) kısayol tuşuna basarak konuştuğunuz sesleri **tamamen yerel ve çevrimdışı (offline)** olarak metne döküp imlecin olduğu yere anında yazan sesli dikte aracıdır.

Sesleriniz hiçbir sunucuya veya buluta gitmez, tamamen bilgisayarınızda yerel Whisper modeli ile işlenir.

---

## Öne Çıkan Özellikler

- **%100 Yerel ve Güvenli:** Ses kaydı ve çözümleme tamamen bilgisayarınızda gerçekleşir, internet gerektirmez.
- **Her Yerde Çalışır:** Düşük seviyeli klavye enjeksiyonu sayesinde imlecin bulunduğu her uygulamada (oyun içi sohbetler, editörler, mesajlaşma uygulamaları) doğrudan yazar.
- **Odak Çalmayan Yüzen Kapsül Arayüzü:** Ekranın altında beliren, ses dalgalarını anlık gösteren ama aktif pencerenizin odağını asla bozmayan modern koyu tema widget'ı.
- **Sistem Tepsisi (Gizli Simgeler):** Görev çubuğunun sağ altındaki bildirim alanında sessizce bekler. Çift tıklayarak ayarları açabilir, sağ tıklayarak kapatabilirsiniz.
- **Kurulumsuz Tek Dosya (.exe):** Ek hiçbir çalışma zamanı veya kütüphane kurmadan tek bir `.exe` dosyasını çalıştırabilirsiniz.
- **Geniş Özelleştirme:**
  - Kısayol tuşunu belirleme (Varsayılan: `Insert` veya `F8`)
  - Bas-Konuş veya Aç/Kapa modu
  - Türkçe, İngilizce veya Otomatik Dil Algılama
  - Mikrofon aygıtı seçimi

---

## Nasıl Kullanılır?

1. **Uygulamayı Açın:** Çalıştırıldığında ekranın alt kısmında minimal bir ses kapsülü görünür ve sağ alttaki gizli simgelere mikrofon simgesi yerleşir.
2. **Konuşun:**
   - Yazmak istediğiniz metin alanına (Not Defteri, Discord vb.) tıklayın.
   - Tanımlı kısayola (**`Insert`** veya **`F8`**) basılı tutarak konuşun.
   - Tuşu bıraktığınız anda sesiniz çözümlenir ve metin imlecin olduğu yere yazılır.
3. **Ayarlar:** Sağ alttaki mikrofon simgesine sağ tıklayıp **Ayarlar**'ı seçerek kısayol tuşunuzu veya dilinizi dilediğiniz gibi değiştirebilirsiniz.

---

## Kurulum ve Derleme

### Tek Dosya (.exe) Olarak Derleme (Standalone)

Başka bilgisayarlara doğrudan atabileceğiniz, tüm bağımlılıkları içine gömülü tek dosya sürümünü üretmek için:

```powershell
dotnet publish src/VoiceDictation.App/VoiceDictation.App.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o ./publish-standalone
```

Oluşan dosya: `./publish-standalone/VoiceDictation.App.exe`

### Kaynak Koddan Çalıştırma

```powershell
dotnet build VoiceDictation.slnx
dotnet run --project src/VoiceDictation.App
```

---

## Lisans

Bu proje MIT Lisansı ile lisanslanmıştır.
