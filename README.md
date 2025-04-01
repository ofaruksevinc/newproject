# E-Ticaret Sipariş API'si

Bu proje, e-ticaret platformları için temel bir sipariş yönetim sistemi sağlayan bir ASP.NET Core Web API'dir.

## Özellikler

- **Sipariş Yönetimi**:
  - Yeni sipariş oluşturma
  - Siparişleri listeleme
  - Sipariş detaylarını görüntüleme
  - Sipariş silme

- **Stok Yönetimi**:
  - Otomatik stok kontrolü
  - Sipariş oluşturulduğunda stoktan düşme
  - Sipariş silindiğinde stoğa geri ekleme

## Teknolojiler

- ASP.NET Core 8.0
- Entity Framework Core
- SQL Server 
- Swagger / OpenAPI

## Başlangıç

### Gereksinimler

- .NET 8.0 SDK veya üstü
- Bir veritabanı: SQL Server veya SQLite

### Kurulum

1. Projeyi klonlayın:
   ```bash
   git clone https://github.com/yourusername/OrderManagementAPI.git
   cd OrderManagementAPI
   ```

2. Veritabanı bağlantı dizesini ayarlayın:
   `appsettings.json` dosyasında veritabanı bağlantı dizesini güncelleyin.

   **SQLite için** (önerilen - MacOS/Linux için):
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Data Source=ordermanagement.db"
   }
   ```

   **SQL Server için** (Windows):
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=OrderManagementDb;Trusted_Connection=True;MultipleActiveResultSets=true"
   }
   ```

3. Veritabanı migrasyonlarını uygulayın:
   ```bash
   dotnet ef database update
   ```

4. Uygulamayı çalıştırın:
   ```bash
   dotnet run
   ```

5. Tarayıcınızda Swagger UI'yi açın:
   ```
   https://localhost:5001/swagger
   ```
   veya
   ```
   http://localhost:5000/swagger
   ```

## API Endpointleri

### Siparişler

| Metod | Endpoint | Açıklama |
|-------|----------|----------|
| GET | `/api/Orders/user/{userId}` | Kullanıcının siparişlerini getirir |
| GET | `/api/Orders/{id}` | Belirli bir siparişin detaylarını getirir |
| POST | `/api/Orders` | Yeni bir sipariş oluşturur |
| DELETE | `/api/Orders/{id}` | Belirli bir siparişi siler |

### Örnek Kullanım

#### Yeni Sipariş Oluşturma

```http
POST /api/Orders
Content-Type: application/json

{
  "userId": "user123",
  "shippingAddress": "Örnek Adres, İstanbul, Türkiye",
  "items": [
    {
      "productId": 1,
      "quantity": 2
    },
    {
      "productId": 3,
      "quantity": 1
    }
  ]
}
```

#### Kullanıcı Siparişlerini Listeleme

```http
GET /api/Orders/user/user123
```

#### Sipariş Detaylarını Görüntüleme

```http
GET /api/Orders/5
```

#### Sipariş Silme

```http
DELETE /api/Orders/5
```

## Proje Yapısı

- **src/Core**: Domain modelleri, DTOs ve servis arayüzleri
- **src/Infrastructure**: Veritabanı erişimi ve servis implementasyonları
- **src/API**: API kontrolcüleri ve genel yapılandırma

## Geliştirme

### Yeni Migrasyon Ekleme

```bash
dotnet ef migrations add MigrasyonAdi
dotnet ef database update
```

### Testler

```bash
dotnet test
```
