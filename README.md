# 🏭 Gecikme Takip Sistemi

Modern RFID teknolojisi kullanarak üretim süreçlerindeki gecikmeleri takip eden gelişmiş bir Windows Forms uygulaması.

## 📋 İçindekiler

- [Özellikler](#özellikler)
- [Teknolojiler](#teknolojiler)
- [Kurulum](#kurulum)
- [Kullanım](#kullanım)
- [Veritabanı Yapısı](#veritabanı-yapısı)
- [RFID Konfigürasyonu](#rfid-konfigürasyonu)
- [Ekran Görüntüleri](#ekran-görüntüleri)
- [Katkıda Bulunma](#katkıda-bulunma)
- [Lisans](#lisans)

## ✨ Özellikler

### 🔐 Kullanıcı Yönetimi
- **RFID Kart ile Giriş**: Güvenli ve hızlı kullanıcı doğrulama
- **Admin Paneli**: Yönetici yetkileri ile tüm kayıtları görüntüleme
- **Kullanıcı Profilleri**: Kişiselleştirilmiş deneyim

### 📊 Gecikme Takibi
- **Gerçek Zamanlı İzleme**: Anlık gecikme süresi takibi
- **Ürün Bazlı Kategorizasyon**: Farklı ürün türleri için ayrı takip
- **Sebep Analizi**: Gecikme nedenlerinin detaylı analizi
- **Süre Hesaplama**: Otomatik geçen süre hesaplaması

### 🎯 Kullanıcı Arayüzü
- **Modern Tasarım**: Kullanıcı dostu arayüz
- **Responsive Layout**: Farklı ekran boyutlarına uyum
- **Çift Panel Görünümü**: Ürün ve işlem seçimi için optimize edilmiş
- **Gerçek Zamanlı Güncelleme**: Otomatik veri yenileme

### 📈 Raporlama ve Analiz
- **Filtreleme Sistemi**: Tarih, ürün ve sebep bazlı filtreleme
- **Dashboard**: Toplam geçen süre görüntüleme
- **Aktif/Pasif Durum Takibi**: İşlem durumlarının anlık izlenmesi
- **Detaylı Raporlar**: Kapsamlı analiz raporları

## 🛠️ Teknolojiler

### Backend
- **.NET Framework 4.8**: Güçlü Windows uygulama geliştirme platformu
- **C#**: Modern programlama dili
- **Windows Forms**: Kullanıcı arayüzü framework'ü

### Veritabanı
- **Microsoft SQL Server**: Güvenilir veritabanı yönetim sistemi
- **ADO.NET**: Veritabanı erişim teknolojisi

### RFID Teknolojisi
- **Impinj Octane SDK**: Profesyonel RFID okuyucu entegrasyonu
- **LLRP Protokolü**: Standart RFID iletişim protokolü

### Diğer Teknolojiler
- **SSH.NET**: Güvenli bağlantı protokolleri
- **System.Threading**: Asenkron işlem yönetimi

## 🚀 Kurulum

### Gereksinimler
- Windows 10/11
- .NET Framework 4.8
- Microsoft SQL Server 2019 veya üzeri
- Impinj RFID Okuyucu (IP: 192.168.0.20)

### Adım Adım Kurulum

1. **Projeyi İndirin**
   ```bash
   git clone https://github.com/kullaniciadi/gecikme-takip-sistemi.git
   cd gecikme-takip-sistemi
   ```

2. **Veritabanını Kurun**
   ```sql
   -- SQL Server Management Studio'da çalıştırın
   CREATE DATABASE StajProjem;
   USE StajProjem;
   
   -- Tabloları oluşturun (schema.sql dosyasından)
   ```

3. **Bağlantı Ayarlarını Yapılandırın**
   ```csharp
   // Form1.cs dosyasında CONNECTION_STRING'i güncelleyin
   private const string CONNECTION_STRING = "Data Source=SUNUCU_ADI;Initial Catalog=StajProjem;Integrated Security=True";
   ```

4. **RFID Okuyucu Ayarlarını Yapın**
   ```csharp
   // Form1.cs dosyasında READER_IP_ADDRESS'i güncelleyin
   private const string READER_IP_ADDRESS = "RFID_OKUYUCU_IP";
   ```

5. **Projeyi Derleyin ve Çalıştırın**
   ```bash
   # Visual Studio'da F5 tuşuna basın veya
   dotnet build
   dotnet run
   ```

## 📖 Kullanım

### Giriş Yapma
1. RFID kartınızı okuyucuya yaklaştırın
2. Sistem otomatik olarak kullanıcıyı tanıyacak
3. Ana menüye yönlendirileceksiniz

### Yeni Gecikme Kaydı Oluşturma
1. **YENİ İŞLEM** butonuna tıklayın
2. Sol panelden ürün seçin
3. Alt panelden gecikme sebebini seçin
4. **KAYIT OLUŞTUR** butonuna tıklayın

### Tamamlanmamış İşlemleri Bitirme
1. Sağ panelden tamamlanmamış işlemi seçin
2. **BİTİR** butonuna tıklayın
3. İşlem otomatik olarak tamamlanacak

### Admin Paneli Kullanımı
1. Admin RFID kartı ile giriş yapın
2. Tüm kayıtları görüntüleyin
3. Filtreleme seçeneklerini kullanın
4. Aktif işlemleri bitirin

## 🗄️ Veritabanı Yapısı

### Ana Tablolar

#### Kullanıcılar
```sql
CREATE TABLE Kullanıcılar (
    Id INT PRIMARY KEY IDENTITY(1,1),
    AdSoyad NVARCHAR(100),
    RFID NVARCHAR(50)
);
```

#### Ürünler
```sql
CREATE TABLE TblUrunler (
    ID INT PRIMARY KEY IDENTITY(1,1),
    Urunler NVARCHAR(100)
);
```

#### Gecikme Nedenleri
```sql
CREATE TABLE TblGecikmeNedenleri (
    No INT PRIMARY KEY IDENTITY(1,1),
    Nedenler NVARCHAR(200)
);
```

#### Gecikme Kayıtları
```sql
CREATE TABLE TblGecikmeKaydı (
    ID INT PRIMARY KEY IDENTITY(1,1),
    KullaniciID INT,
    UrunID INT,
    GecikmeID INT,
    Baslangic DATETIME,
    Bitis DATETIME,
    FOREIGN KEY (KullaniciID) REFERENCES Kullanıcılar(Id),
    FOREIGN KEY (UrunID) REFERENCES TblUrunler(ID),
    FOREIGN KEY (GecikmeID) REFERENCES TblGecikmeNedenleri(No)
);
```

## 🔧 RFID Konfigürasyonu

### Okuyucu Ayarları
- **IP Adresi**: 192.168.0.20
- **Güç Seviyesi**: 15.0 dBm
- **Alma Hassasiyeti**: -30.0 dBm
- **Anten Durumu**: Tüm antenler aktif

### Admin Kartı
```csharp
private const string ADMIN_RFID_TAG = "1000 4084 0000 0000 0000 0122 142F C047";
```

### Güvenlik
- RFID kartları benzersiz tanımlayıcılarla korunur
- Admin erişimi özel kart ile sınırlıdır
- Bağlantı kesintilerinde otomatik uyarı sistemi

## 🎥 Demo Videosu

Sistemin nasıl çalıştığını görmek için aşağıdaki demo videosunu izleyebilirsiniz:

[![Gecikme Takip Sistemi Demo](C:\Users\doruk\OneDrive\Resimler\Resim.png)](https://www.youtube.com/watch?v=2-fDL4E4HgU)

**Video İçeriği:**
- 📱 Giriş ekranı ve RFID kart ile giriş
- 🏠 Ana menü navigasyonu
- 📊 Çift panel ekranında ürün ve sebep seçimi
- ⏱️ Gerçek zamanlı süre takibi
- 👨‍💼 Admin paneli ve filtreleme özellikleri
- 🔄 Tamamlanmamış işlemleri bitirme

> 💡 **Not:** Video ID'sini gerçek YouTube video ID'niz ile değiştirmeyi unutmayın!

## 📱 Ekran Görüntüleri

### Giriş Ekranı
- Büyük ve merkezi başlık
- RFID kart tarama talimatı
- Modern ve temiz tasarım

### Ana Menü
- Kullanıcı karşılama mesajı
- Hızlı erişim butonları
- Navigasyon seçenekleri

### Çift Panel Ekranı
- Sol panel: Ürün seçimi
- Alt panel: Gecikme sebepleri
- Sağ panel: Tamamlanmamış işlemler
- Gerçek zamanlı süre takibi

### Admin Paneli
- Kapsamlı veri görüntüleme
- Gelişmiş filtreleme seçenekleri
- Dashboard ile istatistikler

## 🤝 Katkıda Bulunma

1. Bu repository'yi fork edin
2. Yeni bir branch oluşturun (`git checkout -b feature/yeni-ozellik`)
3. Değişikliklerinizi commit edin (`git commit -am 'Yeni özellik eklendi'`)
4. Branch'inizi push edin (`git push origin feature/yeni-ozellik`)
5. Pull Request oluşturun

### Geliştirme Kuralları
- Kod standartlarına uyun
- Unit testler yazın
- Dokümantasyonu güncelleyin
- Code review sürecine katılın

## 📄 Lisans

Bu proje [MIT Lisansı](LICENSE) altında lisanslanmıştır.

## 📞 İletişim

- **Geliştirici**: [Adınız]
- **E-posta**: [email@example.com]
- **Proje Linki**: [https://github.com/kullaniciadi/gecikme-takip-sistemi](https://github.com/kullaniciadi/gecikme-takip-sistemi)

## 🙏 Teşekkürler

- Impinj teknolojileri için
- .NET Framework ekibi için
- Açık kaynak topluluğu için

---

⭐ Bu projeyi beğendiyseniz yıldız vermeyi unutmayın!
