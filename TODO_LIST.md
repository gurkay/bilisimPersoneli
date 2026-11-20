# Bilişim Personeli — Geliştirme TODO Listesi

Sınav için temiz bir ASP.NET Core 8 MVC + Clean Architecture CRUD uygulaması (AutoMapper, API/JWT, soft delete, audit log ve rol/onay akışı dahil).

**Kapsam:** Departman, Personel, İzin yönetimi  
**Kimlik:** ASP.NET Core Identity (cookie + JWT API)  
**Veritabanı:** SQL Server (Docker)  
**Arayüz:** Razor Views + Bootstrap 5  
**Ortam:** Docker Compose

---

## Bilinçli olarak yapılmayacaklar (karmaşıklık azaltma)

- [x] MediatR / CQRS **kullanılmayacak**
- [x] Dosya yükleme **yapılmayacak**
- [x] Unit test projesi **ilk teslimde yok**

---

## 1. Proje iskeleti ve katmanlar

- [x] Solution oluştur: `BilisimPersoneli.sln`
- [x] Klasör yapısı:
  ```text
  src/
    BilisimPersoneli.Domain/          // Entity'ler, enum'lar, base entity
    BilisimPersoneli.Application/     // Interface'ler, DTO'lar, AutoMapper profilleri, servis sözleşmeleri
    BilisimPersoneli.Infrastructure/  // EF Core, Identity, DbContext, Repository/Service impl, JWT
    BilisimPersoneli.Web/             // MVC Controllers, Razor Views, API Controllers, Program.cs
  docker-compose.yml
  README.md
  ```
- [x] Proje referansları:
  - `Web` → `Application`, `Infrastructure`
  - `Infrastructure` → `Application`, `Domain`
  - `Application` → `Domain`
  - `Domain` → (hiçbir katmana bağımlı değil)
- [x] NuGet paketleri:
  - Web: `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.AspNetCore.Authentication.JwtBearer`
  - Infrastructure: `Microsoft.EntityFrameworkCore.SqlServer`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
  - Application: `AutoMapper.Extensions.Microsoft.DependencyInjection`

---

## 2. Domain entity’leri

### 2.1 Base / ortak alanlar
- [x] `BaseEntity` (veya benzeri):
  - Soft delete: `IsDeleted` (bool), `DeletedAt` (DateTime?)
  - Audit log alanları: `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`
- [x] Departman, Personel, Izin bu base’den türesin

### 2.2 Enum’lar
- [x] `IzinTuru`: Yillik, Mazeret, Ucretsiz
- [x] `IzinDurumu`: Beklemede, Onaylandi, Reddedildi

### 2.3 Entity’ler
- [x] **Departman**
  - `Id` (int)
  - `Ad` (string, zorunlu, benzersiz)
  - `Aciklama` (string?, opsiyonel)
  - Navigation: `ICollection<Personel>`
- [x] **Personel**
  - `Id` (int)
  - `Ad`, `Soyad` (zorunlu)
  - `Email` (zorunlu, benzersiz)
  - `Telefon` (opsiyonel)
  - `Maas` (decimal, zorunlu, >= 0) — personel maaşı
  - `IseGirisTarihi` (DateTime)
  - `DepartmanId` (FK)
  - `IdentityUserId` (Personel ↔ Identity User köprüsü; her Personel bir kullanıcıdır; rol Identity’de tutulur, Domain’de `Personel.Rol` yok)
  - Navigation: `Departman`, `ICollection<Izin>`
- [x] **Izin**
  - `Id` (int)
  - `PersonelId` (FK)
  - `BaslangicTarihi`, `BitisTarihi` (DateTime)
  - `IzinTuru` (enum)
  - `Durum` (enum, varsayılan: Beklemede)
  - `Aciklama` (string?, opsiyonel)
  - Onay alanları: `OnaylayanUserId` (string?), `OnayTarihi` (DateTime?), `OnayNotu` (string?)
  - Navigation: `Personel`
- [x] **AuditLog** (ayrı tablo — kim ne yaptı kaydı)
  - `Id`
  - `EntityName`, `EntityId`
  - `Action` (Create / Update / SoftDelete / Restore / Approve / Reject)
  - `UserId`, `UserName`
  - `Timestamp`
  - `Details` (kısa özet / JSON string, sade tutulacak)

