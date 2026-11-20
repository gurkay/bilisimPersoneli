# Bilişim Personeli

Departman, personel ve izin kayıtlarını yöneten ASP.NET Core 8 MVC uygulaması. Clean Architecture, Identity (cookie + JWT), AutoMapper, soft delete ve audit log içerir.

## Mimari

```text
Domain → Application → Infrastructure
                   ↘
                    Web (MVC + API)
```

## Roller

| Kullanıcı | Şifre | Rol |
|-----------|-------|-----|
| admin@local | Sifre123! | Admin — tüm CRUD, kullanıcı/rol yönetimi, audit, onay |
| yonetici@local | Sifre123! | Yonetici — personel, izin onay/red, kayıt onayı |
| isletmen@local | Sifre123! | Isletmen — departman/personel/izin operasyonları (silme yok) |
| personel@local | Sifre123! | Personel — kendi izin talepleri |

**Kayıt akışı:** `/Account/Register` ile herkes Personel olarak kayıt olur → Admin/Yönetici **Kayıt Onayları**ndan onaylar → giriş açılır. Admin **Kullanıcılar** panelinden rol atar (Admin, Yonetici, Isletmen, Personel).

**İzin akışı:** Personel talep oluşturur (`Beklemede`) → Yonetici/Admin onaylar veya reddeder.

**Şifreler:** Admin/Yonetici/Isletmen personel oluştururken giriş şifresi verir (onaylı Personel hesabı). Güncellemede şifre boş bırakılırsa değişmez. Tüm kullanıcılar menüden **Şifre Değiştir** ile kendi şifrelerini güncelleyebilir.

## Çalıştırma (Docker)

SQL için `azure-sql-edge` kullanılır (Mac/ARM uyumlu).

```bash
docker compose up --build
```

Uygulama: http://localhost:8080

Sadece veritabanı:

```bash
docker compose up -d sqlserver
```

## Yerel geliştirme

```bash
docker compose up sqlserver -d
export PATH="/usr/local/share/dotnet:$PATH"
dotnet ef database update --project src/BilisimPersoneli.Infrastructure --startup-project src/BilisimPersoneli.Web
dotnet run --project src/BilisimPersoneli.Web
```

Connection string ve JWT ayarları `appsettings.json` içindedir.

## API + JWT örneği

```bash
# Token al
curl -s -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@local","password":"Sifre123!"}'

# Departmanları listele
curl -s http://localhost:8080/api/departmanlar \
  -H "Authorization: Bearer <TOKEN>"
```

### Endpoint özeti

- `POST /api/auth/login`
- `POST /api/auth/change-password` (JWT)
- `GET/POST/PUT/DELETE /api/departmanlar` (Admin/Isletmen; DELETE: Admin)
- `GET/POST/PUT/DELETE /api/personeller` (Staff; DELETE: Admin; `sifre` zorunlu oluşturmada)
- `GET/POST /api/izinler`
- `POST /api/izinler/{id}/onayla` / `reddet`
- `GET /api/audit-logs` (Admin)

## Öne çıkan özellikler

- Soft delete + global query filter
- Audit log (Create/Update/SoftDelete/Approve/Reject)
- AutoMapper ile DTO mapping
- Personel `Maas` alanı (decimal, >= 0)
