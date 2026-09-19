# VoiceDictation (by TripleSS)

**VoiceDictation**, Windows işletim sisteminde çalışan; herhangi bir metin alanında (Discord, Not Defteri, Visual Studio / VS Code, Word, WhatsApp, tarayıcılar vb.) global kısayola basılarak konuşulan sesleri **tamamen yerel ve çevrimdışı (offline)** olarak metne döküp imlecin bulunduğu konuma anında yazan yüksek performanslı bir sesli dikte aracıdır.

.NET 10 (C#), WPF mimarisi ve gömülü Whisper C++ çıkarım kütüphaneleriyle sıfırdan geliştirilmiştir.

---

## Öne Çıkan Özellikler

- **%100 Yerel ve Gizli:** Sesleriniz ve transkriptleriniz hiçbir harici sunucuya veya buluta gönderilmez. Tamamen bilgisayarınızdaki Whisper GGML modeliyle yerel olarak işlenir.
- **Sistem Genelinde Kusursuz Enjeksiyon:** Düşük seviyeli Win32 `SendInput` (Doğrudan Unicode Karakter Modu) veya Hibrit Pano-Takas yöntemleriyle imlecin aktif olduğu her yere metin yazar.
- **Odak Çalmayan Yüzen Kapsül HUD (`WS_EX_NOACTIVATE`):** Ekranın altında konumlanan, mikrofon ses dalgalarını gerçek zamanlı gösteren ve siz konuşurken hedef pencerenin (örneğin oyun veya kod editörü) klavye odağını asla bozmayan modern Antigravity Dark arayüzü.
- **Sistem Tepsisi / Gizli Simgeler (System Tray) Entegrasyonu:** Görev çubuğunun sağ altındaki gizli simgeler alanında sessizce çalışır. Çift tıklamayla ayarlar açılır, sağ tıkla kapsül gösterilir veya kapatılır.
- **Tek Dosya (Standalone Single-File) Dağıtımı:** Gömülü yönetilmeyen C++ kütüphaneleri (`whisper.dll`, `ggml-cpu-whisper.dll` vb.) ilk açılışta otomatik olarak ayıklanır. Karşı tarafta .NET Runtime, Visual C++ Redistributable veya ek hiçbir paket kurulu olması gerekmez; tek bir `.exe` doğrudan çalışır.
- **Gelişmiş Yapılandırma:**
  - Özelleştirilebilir Kısayol Tuşu (Varsayılan: `Insert` veya `F8`, `CapsLock`, `F7` vb.)
  - Tetikleme Modu (Bas-Konuş veya Aç/Kapa)
  - Transkripsiyon Dili (Türkçe, İngilizce, Çok Dilli Otomatik Algılama)
  - Mikrofon aygıtı ve Whisper model seçimi (`base`, `small` vb.)

---

## Mimari ve Katman Yapısı

```
VoiceDictation/
├── src/
│   ├── VoiceDictation.Core/              # Domain Modelleri, Durum Makinesi (FSM) ve Sözleşmeler
│   │   ├── Models/                      # DictationState, AudioChunk, TranscriptionResult, AppSettings
│   │   ├── State/                       # DictationStateMachine (Idle -> Listening -> Transcribing -> Injecting)
│   │   └── Interfaces/                  # Ses, Transkripsiyon, Klavye Kancası ve Enjeksiyon Arayüzleri
│   ├── VoiceDictation.Infrastructure/    # Donanım, Windows API ve Motor Uygulamaları
│   │   ├── Audio/                       # WasapiAudioCaptureService (16kHz mono PCM yeniden örnekleme)
│   │   ├── Transcription/               # WhisperTranscriptionEngine (Whisper.net GGML entegrasyonu)
│   │   ├── Input/                       # LowLevelKeyboardHookService (WH_KEYBOARD_LL), WindowsInputInjectionService
│   │   ├── Native/                      # Win32 P/Invoke tanımları, NativeLibraryBootstrapper
│   │   └── Configuration/               # JsonSettingsService (%APPDATA%\VoiceDictation\settings.json)
│   └── VoiceDictation.App/               # Sunum Katmanı (WPF / MVVM)
│       ├── ViewModels/                  # CapsuleViewModel, SettingsViewModel, RelayCommand
│       ├── Views/                       # CapsuleWindow, SettingsWindow
│       ├── Services/                    # TrayIconManager (NotifyIcon sistem tepsisi yöneticisi)
│       └── Styles/                      # Antigravity Dark renk paleti, buton ve ikon vektörleri
└── tests/
    └── VoiceDictation.Tests/            # xUnit Test Paketi (FSM geçişleri, Audio Resampling, Win32 Boyutları)
```

---

## Sistem Gereksinimleri

- **Geliştirici Ortamı:** Windows 10/11 x64, .NET 10 SDK
- **Kullanıcı Ortamı:** Windows 10/11 x64 (Tek dosya standalone sürüm için ek hiçbir kuruluma gerek yoktur)

---

## Kurulum ve Çalıştırma

### 1. Kaynak Koddan Derleme ve Çalıştırma

```powershell
# Depoyu klonlayın
git clone https://github.com/triplessbaba-systems/VoiceDictation--by-TripleSS-.git
cd VoiceDictation--by-TripleSS-

# Çözümü derleyin
dotnet build VoiceDictation.slnx

# Testleri çalıştırın (9/9 Birim Testi)
dotnet test VoiceDictation.slnx

# Uygulamayı başlatın
dotnet run --project src/VoiceDictation.App
```

### 2. Tek Dosya (.EXE) Olarak Yayınlama (Standalone)

Başka bilgisayarlara doğrudan atabileceğiniz, içine tüm çalışma zamanı ve yapay zeka motoru gömülü tek bir `.exe` üretmek için:

```powershell
dotnet publish src/VoiceDictation.App/VoiceDictation.App.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o ./publish-standalone
```

Üretilen çalıştırılabilir dosya `./publish-standalone/VoiceDictation.App.exe` konumunda hazır olacaktır (~174 MB).

---

## Kullanım Kılavuzu

1. **Başlatma:** Uygulamayı çalıştırdığınızda ekranın alt kısmında şeffaf bir kapsül belirecek ve sağ alttaki gizli simgelere mikrofon ikonu eklenecektir.
2. **Dikte Etme:** 
   - İstediğiniz herhangi bir metin alanına (örneğin Not Defteri veya Discord mesaj kutusu) tıklayın.
   - Tanımlı kısayol tuşuna (**`Insert`** veya **`F8`**) basılı tutun ve konuşun.
   - Tuşu bıraktığınızda sistem sesinizi yerel modelle çözümler ve imlecin bulunduğu yere anında yazar.
3. **Ayarlar:** Sağ alttaki gizli simgelerde bulunan mikrofon ikonuna çift tıklayarak veya sağ tıklayıp **Ayarlar** seçeneğini kullanarak kısayol tuşunu, dili ve mikrofon aygıtını değiştirebilirsiniz.

---

## Yapılandırma Dosyası

Tüm tercihler `%APPDATA%\VoiceDictation\settings.json` dosyasında saklanır:

```json
{
  "ShortcutKey": "Insert",
  "TriggerMode": 0,
  "InjectionMode": 0,
  "SelectedLanguage": "tr",
  "ModelName": "ggml-base.bin",
  "SilenceThreshold": 0.015,
  "AudioInputDeviceIndex": -1
}
```

---

## Lisans

Bu proje MIT Lisansı ile lisanslanmıştır.