### 2.4 İş kuralları (Domain / Application seviyesinde)
- [x] `BitisTarihi >= BaslangicTarihi`
- [x] Personel email benzersiz
- [x] Personel `Maas >= 0` (negatif maaş kabul edilmez)
- [x] Departman adı benzersiz
- [x] Soft delete: fiziksel silme yok; `IsDeleted = true`
- [x] Listelerde varsayılan olarak silinmiş kayıtlar gelmez (global query filter)
- [x] Departmanda aktif (silinmemiş) personel varken soft delete engellenebilir veya cascade soft-delete yapılmaz — bağlı kayıt uyarısı
- [x] Personelde aktif izin varken soft delete için uyarı / engel

---

## 3. Infrastructure (DbContext, Identity, migration, seed)

- [x] `AppDbContext` : `IdentityDbContext<ApplicationUser>` (`ApplicationUser : IdentityUser` + `IsApproved`)
- [x] `DbSet<Departman>`, `DbSet<Personel>`, `DbSet<Izin>`, `DbSet<AuditLog>`
- [x] Global query filter: `!IsDeleted` (Departman, Personel, Izin)
- [x] Soft delete override: `SaveChanges` içinde `Deleted` state → soft delete’e çevir
- [x] Audit: Create/Update/SoftDelete sırasında `AuditLog` kaydı yaz ( basitleştirilmiş interceptor veya `SaveChanges` override )
- [x] Fluent API / Data Annotations ile:
  - Unique index: `Departman.Ad`, `Personel.Email`
  - `Personel.Maas`: `decimal(18,2)`, zorunlu
  - FK ilişkileri (Restrict)
  - String max length’ler (örn. Ad 100, Email 256)
- [x] Identity servis kaydı (cookie auth + JWT)
- [x] İlk migration oluştur (`InitialCreate`)
- [x] Migration: `AddUserApprovalAndIsletmen` (`AspNetUsers.IsApproved`; mevcut kullanıcılar onaylı)
- [x] Uygulama start’ta: `Database.Migrate()`
- [x] Seed data:
  - Roller: `Admin`, `Yonetici`, `Isletmen`, `Personel`
  - Kullanıcılar:
    - `admin@local` → Admin
    - `yonetici@local` → Yonetici
    - `isletmen@local` → Isletmen
    - `personel@local` → Personel
  - Şifreler README’de belgele
  - Örnek 1–2 Departman + örnek Personel (smoke test için)

---

## 4. Application servisleri + AutoMapper

Generic repository zorunlu değil; entity bazlı net servisler yeterli.

- [x] AutoMapper kurulum:
  - DTO’lar: `DepartmanDto`, `PersonelDto` (Maas dahil), `IzinDto`, Create/Update DTO’ları
  - Mapping profile’lar (`Application/Mapping`)
  - DI: `AddAutoMapper`
- [x] Interface’ler:
  - `IDepartmanService`
  - `IPersonelService`
  - `IIzinService`
  - `IAuditLogService` (listeleme; sadece Admin)
  - `IDashboardService` (Admin/Yonetici/Isletmen özet rapor)
  - `IUserManagementService` (Register, liste, onay/red, rol atama)
- [x] DTO’lar: `RegisterDto`, `UserListItemDto`, `UserRolesUpdateDto`
- [x] Metotlar: Listele, GetById, Create, Update, SoftDelete (gerekirse Restore — Admin)
- [x] Liste ekranları için `GetPagedAsync` (sunucu taraflı sayfalama; §13)
- [x] Servislerde entity ↔ DTO dönüşümü AutoMapper ile
- [x] Liste ekranları için gerekli join/include:
  - Personel listesinde Departman adı + Maas
  - İzin listesinde Personel adı + durum
- [x] Validasyon: Data Annotations (+ serviste tarih / uniqueness / maaş kontrolleri)

---

## 5. Çoklu rol ve izin onay workflow’u

### 5.1 Roller ve yetkiler
- [x] **İki katman:** Kimlik/yetki = ASP.NET Identity (`ApplicationUser` + roller); İK kaydı = `Personel` + `IdentityUserId` köprüsü (rol Domain’de tutulmaz)
- [x] **Admin**: tüm CRUD, soft delete/restore, audit log, kullanıcı yönetimi / tüm rol atama, kayıt onayı
- [x] **Yonetici**: Personel tanımla / şifre ver; İzin görüntüleme; izin **Onayla / Reddet**; kayıt onayı; **sınırlı rol atama** (`Personel` / `Isletmen` / `Yonetici` — `Admin` atayamaz)
- [x] **Isletmen**: Departman/Personel/İzin operasyonları (liste + oluştur/düzenle); soft delete ve izin onayı yok; kullanıcı yönetimi yok
- [x] **Personel**: varsayılan rol; kendi izinlerini listeleme / yeni izin talebi oluşturma (Durum = Beklemede); Departman/Personel yönetimi yok
- [x] `AppRoles` sabitleri + `Staff` / `AdminAndYonetici` / `AdminAndIsletmen` yardımcı string’ler; `ICurrentUserService.IsStaff`
- [x] `Staff = Admin | Yonetici | Isletmen` — dashboard ve personel listeleri
- [x] **Tek birincil rol:** `UpdateRolesAsync` tek rol zorunlu (radio); çoklu rol checkbox kaldırılır
- [x] **Rol atama güvenlik:** Yonetici `Admin` atayamaz; son Admin düşürülemez; Admin dört rolün hepsini atar
- [x] Personel soft-delete → bağlı Identity hesabı kilitlenir / `IsApproved=false` (giriş kapanır)

### 5.2 İzin onay akışı (sade motor)
- [x] Personel izin oluşturur → `Durum = Beklemede`
- [x] Yonetici veya Admin: `Onayla` → `Onaylandi` + `OnaylayanUserId` / `OnayTarihi`
- [x] Yonetici veya Admin: `Reddet` → `Reddedildi` + opsiyonel `OnayNotu`
- [x] Onaylanmış/reddedilmiş izin Personel tarafından düzenlenemez
- [x] Sadece `Beklemede` kayıtlar onay/red aksiyonuna açık
- [x] MVC’de aksiyon butonları (Onayla / Reddet) rol bazlı görünsün
- [x] `[Authorize(Roles = "...")]` ile controller/action koruması

### 5.3 Kullanıcı kaydı, onay ve rol yaşam döngüsü
- [x] Public **Register** sayfası: Personel kaydı + Identity hesabı (`IsApproved = false`, varsayılan rol `Personel`)
- [x] Onaysız hesap MVC/API login’e alınmaz
- [x] Admin veya Yonetici: kayıt **Onayla** / **Reddet** (`Users/Pending`)
- [x] Admin/Yonetici/Isletmen personel oluştururken Identity hesabı **onaylı** açılır + varsayılan rol **`Personel`** + `IdentityUserId` set
- [x] Seed staff (`admin@local` / `yonetici@local` / `isletmen@local`): Personel satırı zorunlu değil (sistem hesabı); kural: *her Personel → user*, tersi zorunlu değil
- [x] **Kullanıcılar** paneli: Admin + Yonetici (Index/Roles); Admin tüm roller, Yonetici sınırlı (`Personel`/`Isletmen`/`Yonetici`)
- [x] Roles view: checkbox → **radio** (tek birincil rol); Yonetici’ye Admin seçeneği gizlenir
- [x] Rol yükseltme Personel kaydını silmez; `IsStaff` ile tüm listeye geçer

### 5.4 UI yansıması
- [x] Navbar menüleri role göre:
  - Admin: Dashboard, Departmanlar, Personeller, İzinler, İzin Onayları, Kullanıcılar, Kayıt Onayları, Audit Log
  - Yonetici: Dashboard, Personeller, İzinler, İzin Onayları, Kullanıcılar, Kayıt Onayları
  - Isletmen: Dashboard, Departmanlar, Personeller, İzinler
  - Personel: İzinlerim
- [x] Anonim menü: Giriş + Kayıt Ol
- [x] Yetkisiz erişimde 403 / yönlendirme
- [x] Yonetici navbar: **Kullanıcılar** linki (Index + Roles)

---

## 6. Web (Controllers, Views, Auth)

### 6.1 Auth (cookie — MVC)
- [x] `AccountController`: Login, Logout, ChangePassword, Register
- [x] Login View (email + şifre); Register View (Ad/Soyad/Email/Telefon/Departman/Şifre)
- [x] Login’de `IsApproved` kontrolü
- [x] Şifre değiştirme (tüm roller); personel oluştururken Staff şifre verir (onaylı hesap)
- [x] `[Authorize]` + rol bazlı `[Authorize(Roles = ...)]`
- [x] Yetkisiz erişimde Login’e yönlendir

### 6.2 MVC Controllers
- [x] `DepartmanlarController` — CRUD (Admin + Isletmen; Delete: Admin)
- [x] `PersonellerController` — CRUD (Admin + Yonetici + Isletmen; Delete: Admin)
- [x] `IzinlerController` — Personel: kendi talepleri; Staff: tümü; Onayla/Reddet: Admin/Yonetici
- [x] `UsersController` — Pending/Approve/Reject/Index/Roles (Admin + Yonetici; Yonetici Admin atayamaz)
- [x] `AuditLogsController` — Index (sadece Admin)
- [x] `DashboardController` — Index (Admin + Yonetici + Isletmen; maaş özeti + izinli personeller)

### 6.3 Views
- [x] `_Layout.cshtml`: Bootstrap 5 navbar (rol bazlı menü)
- [x] TempData ile başarı / hata flash mesajları
- [x] Tüm etiketler Türkçe
- [x] Formlar: validation summary + field validation
- [x] Dropdown’lar: Personelde Departman; İzinde Personel / İzinTuru
- [x] Personel Create/Edit formunda `Maas` alanı; Index/Details’te maaş gösterimi (örn. `N2` / TL formatı)
- [x] Personeller Index: Ad/Soyad arama kutusu + Departman filtre dropdown (Staff; sayfalama ile birlikte)
- [x] İzinler Index: Personel Ad/Soyad arama + Başlangıç/Bitiş tarih + İzin Türü filtre (sayfalama ile birlikte)
- [x] İzin onay ekranı: Beklemede listesi + Onayla/Reddet

### 6.4 DI (`Program.cs`)
- [x] DbContext + SQL Server connection string
- [x] Identity (cookie)
- [x] JWT Bearer authentication
- [x] AutoMapper
- [x] Application servis impl kayıtları
- [x] Migrate + Seed çağrısı

---

## 7. REST API + JWT

- [x] API controller’lar (`/api/...`), MVC ile aynı servis katmanını kullansın
- [x] `POST /api/auth/login` → JWT token döner (email/şifre)
- [x] JWT ayarları: `Issuer`, `Audience`, `Key`, `ExpireMinutes` (`appsettings`)
- [x] Korumalı endpoint’ler: `[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]` veya policy
- [x] Örnek endpoint’ler (DTO + AutoMapper):
  - `GET/POST/PUT/DELETE /api/departmanlar` (Admin/Isletmen; DELETE = soft delete, Admin)
  - `GET/POST/PUT/DELETE /api/personeller` (Staff; DELETE: Admin)
  - `GET/POST /api/izinler` (+ `POST /api/izinler/{id}/onayla`, `.../reddet` → Admin/Yonetici)
  - `GET /api/audit-logs` (Admin)
- [x] API login’de `IsApproved` kontrolü
- [x] API’de soft delete ve rol kuralları MVC ile aynı olsun
- [x] README’de örnek `curl` / token alma adımları + kayıt/rol özeti

---

## 8. Soft delete ve audit log (özet kontrol)

- [x] Tüm silme işlemleri soft delete
- [x] Global query filter ile silinmişler gizlensin
- [x] Admin için isteğe bağlı “silinmişleri göster / geri al (Restore)”
- [x] Create / Update / SoftDelete / Approve / Reject → `AuditLog` kaydı
- [x] Audit Log MVC + API listesi (filtre: entity, kullanıcı, tarih — basit tut)

---

## 9. Docker Compose

- [x] `docker-compose.yml`:
  - Servis `sqlserver` (Microsoft SQL Server image, SA şifresi, volume, healthcheck)
  - Servis `web` (Dockerfile ile build, SQL’e bağımlı, connection string + JWT key env)
- [x] `BilisimPersoneli.Web/Dockerfile` (multi-stage: build + publish + runtime)
- [x] Connection string ve JWT secret ortam değişkeninden okunacak
- [x] SQL hazır olana kadar healthcheck / retry

---

## 10. Çalıştırma / README kontrol listesi

- [x] `README.md` yaz:
  - Proje amacı (1 paragraf)
  - Mimari katmanlar (kısa)
  - Roller, kayıt onayı ve izin onay akışı
  - Soft delete + audit log özeti
  - API + JWT kullanım örneği
  - AutoMapper’ın rolü (DTO mapping)
  - Gereksinimler (Docker / .NET 8 SDK)
  - Çalıştırma: `docker compose up --build`
  - Yerel geliştirme: SQL container + `dotnet run`
  - Varsayılan kullanıcılar / şifreler / roller (Isletmen dahil)
  - CRUD + API ekran/endpoint özeti
- [x] `.gitignore` (.NET standart: bin, obj, user secrets, vs, vb.)
- [x] `appsettings.json` / Development: connection string + Jwt ayarları (Docker’da env override)

---

## 11. Sınav öncesi smoke test

- [ ] `docker compose up --build` sorunsuz ayağa kalkıyor (`IsApproved` + Isletmen seed dahil)
- [ ] Login: Admin / Yonetici / Isletmen / Personel ile giriş (MVC)
- [ ] Register: yeni kullanıcı Personel olarak kayıt olur; onay öncesi login engellenir
- [ ] Admin/Yonetici Kayıt Onayları’ndan onaylar; onay sonrası giriş açılır
- [ ] Admin Kullanıcılar panelinden tek birincil rol atar (Admin / Yonetici / Isletmen / Personel)
- [ ] Yonetici Kullanıcılar’dan sınırlı rol atar (Personel / Isletmen / Yonetici); Admin atayamaz
- [ ] Son Admin rolü düşürülemez; Personel soft-delete sonrası bağlı hesap giriş yapamaz
- [ ] Departman/Personel CRUD (Admin); soft delete sonrası listede görünmüyor
- [ ] Isletmen Departman/Personel/İzin görür-düzenler; silme ve izin onayı yapamaz
- [ ] Personel eklerken/güncellerken maaş girilebiliyor; listede görünüyor; negatif maaş reddediliyor
- [ ] Personel izin talebi oluşturuyor (Beklemede)
- [ ] Yonetici onaylıyor / reddediyor; audit log’a düşüyor
- [ ] Onaylı izin Personel tarafından düzenlenemiyor
- [ ] Geçersiz tarih (`Bitis < Baslangic`) reddediliyor
- [x] API: login → JWT → korumalı GET çalışıyor (doğrulandı)
- [ ] API: onaysız hesap token alamaz
- [ ] Yetkisiz rol API/MVC’de engelleniyor
- [ ] Logout sonrası korumalı MVC sayfası login istiyor
- [ ] Navbar ve Türkçe etiketler tutarlı (Kullanıcılar / Kayıt Ol dahil)
- [ ] Dashboard: Admin/Yonetici/Isletmen görüyor; Personel erişemiyor
- [ ] Dashboard: departman adı, personel sayısı, ortalama maaş, toplam maaş doğru
- [ ] Dashboard: en az bir izin kaydı olan personeller tek satırda (Ad, Soyad, Departman, İzin sayısı)
- [ ] Dashboard: departman ortalamasının üzerinde maaş alan personeller (Maas + DepartmanOrtalamaMaas; departmansız hariç)
- [ ] Liste sayfalarında satır seçimi (5/10/20/30/50/100) ve sayfa geçişinde yalnızca o kadar kayıt DB’den geliyor
- [ ] Personeller: Ad/Soyad arama + Departman filtre çalışıyor; sonuç sayfalı geliyor
- [ ] İzinler: Personel arama + tarih + izin türü filtre çalışıyor; sonuç sayfalı geliyor

---

## 12. Dashboard (Admin / Yönetici / İşletmen)

Admin, Yönetici ve İşletmen rollerinin görebileceği özet rapor sayfası.

### 12.1 Yetki
- [x] `DashboardController` — `Admin`, `Yonetici`, `Isletmen` (`AppRoles.Staff`)
- [x] Personel rolü erişemez (403 / AccessDenied)
- [x] Navbar’da Admin / Yonetici / Isletmen için **Dashboard** menü linki

### 12.2 Departman bazlı maaş özeti
- [x] Her departman için:
  - Departman adı
  - Personel sayısı
  - Ortalama maaş
  - Toplam maaş
- [x] Soft delete’li personel / departman global filter ile hariç
- [x] Personeli olmayan departmanda ortalama/toplam `0`

### 12.3 İzin kaydı bulunan personeller
- [x] En az bir izin kaydı olan personelleri listele
- [x] Kolonlar: Ad, Soyad, Departman adı, Toplam izin kayıt sayısı
- [x] Aynı personel birden fazla izne sahip olsa bile sonuçta **yalnızca bir kez** görünsün (`GroupBy` / `Distinct` personel)

### 12.4 Departman ortalamasının üzerinde maaş alan personeller
- [x] Maaşı, bağlı olduğu departmanın ortalama maaşından **yüksek** olan personelleri listele
- [x] Kolonlar: Ad, Soyad, Departman adı, Personel maaşı, Departman ortalama maaşı
- [x] Departman bilgisi olmayan personeller sonuç dışında bırakılsın

### 12.5 Application + Infrastructure
- [x] DTO’lar: `DepartmanMaasOzetiDto`, `IzinliPersonelOzetDto`, `OrtalamaUstuPersonelDto`, `DashboardDto`
- [x] `IDashboardService` + `DashboardService` (EF aggregation / group by)
- [x] DI kaydı (`AddScoped<IDashboardService, DashboardService>`)

### 12.6 MVC View
- [x] `Views/Dashboard/Index.cshtml` — üç bölüm (maaş özeti + izinli personel + ortalama üstü maaş)
- [x] Türkçe etiketler; maaş `N2` / TL formatı
- [x] `setup_install_steps.md` içinde Dashboard adımları belgelensin

---

## 13. Sunucu taraflı sayfalama (Pagination)

Tablo kullanan tüm MVC liste sayfalarında bir kerede tüm veri yüklenmez; her istekte yalnızca seçilen satır sayısı kadar kayıt veritabanından çekilir.

### 13.1 Ortak altyapı
- [x] `PagedResult<T>` + `IPagedResult` + `Pagination` / `PaginationDefaults` (`Application/Common/PagedResult.cs`)
- [x] Sayfa boyutu seçenekleri: **5, 10, 20, 30, 50, 100** (varsayılan 10)
- [x] `PaginationUiModel` + `Views/Shared/_Pagination.cshtml` (satır seçici + Önceki/Sonraki)

### 13.2 Servis metotları (`Skip` / `Take` + `Count`)
- [x] `IDepartmanService.GetPagedAsync`
- [x] `IPersonelService.GetPagedAsync` (opsiyonel `search`, `departmanId`)
- [x] `IIzinService.GetPagedAsync` / `GetPagedByPersonelIdAsync` (opsiyonel `search`, `baslangic`, `bitis`, `izinTuru`) / `GetPendingPagedAsync`
- [x] `IAuditLogService.GetPagedAsync` (filtreler korunur)
- [x] `IDashboardService.GetDashboardAsync` — üç tablo için bağımsız `page` / `pageSize`

### 13.3 MVC sayfaları
- [x] Departmanlar / Index
- [x] Personeller / Index
- [x] İzinler / Index
- [x] İzinler / Onaylar
- [x] Audit Logs / Index (filtre query string korunur)
- [x] Dashboard / Index (üç tablo ayrı sayfalanır)
- [x] `setup_install_steps.md` içinde sayfalama belgelensin

---

## 14. Personeller arama / filtre

Admin, Yönetici ve İşletmen `Personeller/Index` ekranında listeyi daraltabilir.

- [x] Ad / Soyad için arama kutusu (`search` query; `Ad.Contains` veya `Soyad.Contains`)
- [x] Departman dropdown filtresi (`departmanId`; boş = tüm departmanlar)
- [x] Filtreler sunucu tarafında uygulanır; ardından sayfalama (`Skip`/`Take`)
- [x] Sayfa geçişinde `search` ve `departmanId` korunur (`_Pagination` ExtraQuery)
- [x] Filtre değişince `page=1`’e dönülür; Temizle linki filtreleri sıfırlar
- [x] `TODO_LIST.md` / `setup_install_steps.md` güncellendi

---

## 15. İzinler arama / filtre

`Izinler/Index` (İzinlerim dahil) ekranında liste daraltılabilir.

- [x] Personel Ad / Soyad arama (`search`; Staff’ta görünür; Personel-only rolünde gizli)
- [x] Başlangıç tarihi filtresi (`baslangic` → `BaslangicTarihi >=`)
- [x] Bitiş tarihi filtresi (`bitis` → `BitisTarihi <=`)
- [x] İzin türü dropdown (`izinTuru`; boş = tüm türler)
- [x] Filtreler sunucu tarafında; ardından sayfalama (`Skip`/`Take`)
- [x] Sayfa geçişinde filtre query string korunur; filtrede `page=1`; Temizle sıfırlar
- [x] `TODO_LIST.md` / `setup_install_steps.md` güncellendi

---

## 16. Kullanıcı kaydı, onay ve rol yönetimi

- [x] `ApplicationUser` + `IsApproved`
- [x] `IUserManagementService` / `UserManagementService`
- [x] Register (MVC) + Login/API onay kontrolü
- [x] `UsersController` + Views (`Index`, `Pending`, `Roles`)
- [x] Isletmen rolü + navbar / Authorize güncellemesi
- [x] Migration `AddUserApprovalAndIsletmen` + seed (`isletmen@local`)
- [x] Kullanıcı–rol tasarımı dokümante edildi (`TODO_LIST.md` / `setup_install_steps.md`): Identity ↔ Personel köprüsü, varsayılan `Personel`, tek birincil rol, Yonetici sınırlı atama

### 16.1 Kullanıcı–rol tasarımı (uygulama)

- [x] `UpdateRolesAsync`: tek birincil rol + Yonetici Admin atayamasın + son Admin koruması
- [x] `UsersController` Roles/Index → `AdminAndYonetici`; Roles view radio; Yonetici’ye Admin gizle
- [x] Yonetici navbar: Kullanıcılar
- [x] Personel soft-delete → bağlı Identity `Lockout` / `IsApproved=false` (Restore ile geri açılır)
- [x] Domain’e `Personel.Rol` **eklenmez** (roller yalnızca Identity’de)

---

## Önerilen uygulama sırası

1. Solution + 4 proje + referanslar + AutoMapper paketi  
2. Domain: BaseEntity (soft delete + audit alanları), entity’ler, enum’lar, AuditLog  
3. Infrastructure: DbContext, global filter, SaveChanges soft delete/audit, Identity (`ApplicationUser`), migration, çoklu rol seed  
4. Application: DTO’lar, AutoMapper profiles, servis interface + impl  
5. Web: cookie auth + rol bazlı MVC CRUD + izin onay ekranları + Audit Log  
6. API Controllers + JWT  
7. Docker Compose + Dockerfile  
8. README + smoke test  
9. Dashboard (Staff): departman maaş özeti + izinli personel + ortalama üstü maaş listesi  
10. Sunucu taraflı sayfalama (tüm tablo listeleri; 5–100 satır seçimi)  
11. Personeller Index: Ad/Soyad arama + Departman filtre  
12. İzinler Index: Personel arama + tarih + izin türü filtre  
13. Kullanıcı kaydı / onay / Admin rol paneli + Isletmen (§16)  
14. Tek birincil rol + Yonetici sınırlı rol atama + Personel silince hesap kilidi (§16.1)  

---

## Not

Bu dosya geliştirme kontrol listesidir. Kodlama başladığında maddeler tamamlandıkça işaretlenir.
