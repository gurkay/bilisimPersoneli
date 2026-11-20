# Bilişim Personeli — Sıfırdan Kurulum Rehberi

Bu doküman, projeyi boş bir klasörden başlayarak **klasör oluşturma → solution/katmanlar → sınıflar → EF/Identity → MVC/API → Docker Compose** sırasıyla nasıl kuracağınızı adım adım anlatır. İçerik, bu repodaki gerçek kodla uyumludur.

**Kapsam:** Departman, Personel, İzin yönetimi; kullanıcı kaydı / onay / rol atama  
**Stack:** ASP.NET Core 8 MVC + Clean Architecture, Identity (cookie + JWT, `ApplicationUser`), AutoMapper, EF Core + SQL Server, soft delete, audit log  
**Ortam:** Docker Compose (`azure-sql-edge` + web)

**Bilinçli olarak yapılmayanlar:** MediatR/CQRS, dosya yükleme, unit test projesi (ilk teslimde yok).

---

## İçindekiler

0. [Önkoşullar](#0-önkoşullar)  
1. [Solution ve klasör iskeleti](#1-solution-ve-klasör-iskeleti)  
2. [Domain katmanı](#2-domain-katmanı)  
3. [Application katmanı](#3-application-katmanı)  
4. [Infrastructure katmanı](#4-infrastructure-katmanı)  
5. [Migration](#5-migration)  
6. [Web katmanı (MVC + API)](#6-web-katmanı-mvc--api)  
7. [Docker Compose](#7-docker-compose)  
8. [Doğrulama ve smoke test](#8-doğrulama-ve-smoke-test)  
9. [Dashboard (Admin / Yönetici)](#9-dashboard-admin--yönetici)  
10. [Sunucu taraflı sayfalama](#10-sunucu-taraflı-sayfalama)  
11. [Personeller arama / filtre](#11-personeller-arama--filtre)  
12. [İzinler arama / filtre](#12-izinler-arama--filtre)  
13. [Kullanıcı kaydı, onay ve rol yönetimi](#13-kullanıcı-kaydı-onay-ve-rol-yönetimi)

---

## 0. Önkoşullar

Kuruluma başlamadan önce makinenizde şunlar kurulu olmalı:

| Araç | Amaç | Kontrol |
|------|------|---------|
| **.NET 8 SDK** | Proje derleme / çalıştırma | `dotnet --version` → `8.x` |
| **Docker Desktop** | SQL Server + web container | `docker --version` |
| **dotnet-ef** (opsiyonel, yerel migration için) | EF migration CLI | `dotnet ef --version` |

```bash
# EF araçları yoksa:
dotnet tool install --global dotnet-ef
# veya güncellemek için:
dotnet tool update --global dotnet-ef
```

**Not (macOS / ARM):** Bu projede SQL Server imajı olarak `mcr.microsoft.com/azure-sql-edge:latest` kullanılır. Klasik `mssql/server` imajı Apple Silicon’da sorun çıkarabilir.

---

## 1. Solution ve klasör iskeleti

### 1.1 Hedef dizin yapısı

```text
bilisimPersoneli/
├── BilisimPersoneli.sln
├── docker-compose.yml
├── README.md
├── setup_install_steps.md
└── src/
    ├── BilisimPersoneli.Domain/          # Entity, enum, BaseEntity
    ├── BilisimPersoneli.Application/     # DTO, interface, AutoMapper
    ├── BilisimPersoneli.Infrastructure/  # EF, Identity, servis impl
    └── BilisimPersoneli.Web/             # MVC + API + Program.cs + Dockerfile
```

### 1.2 Bağımlılık kuralı (Clean Architecture)

```text
Web → Application, Infrastructure
Infrastructure → Application, Domain
Application → Domain
Domain → (hiçbir katmana bağımlı değil)
```

- Domain’de EF / Identity / UI paketi olmaz.
- Application’da DbContext veya controller olmaz.
- İş kuralları servislerde; controller’lar ince kalır.

### 1.3 Solution ve projeleri oluşturma

Proje kökünde (ör. `bilisimPersoneli`) çalışın:

```bash
mkdir -p bilisimPersoneli && cd bilisimPersoneli

dotnet new sln -n BilisimPersoneli

dotnet new classlib -n BilisimPersoneli.Domain -o src/BilisimPersoneli.Domain -f net8.0
dotnet new classlib -n BilisimPersoneli.Application -o src/BilisimPersoneli.Application -f net8.0
dotnet new classlib -n BilisimPersoneli.Infrastructure -o src/BilisimPersoneli.Infrastructure -f net8.0
dotnet new mvc -n BilisimPersoneli.Web -o src/BilisimPersoneli.Web -f net8.0

dotnet sln add src/BilisimPersoneli.Domain/BilisimPersoneli.Domain.csproj
dotnet sln add src/BilisimPersoneli.Application/BilisimPersoneli.Application.csproj
dotnet sln add src/BilisimPersoneli.Infrastructure/BilisimPersoneli.Infrastructure.csproj
dotnet sln add src/BilisimPersoneli.Web/BilisimPersoneli.Web.csproj
```

Şablonun ürettiği varsayılan `Class1.cs` dosyalarını silin:

```bash
rm -f src/BilisimPersoneli.Domain/Class1.cs
rm -f src/BilisimPersoneli.Application/Class1.cs
rm -f src/BilisimPersoneli.Infrastructure/Class1.cs
```

### 1.4 Proje referansları

```bash
dotnet add src/BilisimPersoneli.Application/BilisimPersoneli.Application.csproj \
  reference src/BilisimPersoneli.Domain/BilisimPersoneli.Domain.csproj

dotnet add src/BilisimPersoneli.Infrastructure/BilisimPersoneli.Infrastructure.csproj \
  reference src/BilisimPersoneli.Application/BilisimPersoneli.Application.csproj

dotnet add src/BilisimPersoneli.Infrastructure/BilisimPersoneli.Infrastructure.csproj \
  reference src/BilisimPersoneli.Domain/BilisimPersoneli.Domain.csproj

dotnet add src/BilisimPersoneli.Web/BilisimPersoneli.Web.csproj \
  reference src/BilisimPersoneli.Application/BilisimPersoneli.Application.csproj

dotnet add src/BilisimPersoneli.Web/BilisimPersoneli.Web.csproj \
  reference src/BilisimPersoneli.Infrastructure/BilisimPersoneli.Infrastructure.csproj
```

### 1.5 NuGet paketleri

```bash
# Application
dotnet add src/BilisimPersoneli.Application package AutoMapper --version 15.1.4
dotnet add src/BilisimPersoneli.Application package System.ComponentModel.Annotations --version 5.0.0

# Infrastructure
dotnet add src/BilisimPersoneli.Infrastructure package Microsoft.EntityFrameworkCore.SqlServer --version 8.0.11
dotnet add src/BilisimPersoneli.Infrastructure package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 8.0.11
dotnet add src/BilisimPersoneli.Infrastructure package Microsoft.EntityFrameworkCore.Design --version 8.0.11

# Web
dotnet add src/BilisimPersoneli.Web package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.11
dotnet add src/BilisimPersoneli.Web package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 8.0.11
dotnet add src/BilisimPersoneli.Web package Microsoft.EntityFrameworkCore.Design --version 8.0.11
```

Infrastructure projesinde ASP.NET Identity / HTTP için shared framework referansı gerekir. `.csproj` içine şu satırı ekleyin (veya zaten varsa bırakın):

```xml
<ItemGroup>
  <FrameworkReference Include="Microsoft.AspNetCore.App" />
</ItemGroup>
```

### 1.6 Alt klasörleri oluşturma

```bash
# Domain
mkdir -p src/BilisimPersoneli.Domain/Common
mkdir -p src/BilisimPersoneli.Domain/Entities
mkdir -p src/BilisimPersoneli.Domain/Enums

# Application
mkdir -p src/BilisimPersoneli.Application/Common
mkdir -p src/BilisimPersoneli.Application/DTOs
mkdir -p src/BilisimPersoneli.Application/Interfaces
mkdir -p src/BilisimPersoneli.Application/Mapping

# Infrastructure
mkdir -p src/BilisimPersoneli.Infrastructure/Persistence
mkdir -p src/BilisimPersoneli.Infrastructure/Persistence/Migrations
mkdir -p src/BilisimPersoneli.Infrastructure/Services
mkdir -p src/BilisimPersoneli.Infrastructure/Identity

# Web (API controller + JWT servisi)
mkdir -p src/BilisimPersoneli.Web/Controllers/Api
mkdir -p src/BilisimPersoneli.Web/Services
mkdir -p src/BilisimPersoneli.Web/Views/Users
```

Bu aşamada `dotnet build BilisimPersoneli.sln` başarılı olmalıdır (henüz iş kodu yokken bile).

---

## 2. Domain katmanı

Domain yalnızca iş varlıkları ve enum’ları içerir; dış bağımlılık yoktur.

### 2.1 `BaseEntity`

**Dosya:** `src/BilisimPersoneli.Domain/Common/BaseEntity.cs`

```csharp
namespace BilisimPersoneli.Domain.Common;

public abstract class BaseEntity
{
    public int Id { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
```

### 2.2 Enum’lar

**`src/BilisimPersoneli.Domain/Enums/IzinTuru.cs`**

```csharp
namespace BilisimPersoneli.Domain.Enums;

public enum IzinTuru
{
    Yillik = 1,
    Mazeret = 2,
    Ucretsiz = 3
}
```

**`src/BilisimPersoneli.Domain/Enums/IzinDurumu.cs`**

```csharp
namespace BilisimPersoneli.Domain.Enums;

public enum IzinDurumu
{
    Beklemede = 1,
    Onaylandi = 2,
    Reddedildi = 3
}
```

**`src/BilisimPersoneli.Domain/Enums/AuditAction.cs`**

```csharp
namespace BilisimPersoneli.Domain.Enums;

public enum AuditAction
{
    Create = 1,
    Update = 2,
    SoftDelete = 3,
    Restore = 4,
    Approve = 5,
    Reject = 6
}
```

### 2.3 Entity’ler

**İlişkiler:**

```text
Departman (1) ──< Personel (N)
Personel  (1) ──< Izin     (N)
AuditLog      → EntityName + EntityId ile mantıksal referans (FK yok)
```

**`src/BilisimPersoneli.Domain/Entities/Departman.cs`**

```csharp
using BilisimPersoneli.Domain.Common;

namespace BilisimPersoneli.Domain.Entities;

public class Departman : BaseEntity
{
    public string Ad { get; set; } = string.Empty;
    public string? Aciklama { get; set; }

    public ICollection<Personel> Personeller { get; set; } = new List<Personel>();
}
```

**`src/BilisimPersoneli.Domain/Entities/Personel.cs`**

```csharp
using BilisimPersoneli.Domain.Common;

namespace BilisimPersoneli.Domain.Entities;

public class Personel : BaseEntity
{
    public string Ad { get; set; } = string.Empty;
    public string Soyad { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefon { get; set; }
    public decimal Maas { get; set; }
    public DateTime IseGirisTarihi { get; set; }

    public int DepartmanId { get; set; }
    public Departman? Departman { get; set; }

    /// <summary>ASP.NET Identity kullanıcısı ile köprü. Her Personel bir kullanıcıdır; rol Identity’de tutulur (Domain’de Personel.Rol yok).</summary>
    public string? IdentityUserId { get; set; }

    public ICollection<Izin> Izinler { get; set; } = new List<Izin>();
}
```

**`src/BilisimPersoneli.Domain/Entities/Izin.cs`**

```csharp
using BilisimPersoneli.Domain.Common;
using BilisimPersoneli.Domain.Enums;

namespace BilisimPersoneli.Domain.Entities;

public class Izin : BaseEntity
{
    public int PersonelId { get; set; }
    public Personel? Personel { get; set; }

    public DateTime BaslangicTarihi { get; set; }
    public DateTime BitisTarihi { get; set; }
    public IzinTuru IzinTuru { get; set; }
    public IzinDurumu Durum { get; set; } = IzinDurumu.Beklemede;
    public string? Aciklama { get; set; }

    public string? OnaylayanUserId { get; set; }
    public DateTime? OnayTarihi { get; set; }
    public string? OnayNotu { get; set; }
}
```

**`src/BilisimPersoneli.Domain/Entities/AuditLog.cs`**  
(`BaseEntity`’den türemez — soft delete uygulanmaz.)

```csharp
using BilisimPersoneli.Domain.Enums;

namespace BilisimPersoneli.Domain.Entities;

public class AuditLog
{
    public int Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public AuditAction Action { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Details { get; set; }
}
```

### 2.4 Domain iş kuralları (hatırlatma)

Bu kurallar Domain sınıflarında değil; Application validasyonu + Infrastructure servislerinde uygulanır:

- `BitisTarihi >= BaslangicTarihi`
- Personel e-posta ve Departman adı benzersiz
- `Maas >= 0`
- Fiziksel silme yok → soft delete (`IsDeleted = true`)
- Bağlı aktif personel varken departman silinemez; bağlı izin varken personel silinemez

---

## 3. Application katmanı

Application; sözleşmeler (interface), DTO’lar ve AutoMapper profilini tutar. Implementasyon Infrastructure’dadır.

### 3.1 Ortak yardımcılar

**`src/BilisimPersoneli.Application/Common/AppRoles.cs`**

```csharp
namespace BilisimPersoneli.Application.Common;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Yonetici = "Yonetici";
    public const string Isletmen = "Isletmen";
    public const string Personel = "Personel";

    public static readonly string[] All = { Admin, Yonetici, Isletmen, Personel };

    public const string Staff = $"{Admin},{Yonetici},{Isletmen}";
    public const string AdminAndYonetici = $"{Admin},{Yonetici}";
    public const string AdminAndIsletmen = $"{Admin},{Isletmen}";
}
```

Güncel dosya: `src/BilisimPersoneli.Application/Common/AppRoles.cs`

**Rol modeli:** Roller yalnızca Identity’de tutulur. Personel oluşturma/register → varsayılan `Personel`. Atama tek birincil rol (Admin: hepsi; Yonetici: `Personel`/`Isletmen`/`Yonetici`). Detay: [§13.0](#130-kullanıcırol-tasarımı-özet).

**`src/BilisimPersoneli.Application/Common/ServiceResult.cs`**

```csharp
namespace BilisimPersoneli.Application.Common;

public class ServiceResult
{
    public bool Succeeded { get; init; }
    public string? Error { get; init; }

    public static ServiceResult Success() => new() { Succeeded = true };
    public static ServiceResult Fail(string error) => new() { Succeeded = false, Error = error };
}

public class ServiceResult<T> : ServiceResult
{
    public T? Data { get; init; }

    public static ServiceResult<T> Success(T data) => new() { Succeeded = true, Data = data };
    public new static ServiceResult<T> Fail(string error) => new() { Succeeded = false, Error = error };
}
```

**`PagedResult.cs`** — sunucu taraflı sayfalama için `PagedResult<T>`, `PaginationDefaults` (5/10/20/30/50/100), `Pagination.Normalize`. Detay: [§10](#10-sunucu-taraflı-sayfalama).

### 3.2 DTO’lar

**`src/BilisimPersoneli.Application/DTOs/DepartmanDtos.cs`**

```csharp
using System.ComponentModel.DataAnnotations;

namespace BilisimPersoneli.Application.DTOs;

public class DepartmanDto
{
    public int Id { get; set; }
    public string Ad { get; set; } = string.Empty;
    public string? Aciklama { get; set; }
}

public class DepartmanCreateDto
{
    [Required(ErrorMessage = "Departman adı zorunludur.")]
    [StringLength(100, ErrorMessage = "Departman adı en fazla 100 karakter olabilir.")]
    [Display(Name = "Ad")]
    public string Ad { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Açıklama")]
    public string? Aciklama { get; set; }
}

public class DepartmanUpdateDto : DepartmanCreateDto
{
    public int Id { get; set; }
}
```

**`src/BilisimPersoneli.Application/DTOs/PersonelDtos.cs`** (Maas + şifre alanları)

```csharp
using System.ComponentModel.DataAnnotations;

namespace BilisimPersoneli.Application.DTOs;

public class PersonelDto
{
    public int Id { get; set; }
    public string Ad { get; set; } = string.Empty;
    public string Soyad { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefon { get; set; }
    public decimal Maas { get; set; }
    public DateTime IseGirisTarihi { get; set; }
    public int DepartmanId { get; set; }
    public string? DepartmanAd { get; set; }
    public string? IdentityUserId { get; set; }
}

public class PersonelCreateDto
{
    [Required(ErrorMessage = "Ad zorunludur.")]
    [StringLength(100)]
    [Display(Name = "Ad")]
    public string Ad { get; set; } = string.Empty;

    [Required(ErrorMessage = "Soyad zorunludur.")]
    [StringLength(100)]
    [Display(Name = "Soyad")]
    public string Soyad { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta giriniz.")]
    [StringLength(256)]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    [StringLength(30)]
    [Display(Name = "Telefon")]
    public string? Telefon { get; set; }

    [Required(ErrorMessage = "Maaş zorunludur.")]
    [Range(0, double.MaxValue, ErrorMessage = "Maaş negatif olamaz.")]
    [Display(Name = "Maaş")]
    public decimal Maas { get; set; }

    [Required(ErrorMessage = "İşe giriş tarihi zorunludur.")]
    [DataType(DataType.Date)]
    [Display(Name = "İşe Giriş Tarihi")]
    public DateTime IseGirisTarihi { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Departman seçiniz.")]
    [Display(Name = "Departman")]
    public int DepartmanId { get; set; }

    public string? IdentityUserId { get; set; }

    /// <summary>Oluşturmada zorunlu; güncellemede boş bırakılırsa şifre değişmez.</summary>
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
    [Display(Name = "Şifre")]
    public string? Sifre { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Şifre (Tekrar)")]
    [Compare(nameof(Sifre), ErrorMessage = "Şifreler eşleşmiyor.")]
    public string? SifreTekrar { get; set; }
}

public class PersonelUpdateDto : PersonelCreateDto
{
    public int Id { get; set; }
}
```

**`src/BilisimPersoneli.Application/DTOs/IzinDtos.cs`**

```csharp
using System.ComponentModel.DataAnnotations;
using BilisimPersoneli.Domain.Enums;

namespace BilisimPersoneli.Application.DTOs;

public class IzinDto
{
    public int Id { get; set; }
    public int PersonelId { get; set; }
    public string? PersonelAdSoyad { get; set; }
    public DateTime BaslangicTarihi { get; set; }
    public DateTime BitisTarihi { get; set; }
    public IzinTuru IzinTuru { get; set; }
    public IzinDurumu Durum { get; set; }
    public string? Aciklama { get; set; }
    public string? OnaylayanUserId { get; set; }
    public DateTime? OnayTarihi { get; set; }
    public string? OnayNotu { get; set; }
}

public class IzinCreateDto
{
    [Required(ErrorMessage = "Personel seçiniz.")]
    [Display(Name = "Personel")]
    public int PersonelId { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Başlangıç Tarihi")]
    public DateTime BaslangicTarihi { get; set; } = DateTime.Today;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Bitiş Tarihi")]
    public DateTime BitisTarihi { get; set; } = DateTime.Today;

    [Required]
    [Display(Name = "İzin Türü")]
    public IzinTuru IzinTuru { get; set; } = IzinTuru.Yillik;

    [StringLength(500)]
    [Display(Name = "Açıklama")]
    public string? Aciklama { get; set; }
}

public class IzinUpdateDto : IzinCreateDto
{
    public int Id { get; set; }
}

public class IzinReddetDto
{
    [StringLength(500)]
    [Display(Name = "Onay Notu")]
    public string? OnayNotu { get; set; }
}
```

**`src/BilisimPersoneli.Application/DTOs/AuthDtos.cs`**

```csharp
using System.ComponentModel.DataAnnotations;

namespace BilisimPersoneli.Application.DTOs;

public class LoginDto
{
    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre zorunludur.")]
    [DataType(DataType.Password)]
    [Display(Name = "Şifre")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Beni hatırla")]
    public bool RememberMe { get; set; }
}

public class TokenResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string Email { get; set; } = string.Empty;
    public IList<string> Roles { get; set; } = new List<string>();
}

public class ChangePasswordDto
{
    [Required(ErrorMessage = "Mevcut şifre zorunludur.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mevcut Şifre")]
    public string MevcutSifre { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yeni şifre zorunludur.")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
    [Display(Name = "Yeni Şifre")]
    public string YeniSifre { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yeni şifre tekrarı zorunludur.")]
    [DataType(DataType.Password)]
    [Display(Name = "Yeni Şifre (Tekrar)")]
    [Compare(nameof(YeniSifre), ErrorMessage = "Şifreler eşleşmiyor.")]
    public string YeniSifreTekrar { get; set; } = string.Empty;
}
```

**`UserManagementDtos.cs`** — `RegisterDto`, `UserListItemDto`, `UserRolesUpdateDto` (detay: [§13](#13-kullanıcı-kaydı-onay-ve-rol-yönetimi)).

**`src/BilisimPersoneli.Application/DTOs/AuditLogDto.cs`**

```csharp
using BilisimPersoneli.Domain.Enums;

namespace BilisimPersoneli.Application.DTOs;

public class AuditLogDto
{
    public int Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public AuditAction Action { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Details { get; set; }
}
```

**`DashboardDtos.cs`** — `DepartmanMaasOzetiDto`, `IzinliPersonelOzetDto`, `OrtalamaUstuPersonelDto`, `DashboardDto` (detay: [§9 Dashboard](#9-dashboard-admin--yönetici)).

### 3.3 Interface’ler

**`IDepartmanService`**

```csharp
using BilisimPersoneli.Application.Common;
using BilisimPersoneli.Application.DTOs;

namespace BilisimPersoneli.Application.Interfaces;

public interface IDepartmanService
{
    Task<IReadOnlyList<DepartmanDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<DepartmanDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<DepartmanDto>> CreateAsync(DepartmanCreateDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult<DepartmanDto>> UpdateAsync(DepartmanUpdateDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult> SoftDeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> RestoreAsync(int id, CancellationToken cancellationToken = default);
}
```

**`IPersonelService`** — yukarıdakine ek olarak `GetByIdentityUserIdAsync`.

**`IIzinService`** — listeleme (`GetAll`, `GetByPersonelId`, `GetPending`) + CRUD + `ApproveAsync` / `RejectAsync`.

**`IAuditLogService`** — filtreli liste (`entityName`, `userName`, `from`, `to`).

**`IDashboardService`** — Admin/Yonetici dashboard özeti (`GetDashboardAsync`).

**`ICurrentUserService`**

```csharp
namespace BilisimPersoneli.Application.Interfaces;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserName { get; }
    bool IsInRole(string role);
    bool IsAuthenticated { get; }
    bool IsStaff { get; } // Admin || Yonetici || Isletmen
}
```

**`IUserManagementService`** — `RegisterAsync`, `GetUsersAsync`, `ApproveAsync`, `RejectAsync`, `UpdateRolesAsync` (detay: [§13](#13-kullanıcı-kaydı-onay-ve-rol-yönetimi)).

Tam imzalar: `src/BilisimPersoneli.Application/Interfaces/*.cs`

### 3.4 AutoMapper profili

**`src/BilisimPersoneli.Application/Mapping/MappingProfile.cs`**

```csharp
using AutoMapper;
using BilisimPersoneli.Application.DTOs;
using BilisimPersoneli.Domain.Entities;

namespace BilisimPersoneli.Application.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Departman, DepartmanDto>();
        CreateMap<DepartmanCreateDto, Departman>();
        CreateMap<DepartmanUpdateDto, Departman>();

        CreateMap<Personel, PersonelDto>()
            .ForMember(d => d.DepartmanAd, opt => opt.MapFrom(s => s.Departman != null ? s.Departman.Ad : null));
        CreateMap<PersonelCreateDto, Personel>()
            .ForMember(d => d.IdentityUserId, opt => opt.Ignore());
        CreateMap<PersonelUpdateDto, Personel>()
            .ForMember(d => d.IdentityUserId, opt => opt.Ignore());

        CreateMap<Izin, IzinDto>()
            .ForMember(d => d.PersonelAdSoyad, opt => opt.MapFrom(s =>
                s.Personel != null ? $"{s.Personel.Ad} {s.Personel.Soyad}" : null));
        CreateMap<IzinCreateDto, Izin>();
        CreateMap<IzinUpdateDto, Izin>();

        CreateMap<AuditLog, AuditLogDto>();
    }
}
```

---

## 4. Infrastructure katmanı

Bu katmanda DbContext, Identity, seed ve servis implementasyonları yer alır.

### 4.1 `AppDbContext`

**Dosya:** `src/BilisimPersoneli.Infrastructure/Persistence/AppDbContext.cs`

Önemli noktalar:

1. `IdentityDbContext<ApplicationUser>` tabanı (`ApplicationUser : IdentityUser` + `IsApproved`)  
2. `DbSet`’ler: `Departmanlar`, `Personeller`, `Izinler`, `AuditLogs`  
3. Fluent API: unique index (`Ad`, `Email`), `Maas` → `decimal(18,2)`, FK `Restrict`  
4. Global query filter: `HasQueryFilter(x => !x.IsDeleted)` (Departman, Personel, Izin)  
5. `SaveChanges` / `SaveChangesAsync` içinde soft delete + audit  
6. Onay/red için `AddManualAudit(...)`

**`ApplicationUser`:** `src/BilisimPersoneli.Infrastructure/Identity/ApplicationUser.cs`

```csharp
using BilisimPersoneli.Application.Interfaces;
using BilisimPersoneli.Domain.Common;
using BilisimPersoneli.Domain.Entities;
using BilisimPersoneli.Domain.Enums;
using BilisimPersoneli.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace BilisimPersoneli.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly ICurrentUserService? _currentUser;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserService? currentUser = null)
        : base(options)
    {
        _currentUser = currentUser;
    }

    public DbSet<Departman> Departmanlar => Set<Departman>();
    public DbSet<Personel> Personeller => Set<Personel>();
    public DbSet<Izin> Izinler => Set<Izin>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Departman>(e =>
        {
            e.Property(x => x.Ad).HasMaxLength(100).IsRequired();
            e.Property(x => x.Aciklama).HasMaxLength(500);
            e.HasIndex(x => x.Ad).IsUnique();
            e.HasQueryFilter(x => !x.IsDeleted);
            e.HasMany(x => x.Personeller)
                .WithOne(x => x.Departman)
                .HasForeignKey(x => x.DepartmanId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Personel>(e =>
        {
            e.Property(x => x.Ad).HasMaxLength(100).IsRequired();
            e.Property(x => x.Soyad).HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.Property(x => x.Telefon).HasMaxLength(30);
            e.Property(x => x.Maas).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(x => x.IdentityUserId).HasMaxLength(450);
            e.HasIndex(x => x.Email).IsUnique();
            e.HasQueryFilter(x => !x.IsDeleted);
            e.HasMany(x => x.Izinler)
                .WithOne(x => x.Personel)
                .HasForeignKey(x => x.PersonelId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Izin>(e =>
        {
            e.Property(x => x.Aciklama).HasMaxLength(500);
            e.Property(x => x.OnaylayanUserId).HasMaxLength(450);
            e.Property(x => x.OnayNotu).HasMaxLength(500);
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        builder.Entity<AuditLog>(e =>
        {
            e.Property(x => x.EntityName).HasMaxLength(100).IsRequired();
            e.Property(x => x.EntityId).HasMaxLength(50).IsRequired();
            e.Property(x => x.UserId).HasMaxLength(450);
            e.Property(x => x.UserName).HasMaxLength(256);
            e.Property(x => x.Details).HasMaxLength(2000);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditAndSoftDelete();
        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ApplyAuditAndSoftDelete();
        return base.SaveChanges();
    }

    private void ApplyAuditAndSoftDelete()
    {
        var now = DateTime.UtcNow;
        var userId = _currentUser?.UserId;
        var userName = _currentUser?.UserName ?? "system";
        var auditEntries = new List<AuditLog>();

        foreach (var entry in ChangeTracker.Entries<BaseEntity>().ToList())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.CreatedBy = userName;
                entry.Entity.IsDeleted = false;
                auditEntries.Add(CreateAudit(entry, AuditAction.Create, userId, userName, now));
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedBy = userName;
                if (entry.Property(nameof(BaseEntity.IsDeleted)).IsModified && entry.Entity.IsDeleted)
                {
                    entry.Entity.DeletedAt ??= now;
                    auditEntries.Add(CreateAudit(entry, AuditAction.SoftDelete, userId, userName, now));
                }
                else if (entry.Property(nameof(BaseEntity.IsDeleted)).IsModified && !entry.Entity.IsDeleted)
                {
                    entry.Entity.DeletedAt = null;
                    auditEntries.Add(CreateAudit(entry, AuditAction.Restore, userId, userName, now));
                }
                else
                {
                    auditEntries.Add(CreateAudit(entry, AuditAction.Update, userId, userName, now));
                }
            }
            else if (entry.State == EntityState.Deleted)
            {
                // Fiziksel silmeyi soft delete'e çevir
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAt = now;
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedBy = userName;
                auditEntries.Add(CreateAudit(entry, AuditAction.SoftDelete, userId, userName, now));
            }
        }

        if (auditEntries.Count > 0) AuditLogs.AddRange(auditEntries);
    }

    private static AuditLog CreateAudit(
        EntityEntry<BaseEntity> entry,
        AuditAction action,
        string? userId,
        string? userName,
        DateTime timestamp)
    {
        return new AuditLog
        {
            EntityName = entry.Entity.GetType().Name,
            EntityId = entry.Entity.Id.ToString(),
            Action = action,
            UserId = userId,
            UserName = userName,
            Timestamp = timestamp,
            Details = $"{action} on {entry.Entity.GetType().Name}"
        };
    }

    public void AddManualAudit(string entityName, string entityId, AuditAction action, string? details = null)
    {
        AuditLogs.Add(new AuditLog
        {
            EntityName = entityName,
            EntityId = entityId,
            Action = action,
            UserId = _currentUser?.UserId,
            UserName = _currentUser?.UserName ?? "system",
            Timestamp = DateTime.UtcNow,
            Details = details ?? $"{action} on {entityName}"
        });
    }
}
```

### 4.2 Design-time factory (EF CLI)

**`src/BilisimPersoneli.Infrastructure/Persistence/AppDbContextFactory.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BilisimPersoneli.Infrastructure.Persistence;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=localhost,1433;Database=BilisimPersoneli;User Id=sa;Password=Your_strong_Password123;TrustServerCertificate=True;");
        return new AppDbContext(optionsBuilder.Options);
    }
}
```

Migration komutları `dotnet ef` çalıştırılırken SQL’in `localhost:1433`’te ayakta olmasını ister (önce `docker compose up sqlserver -d`).

### 4.3 `DbSeeder`

**Dosya:** `src/BilisimPersoneli.Infrastructure/Persistence/DbSeeder.cs`

Startup’ta yapılanlar:

1. `Database.MigrateAsync()` — bekleyen migration’ları uygula  
2. Roller: `Admin`, `Yonetici`, `Isletmen`, `Personel`  
3. Kullanıcılar (şifre **`Sifre123!`**, hepsi `IsApproved = true`):
   - `admin@local` → Admin  
   - `yonetici@local` → Yonetici  
   - `isletmen@local` → Isletmen  
   - `personel@local` → Personel  
4. Örnek veri (DB boşsa): Bilişim + İnsan Kaynakları departmanları, Ali Yılmaz personeli (`Maas = 45000`), örnek Beklemede izin

Tam kod: `src/BilisimPersoneli.Infrastructure/Persistence/DbSeeder.cs`

### 4.4 Servis implementasyonları

| Sınıf | Dosya | Görev |
|-------|-------|--------|
| `CurrentUserService` | `Services/CurrentUserService.cs` | `HttpContext` üzerinden UserId / UserName / rol / `IsStaff` |
| `DepartmanService` | `Services/DepartmanService.cs` | CRUD, ad benzersizliği, bağlı personel varken silme engeli, Restore |
| `PersonelService` | `Services/PersonelService.cs` | CRUD, Maas >= 0, e-posta unique, onaylı `ApplicationUser` oluşturma/şifre senkronu |
| `IzinService` | `Services/IzinService.cs` | CRUD, tarih kontrolü, Onayla/Reddet + manuel audit |
| `AuditLogService` | `Services/AuditLogService.cs` | Filtreli liste (max 500) |
| `DashboardService` | `Services/DashboardService.cs` | Departman maaş özeti + izinli personel + ortalama üstü maaş listesi |
| `UserManagementService` | `Services/UserManagementService.cs` | Register, liste, kayıt onay/red, rol atama |

**Soft delete örneği (`DepartmanService`):** `_db.Departmanlar.Remove(entity)` çağrılır; `AppDbContext` bunu soft delete’e çevirir.

**Onay örneği (`IzinService.ApproveAsync`):**

```csharp
entity.Durum = IzinDurumu.Onaylandi;
entity.OnaylayanUserId = _currentUser.UserId;
entity.OnayTarihi = DateTime.UtcNow;
_db.AddManualAudit(nameof(Izin), entity.Id.ToString(), AuditAction.Approve, "İzin onaylandı");
await _db.SaveChangesAsync(cancellationToken);
```

**Personel oluştururken Identity:** e-posta ile `ApplicationUser` açılır (`IsApproved = true`), varsayılan **tek** rol `Personel` verilir, `Personel.IdentityUserId` set edilir. Self-register ise `IsApproved = false` (Admin/Yonetici onayı gerekir). Rol yükseltme Users panelinden yapılır; Personel kaydı silinmez. Soft-delete Personel → bağlı Identity hesabı kilitlenmeli / onay kalkmalı (giriş kapanır).

Tam kaynaklar: `src/BilisimPersoneli.Infrastructure/Services/*.cs`

### 4.5 DI kaydı

**`src/BilisimPersoneli.Infrastructure/DependencyInjection.cs`**

```csharp
using BilisimPersoneli.Application.Interfaces;
using BilisimPersoneli.Infrastructure.Identity;
using BilisimPersoneli.Infrastructure.Persistence;
using BilisimPersoneli.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BilisimPersoneli.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 6;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IDepartmanService, DepartmanService>();
        services.AddScoped<IPersonelService, PersonelService>();
        services.AddScoped<IIzinService, IzinService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IUserManagementService, UserManagementService>();

        return services;
    }
}
```

---

## 5. Migration

SQL container’ı başlatın:

```bash
docker compose up sqlserver -d
# SQL hazır olana kadar ~30–60 sn bekleyin
```

İlk migration’ı oluşturun:

```bash
dotnet ef migrations add InitialCreate \
  --project src/BilisimPersoneli.Infrastructure/BilisimPersoneli.Infrastructure.csproj \
  --startup-project src/BilisimPersoneli.Web/BilisimPersoneli.Web.csproj \
  --output-dir Persistence/Migrations
```

Bu komut `Persistence/Migrations/` altına `*_InitialCreate.cs`, Designer ve `AppDbContextModelSnapshot.cs` üretir.

Kayıt onayı / `ApplicationUser` için ek migration (repoda mevcut):

```bash
dotnet ef migrations add AddUserApprovalAndIsletmen \
  --project src/BilisimPersoneli.Infrastructure/BilisimPersoneli.Infrastructure.csproj \
  --startup-project src/BilisimPersoneli.Web/BilisimPersoneli.Web.csproj \
  --output-dir Persistence/Migrations
```

`AspNetUsers.IsApproved` ekler; Up içinde mevcut kullanıcılar `IsApproved = 1` yapılır. Startup `MigrateAsync` her iki migration’ı uygular.

İsteğe bağlı elle uygulama:

```bash
dotnet ef database update \
  --project src/BilisimPersoneli.Infrastructure/BilisimPersoneli.Infrastructure.csproj \
  --startup-project src/BilisimPersoneli.Web/BilisimPersoneli.Web.csproj
```

**Not:** Uygulama ayağa kalkınca `DbSeeder.SeedAsync` zaten `MigrateAsync()` çağırır. Docker’da SQL geç ayağa kalktığı için `Program.cs` içinde **15 deneme × 3 sn** retry vardır.

---

## 6. Web katmanı (MVC + API)

### 6.1 `appsettings.json`

**Dosya:** `src/BilisimPersoneli.Web/appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=BilisimPersoneli;User Id=sa;Password=Your_strong_Password123;TrustServerCertificate=True;MultipleActiveResultSets=true"
  },
  "Jwt": {
    "Issuer": "BilisimPersoneli",
    "Audience": "BilisimPersoneliClients",
    "Key": "BilisimPersoneli_Dev_Secret_Key_Min32Chars!",
    "ExpireMinutes": "120"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

Docker’da connection string ve JWT değerleri **ortam değişkenleriyle override** edilir (`ConnectionStrings__DefaultConnection`, `Jwt__Key`, …).

### 6.2 `Program.cs`

**Dosya:** `src/BilisimPersoneli.Web/Program.cs`

```csharp
using System.Text;
using BilisimPersoneli.Application.Mapping;
using BilisimPersoneli.Infrastructure;
using BilisimPersoneli.Infrastructure.Persistence;
using BilisimPersoneli.Web.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddControllers();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAutoMapper(cfg => { }, typeof(MappingProfile).Assembly);
builder.Services.AddScoped<JwtTokenService>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
});

var jwtSection = builder.Configuration.GetSection("Jwt");
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!));

builder.Services.AddAuthentication()
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

await SeedWithRetryAsync(app.Services);

static async Task SeedWithRetryAsync(IServiceProvider services)
{
    const int maxAttempts = 15;
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await DbSeeder.SeedAsync(services);
            return;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            Console.WriteLine($"DB henüz hazır değil (deneme {attempt}/{maxAttempts}): {ex.Message}");
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
```

**Middleware sırası kritik:** `UseAuthentication` → `UseAuthorization` → endpoint map.

### 6.3 JWT token servisi

**`src/BilisimPersoneli.Web/Services/JwtTokenService.cs`**

- `Jwt` config’ten Issuer / Audience / Key / ExpireMinutes okur  
- Claim’ler: Sub, Email, NameIdentifier, Name, Role(s), Jti  
- `TokenResponseDto` döner  

Tam kod: `src/BilisimPersoneli.Web/Services/JwtTokenService.cs`

### 6.4 MVC controller’lar ve roller

| Controller | Yetki | Görev |
|------------|-------|--------|
| `AccountController` | Login/Register anonim; diğerleri auth | Login, Logout, ChangePassword, Register |
| `HomeController` | `[Authorize]` | Ana sayfa |
| `DashboardController` | Admin + Yonetici + Isletmen (`Staff`) | Departman maaş özeti + izinli personel + ortalama üstü maaş |
| `DepartmanlarController` | Admin + Isletmen (Delete: Admin) | Departman CRUD |
| `PersonellerController` | Staff (Delete: Admin) | Personel CRUD + Maas + şifre |
| `IzinlerController` | Auth; Onayla/Reddet: Admin/Yonetici | İzin listesi / talep / Onaylar |
| `UsersController` | Admin + Yonetici (Pending/Approve; Index/Roles: Yonetici sınırlı atama) | Kullanıcı listesi, kayıt onayı, tek birincil rol atama |
| `AuditLogsController` | Admin | Audit log listesi |

Controller dosyaları: `src/BilisimPersoneli.Web/Controllers/`

### 6.5 Razor Views

Önerilen view klasörleri:

```text
Views/
  Account/       Login, Register, ChangePassword, AccessDenied
  Dashboard/     Index (Staff özet rapor)
  Departmanlar/  Index, Create, Edit, Details, Delete
  Personeller/   Index, Create, Edit, Details, Delete, _PersonelForm
  Izinler/       Index, Create, Edit, Details, Delete, Onaylar
  Users/         Index, Pending, Roles
  AuditLogs/     Index
  Shared/        _Layout (rol bazlı navbar + TempData flash), _Pagination
```

Liste `Index` view’ları `PagedResult<T>` model kullanır; satır seçimi ve sayfa linkleri `_Pagination` partial’ı ile gelir (detay: [§10](#10-sunucu-taraflı-sayfalama)).

`_Layout.cshtml` içinde menü:

- **Admin:** Dashboard, Departmanlar, Personeller, İzinler, İzin Onayları, Kullanıcılar, Kayıt Onayları, Audit Log  
- **Yonetici:** Dashboard, Personeller, İzinler, İzin Onayları, Kullanıcılar (sınırlı rol), Kayıt Onayları  
- **Isletmen:** Dashboard, Departmanlar, Personeller, İzinler  
- **Personel:** İzinlerim  
- **Anonim:** Giriş, Kayıt Ol  

Tüm form etiketleri Türkçe; Bootstrap 5 kullanılır. Personel formunda `Maas` alanı zorunlu; Index/Details’te `N2` / TL formatı önerilir.

### 6.6 REST API controller’lar

| Controller | Route | Auth |
|------------|-------|------|
| `AuthApiController` | `api/auth` | Login anonim (`IsApproved` zorunlu); change-password JWT |
| `DepartmanlarApiController` | `api/departmanlar` | JWT + Admin/Isletmen (DELETE: Admin) |
| `PersonellerApiController` | `api/personeller` | JWT + Staff (silme Admin) |
| `IzinlerApiController` | `api/izinler` | JWT; onayla/reddet Admin/Yonetici |
| `AuditLogsApiController` | `api/audit-logs` | JWT + Admin |

Örnek login aksiyonu (`AuthApiController`):

```csharp
[HttpPost("login")]
[AllowAnonymous]
public async Task<ActionResult<TokenResponseDto>> Login([FromBody] LoginDto model)
{
    var user = await _userManager.FindByEmailAsync(model.Email);
    if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
        return Unauthorized(new { message = "Geçersiz e-posta veya şifre." });

    if (!user.IsApproved)
        return Unauthorized(new { message = "Hesabınız henüz onaylanmadı." });

    var roles = await _userManager.GetRolesAsync(user);
    return Ok(_jwtTokenService.CreateToken(user, roles));
}
```

Korumalı API’lerde şema:

```csharp
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = AppRoles.Admin)]
```

Dosyalar: `src/BilisimPersoneli.Web/Controllers/Api/`

### 6.7 Rol / izin / kayıt onay akışı (özet)

```text
Anonim → /Account/Register (Personel + IsApproved=false)
Staff → Personel oluştur (Identity + IsApproved=true + varsayılan rol Personel + IdentityUserId)
Admin/Yonetici → Kayıt Onayları: Onayla / Reddet
Admin → Kullanıcılar: tek birincil rol (Admin, Yonetici, Isletmen, Personel)
Yonetici → Kullanıcılar: tek birincil rol (Personel, Isletmen, Yonetici) — Admin atayamaz

Personel → izin oluşturur (Durum = Beklemede)
Yonetici/Admin → Onayla (Onaylandi) veya Reddet (Reddedildi + OnayNotu)
Onaylı/reddedilmiş izin Personel tarafından düzenlenemez
Sadece Beklemede kayıtlar onay/red aksiyonuna açık
```

Detaylı kurulum: [§13](#13-kullanıcı-kaydı-onay-ve-rol-yönetimi).

---

## 7. Docker Compose

### 7.1 Multi-stage Dockerfile

**Dosya:** `src/BilisimPersoneli.Web/Dockerfile`  
**Build context:** solution kökü (compose `context: .`)

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY BilisimPersoneli.sln ./
COPY src/BilisimPersoneli.Domain/BilisimPersoneli.Domain.csproj src/BilisimPersoneli.Domain/
COPY src/BilisimPersoneli.Application/BilisimPersoneli.Application.csproj src/BilisimPersoneli.Application/
COPY src/BilisimPersoneli.Infrastructure/BilisimPersoneli.Infrastructure.csproj src/BilisimPersoneli.Infrastructure/
COPY src/BilisimPersoneli.Web/BilisimPersoneli.Web.csproj src/BilisimPersoneli.Web/

RUN dotnet restore src/BilisimPersoneli.Web/BilisimPersoneli.Web.csproj

COPY src/ src/
RUN dotnet publish src/BilisimPersoneli.Web/BilisimPersoneli.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BilisimPersoneli.Web.dll"]
```

Aşamalar: **restore (csproj cache) → publish → runtime aspnet:8.0**.

### 7.2 `docker-compose.yml`

**Dosya:** proje kökünde `docker-compose.yml`

```yaml
services:
  sqlserver:
    image: mcr.microsoft.com/azure-sql-edge:latest
    container_name: bilisim-sqlserver
    environment:
      ACCEPT_EULA: "Y"
      MSSQL_SA_PASSWORD: "Your_strong_Password123"
      MSSQL_PID: "Developer"
    ports:
      - "1433:1433"
    volumes:
      - mssql_data:/var/opt/mssql
    healthcheck:
      test: ["CMD-SHELL", "timeout 1 bash -c '</dev/tcp/127.0.0.1/1433' || exit 1"]
      interval: 10s
      timeout: 5s
      retries: 12
      start_period: 40s

  web:
    build:
      context: .
      dockerfile: src/BilisimPersoneli.Web/Dockerfile
    container_name: bilisim-web
    ports:
      - "8080:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ConnectionStrings__DefaultConnection: "Server=sqlserver,1433;Database=BilisimPersoneli;User Id=sa;Password=Your_strong_Password123;TrustServerCertificate=True;MultipleActiveResultSets=true"
      Jwt__Issuer: BilisimPersoneli
      Jwt__Audience: BilisimPersoneliClients
      Jwt__Key: BilisimPersoneli_Dev_Secret_Key_Min32Chars!
      Jwt__ExpireMinutes: "120"
    depends_on:
      - sqlserver

volumes:
  mssql_data:
```

**Önemli:** Container içinden DB host adı `localhost` değil, compose servis adı olan **`sqlserver`** olmalıdır.

### 7.3 Tam stack çalıştırma

```bash
cd /path/to/bilisimPersoneli
docker compose up --build
```

Uygulama: **http://localhost:8080**

Arka planda:

```bash
docker compose up --build -d
docker compose logs -f web
```

Durdurma:

```bash
docker compose down
# Veriyi de silmek için:
docker compose down -v
```

### 7.4 Hibrit geliştirme (SQL Docker, Web host)

```bash
docker compose up sqlserver -d

dotnet build BilisimPersoneli.sln
dotnet run --project src/BilisimPersoneli.Web/BilisimPersoneli.Web.csproj
```

Yerel URL’ler (`launchSettings.json`): HTTP `5075`, HTTPS `7065`. Connection string `appsettings.json` içindeki `localhost,1433` değerini kullanır.

---

## 8. Doğrulama ve smoke test

### 8.1 Varsayılan kullanıcılar

| E-posta | Şifre | Rol |
|---------|-------|-----|
| `admin@local` | `Sifre123!` | Admin — tüm CRUD, kullanıcı/tüm roller, audit, onay (Personel satırı zorunlu değil) |
| `yonetici@local` | `Sifre123!` | Yonetici — personel, izin onay/red, kayıt onayı, sınırlı rol atama |
| `isletmen@local` | `Sifre123!` | Isletmen — departman/personel/izin operasyonları (silme yok) |
| `personel@local` | `Sifre123!` | Personel — kendi izin talepleri (+ bağlı `Personel` satırı) |

### 8.2 MVC kontrol listesi

1. `docker compose up --build` sorunsuz ayağa kalkıyor (`IsApproved` + Isletmen seed)  
2. Dört rolle login çalışıyor (Admin / Yonetici / Isletmen / Personel)  
3. Register → onay öncesi login engellenir; Admin/Yonetici onaylayınca giriş açılır  
4. Admin Kullanıcılar panelinden **tek birincil rol** atar (dört rol)  
4b. Yonetici Kullanıcılar’dan sınırlı rol atar (`Personel`/`Isletmen`/`Yonetici`); Admin atayamaz; son Admin düşürülemez  
4c. Personel soft-delete sonrası bağlı Identity hesabı giriş yapamaz  
5. Admin: Departman/Personel CRUD; soft delete sonrası listede yok  
6. Isletmen: Departman/Personel/İzin düzenler; silme ve izin onayı yapamaz  
7. Personel formunda Maas giriliyor; negatif maaş reddediliyor  
8. Personel izin talebi oluşturuyor (`Beklemede`)  
9. Yonetici onaylıyor/reddediyor; Audit Log’da kayıt görünüyor  
10. Onaylı izin Personel tarafından düzenlenemiyor  
11. `Bitis < Baslangic` reddediliyor  
12. Yetkisiz rol engelleniyor; logout sonrası korumalı sayfa login istiyor  
13. Navbar ve Türkçe etiketler tutarlı (Kayıt Ol / Kullanıcılar / Kayıt Onayları)  
14. Admin/Yonetici/Isletmen Dashboard görüyor; Personel `/Dashboard` erişemiyor  
15. Dashboard’da departman adı, personel sayısı, ortalama/toplam maaş doğru  
16. En az bir izin kaydı olan personeller tek satırda (Ad, Soyad, Departman, İzin sayısı)  
17. Departman ortalamasının üzerinde maaş alan personeller (Maas + DepartmanOrtalamaMaas; departmansız hariç)  
18. Liste sayfalarında satır seçimi (5–100) çalışıyor; sayfa değişince yalnızca o kadar kayıt geliyor  
19. Personeller: Ad/Soyad arama + Departman filtre; sayfalama filtreleri koruyor  
20. İzinler: Personel arama + başlangıç/bitiş + izin türü filtre; sayfalama filtreleri koruyor  

### 8.3 API + JWT örneği

```bash
# Token al
curl -s -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@local","password":"Sifre123!"}'

# Dönen token ile departmanları listele
curl -s http://localhost:8080/api/departmanlar \
  -H "Authorization: Bearer <TOKEN>"
```

### 8.4 Endpoint özeti

- `POST /api/auth/login` (onaylı hesap zorunlu)  
- `POST /api/auth/change-password` (JWT)  
- `GET/POST/PUT/DELETE /api/departmanlar` (Admin/Isletmen; DELETE = soft delete, Admin)  
- `GET/POST/PUT/DELETE /api/personeller` (Staff; DELETE: Admin; oluşturmada `sifre` zorunlu)  
- `GET/POST /api/izinler`  
- `POST /api/izinler/{id}/onayla` / `reddet`  
- `GET /api/audit-logs` (Admin)  

### 8.5 Derleme kontrolü

```bash
dotnet restore BilisimPersoneli.sln
dotnet build BilisimPersoneli.sln -c Release
```

---

## 9. Dashboard (Admin / Yönetici / İşletmen)

Admin, Yönetici ve İşletmen rollerinin gördüğü özet rapor sayfasıdır. Personel rolü erişemez.

### 9.1 DTO’lar

**Dosya:** `src/BilisimPersoneli.Application/DTOs/DashboardDtos.cs`

```csharp
namespace BilisimPersoneli.Application.DTOs;

public class DepartmanMaasOzetiDto
{
    public string DepartmanAd { get; set; } = string.Empty;
    public int PersonelSayisi { get; set; }
    public decimal OrtalamaMaas { get; set; }
    public decimal ToplamMaas { get; set; }
}

public class IzinliPersonelOzetDto
{
    public string Ad { get; set; } = string.Empty;
    public string Soyad { get; set; } = string.Empty;
    public string DepartmanAd { get; set; } = string.Empty;
    public int IzinKayitSayisi { get; set; }
}

public class OrtalamaUstuPersonelDto
{
    public string Ad { get; set; } = string.Empty;
    public string Soyad { get; set; } = string.Empty;
    public string DepartmanAd { get; set; } = string.Empty;
    public decimal Maas { get; set; }
    public decimal DepartmanOrtalamaMaas { get; set; }
}

public class DashboardDto
{
    public PagedResult<DepartmanMaasOzetiDto> DepartmanMaasOzetleri { get; set; } = /* boş paged */;
    public PagedResult<IzinliPersonelOzetDto> IzinliPersoneller { get; set; } = /* boş paged */;
    public PagedResult<OrtalamaUstuPersonelDto> OrtalamaUstuPersoneller { get; set; } = /* boş paged */;
}
```

### 9.2 Interface ve servis

**`IDashboardService`**

```csharp
Task<DashboardDto> GetDashboardAsync(
    int maasPage = 1, int maasPageSize = 10,
    int izinliPage = 1, int izinliPageSize = 10,
    int ortalamaPage = 1, int ortalamaPageSize = 10,
    CancellationToken cancellationToken = default);
```

**`DashboardService` özeti** (`Infrastructure/Services/DashboardService.cs`):

1. **Departman maaş özeti:** Her departman için `Personeller.Count()`, `Sum(Maas)`, `Average(Maas)` (personel yoksa ortalama/toplam `0`). Soft delete global filter ile hariç tutulur.  
2. **İzinli personeller:** `Izinler.Any()` olan personeller; her satırda Ad, Soyad, DepartmanAd, `Izinler.Count()`. Aynı personel birden fazla izne sahip olsa bile **tek satır** görünür.  
3. **Ortalama üstü maaş:** `Departman != null` ve `Maas > Departman.Personeller.Average(Maas)` olan personeller; satırda personel maaşı + departman ortalama maaşı. Departman bilgisi olmayan personeller hariç.  
4. Her bölüm `Count` + `Skip`/`Take` ile ayrı sayfalanır ([§10](#10-sunucu-taraflı-sayfalama)).

DI: `services.AddScoped<IDashboardService, DashboardService>();`

### 9.3 MVC

**`DashboardController`**

```csharp
[Authorize(Roles = AppRoles.Staff)]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService) => _dashboardService = dashboardService;

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = await _dashboardService.GetDashboardAsync(cancellationToken);
        return View(model);
    }
}
```

**View:** `Views/Dashboard/Index.cshtml` — üç tablo (Departman Bazlı Maaş Özeti + İzin Kaydı Bulunan Personeller + Departman Ortalamasının Üzerinde Maaş Alan Personeller); maaş `N2` / TL; her tablo ayrı sayfalanır.

**Navbar:** Admin, Yonetici ve Isletmen menüsüne `Dashboard` linki (`asp-controller="Dashboard"`).

URL: `/Dashboard` (veya `/Dashboard/Index`).

---

## 10. Sunucu taraflı sayfalama

Tablo kullanan MVC listelerinde tüm kayıtlar bir kerede çekilmez. Her istekte `Count` + `Skip` / `Take` ile yalnızca seçilen sayfa boyutu kadar satır veritabanından gelir.

### 10.1 Ortak tipler

**`src/BilisimPersoneli.Application/Common/PagedResult.cs`**

- `PaginationDefaults.PageSizeOptions` = `{ 5, 10, 20, 30, 50, 100 }`  
- Varsayılan `PageSize` = `10`  
- `PagedResult<T>`: `Items`, `Page`, `PageSize`, `TotalCount`, `TotalPages`, `HasPrevious`, `HasNext`  
- `Pagination.Normalize(page, pageSize)` geçersiz boyutu varsayılana çeker  

**UI:** `PaginationUiModel` + `Views/Shared/_Pagination.cshtml`  
- Satır sayısı `<select>` (değişince `page=1` ile GET)  
- Önceki / Sonraki linkleri  
- Toplam kayıt ve sayfa bilgisi  

### 10.2 Servis imzaları

| Servis | Paged metot |
|--------|-------------|
| `IDepartmanService` | `GetPagedAsync(page, pageSize)` |
| `IPersonelService` | `GetPagedAsync(page, pageSize, search?, departmanId?)` |
| `IIzinService` | `GetPagedAsync` / `GetPagedByPersonelIdAsync` (`search?`, `baslangic?`, `bitis?`, `izinTuru?`), `GetPendingPagedAsync` |
| `IAuditLogService` | `GetPagedAsync(page, pageSize, filtreler…)` |
| `IDashboardService` | `GetDashboardAsync(maasPage/Size, izinliPage/Size, ortalamaPage/Size)` |

API controller’lar dropdown/lookup için mevcut `GetAllAsync` kullanmaya devam eder.

### 10.3 Uygulanan sayfalar

| Sayfa | Query parametreleri |
|-------|---------------------|
| Departmanlar, Onaylar | `page`, `pageSize` |
| Personeller | `page`, `pageSize`, `search`, `departmanId` ([§11](#11-personeller-arama--filtre)) |
| İzinler | `page`, `pageSize`, `search`, `baslangic`, `bitis`, `izinTuru` ([§12](#12-izinler-arama--filtre)) |
| Audit Log | `page`, `pageSize` + mevcut filtreler |
| Dashboard (3 tablo) | `maasPage`/`maasPageSize`, `izinliPage`/`izinliPageSize`, `ortalamaPage`/`ortalamaPageSize` |

Örnek controller aksiyonu:

```csharp
public async Task<IActionResult> Index(
    int page = 1,
    int pageSize = PaginationDefaults.DefaultPageSize,
    CancellationToken cancellationToken = default)
{
    return View(await _service.GetPagedAsync(page, pageSize, cancellationToken));
}
```

View modeli: `@model PagedResult<T>` → satırlar `Model.Items`; altta `@await Html.PartialAsync("_Pagination", PaginationUiModel.From(Model))`.

---

## 11. Personeller arama / filtre

Admin, Yönetici ve İşletmen `Personeller/Index` sayfasında listeyi ad/soyad ve departmana göre daraltabilir. Filtre sunucuda uygulanır; ardından sayfalama yapılır.

### 11.1 Servis

`IPersonelService.GetPagedAsync`:

```csharp
Task<PagedResult<PersonelDto>> GetPagedAsync(
    int page,
    int pageSize,
    string? search = null,
    int? departmanId = null,
    CancellationToken cancellationToken = default);
```

Kurallar:

- `search` doluysa: `Ad.Contains(term) || Soyad.Contains(term)` (trim)  
- `departmanId > 0` ise: `DepartmanId == departmanId`  
- Sonra `OrderBy Ad/Soyad` + `Count` + `Skip`/`Take`

### 11.2 Controller / View

`PersonellerController.Index` query parametreleri: `search`, `departmanId`, `page`, `pageSize`.

`Views/Personeller/Index.cshtml`:

- Ad/Soyad arama kutusu  
- Departman dropdown (`ViewBag.Departmanlar` + “Tüm departmanlar”)  
- Filtrele / Temizle  
- `_Pagination` ExtraQuery ile `search` + `departmanId` sayfa geçişinde korunur  
- Filtre submit’te `page=1`

---

## 12. İzinler arama / filtre

`Izinler/Index` (Staff: tüm izinler; Personel: kendi izinleri) sayfasında liste daraltılabilir. Filtre sunucuda uygulanır; ardından sayfalama yapılır.

### 12.1 Servis

`GetPagedAsync` / `GetPagedByPersonelIdAsync` ortak filtreler:

```csharp
string? search = null,          // Personel Ad/Soyad Contains
DateTime? baslangic = null,     // BaslangicTarihi >=
DateTime? bitis = null,         // BitisTarihi <=
IzinTuru? izinTuru = null       // IzinTuru ==
```

### 12.2 Controller / View

Query parametreleri: `search`, `baslangic`, `bitis`, `izinTuru`, `page`, `pageSize`.

`Views/Izinler/Index.cshtml`:

- Personel Ad/Soyad arama (Personel-only rolünde gizli)  
- Başlangıç (≥) / Bitiş (≤) tarih alanları  
- İzin Türü dropdown (`Yillik` / `Mazeret` / `Ucretsiz` + “Tüm türler”)  
- Filtrele / Temizle; `_Pagination` ExtraQuery ile filtreler korunur; submit’te `page=1`

---

## 13. Kullanıcı kaydı, onay ve rol yönetimi

Self-service kayıt, Admin/Yönetici onayı ve **tek birincil rol** atama paneli.

### 13.0 Kullanıcı–rol tasarımı (özet)

İki katmanı ayırın; birini diğerine gömmeyin:

| Katman | Ne tutar | Nerede |
|--------|----------|--------|
| **Kimlik / yetki** | Giriş, şifre, `IsApproved`, roller | ASP.NET Identity (`ApplicationUser` + `AspNetRoles`) |
| **İK kaydı** | Ad, maaş, departman, izinler | `Personel` + `IdentityUserId` köprüsü |

**Kural:** Her Personel bir Identity kullanıcısıdır (oluşturma/register → varsayılan rol `Personel`). Seed staff (`admin`/`yonetici`/`isletmen`) için Personel satırı zorunlu değildir. Domain’e `Personel.Rol` eklenmez.

| Rol | Amaç |
|-----|------|
| **Personel** | Varsayılan. Kendi izinleri. |
| **Isletmen** | Operasyon: Departman/Personel/İzin CRUD (silme/onay yok). |
| **Yonetici** | Personel + izin onayı + kayıt onayı; sınırlı rol atama. |
| **Admin** | Tam yetki: silme, audit, tüm roller. |

**Kim ne atar:** Admin → dört rol; Yonetici → `Personel` / `Isletmen` / `Yonetici` (`Admin` atayamaz; son Admin düşürülemez). UI: radio (tek birincil rol).

### 13.1 Identity modeli

- `ApplicationUser : IdentityUser` → `bool IsApproved`  
- Dosya: `src/BilisimPersoneli.Infrastructure/Identity/ApplicationUser.cs`  
- Migration: `AddUserApprovalAndIsletmen` (`AspNetUsers.IsApproved`)  
- Seed kullanıcıları onaylı; Register ile gelenler onaysız  

### 13.2 Application

| Parça | Path |
|-------|------|
| DTO | `Application/DTOs/UserManagementDtos.cs` (`RegisterDto`, `UserListItemDto`, `UserRolesUpdateDto`) |
| Interface | `Application/Interfaces/IUserManagementService.cs` |
| Roller | `AppRoles.Isletmen`, `Staff`, `All` |

### 13.3 Servis akışı

`UserManagementService` + `PersonelService`:

1. **Register:** departman + e-posta kontrolü → `ApplicationUser` (`IsApproved=false`) + tek rol `Personel` + `Personel` kaydı (`Maas=0`, `IdentityUserId`)  
2. **Personel Create (Staff):** aynı bağ; `IsApproved=true` + varsayılan rol `Personel`  
3. **Approve:** `IsApproved=true` + audit  
4. **Reject:** onaysız kullanıcı + bağlı Personel silinir (izin varsa engel)  
5. **UpdateRoles:** yalnızca onaylı kullanıcı; **tek birincil rol**; Yonetici `Admin` atayamaz; son Admin koruması  
6. **Personel soft-delete:** bağlı Identity `Lockout` / `IsApproved=false`  

### 13.4 MVC

| Sayfa | Yetki | Açıklama |
|-------|-------|----------|
| `/Account/Register` | Anonim | Personel kaydı formu |
| `/Users/Pending` | Admin + Yonetici | Onay bekleyenler |
| `/Users/Index` | Admin + Yonetici | Tüm kullanıcılar + filtre |
| `/Users/Roles/{id}` | Admin + Yonetici | **Radio** ile tek birincil rol (Yonetici’ye Admin seçeneği yok) |

Login (MVC + API): `IsApproved == false` → giriş/token yok.

### 13.5 Isletmen yetkileri

| Alan | Erişim |
|------|--------|
| Dashboard, Departman, Personel, İzin listesi/CRUD | Evet |
| Soft delete / Restore | Hayır (Admin) |
| İzin Onayları | Hayır |
| Kullanıcılar / rol atama | Hayır |
| Audit Log | Hayır |

### 13.6 Kontrol listesi

- [x] `ApplicationUser` + migration  
- [x] `IUserManagementService` / DI  
- [x] Register + Login onay kontrolü  
- [x] `UsersController` + Views  
- [x] Isletmen seed + navbar / Authorize  
- [x] Kullanıcı–rol tasarımı bu rehbere / `TODO_LIST.md` işlendi (§13.0)  
- [x] Tek birincil rol (`UpdateRolesAsync` + Roles radio)  
- [x] Yonetici sınırlı rol atama + Index/Roles yetkisi + navbar  
- [x] Son Admin koruması  
- [x] Personel soft-delete → Identity kilidi (Restore ile açılır)  

---

## Hızlı referans — kurulum sırası

1. Önkoşullar (.NET 8, Docker, opsiyonel `dotnet-ef` 8.x)  
2. Solution + 4 proje + referanslar + NuGet  
3. Domain: `BaseEntity`, enum’lar, entity’ler  
4. Application: DTO, interface, `MappingProfile`, `AppRoles`, `ServiceResult`, `PagedResult`  
5. Infrastructure: `ApplicationUser`, `AppDbContext`, soft delete/audit, Identity DI, servisler, `DbSeeder`  
6. Migration (`InitialCreate` + `AddUserApprovalAndIsletmen`)  
7. Web: `Program.cs`, cookie + JWT, MVC Views, API controllers  
8. `Dockerfile` + `docker-compose.yml`  
9. Dashboard: DTO + `IDashboardService` + MVC (Staff)  
10. Sayfalama: `GetPagedAsync` + `_Pagination` (tüm tablo listeleri)  
11. Personeller: Ad/Soyad arama + Departman filtre  
12. İzinler: Personel arama + tarih + izin türü filtre  
13. Kullanıcı kaydı / onay / rol paneli + Isletmen  
14. Tek birincil rol + Yonetici sınırlı rol atama + Personel silince hesap kilidi (§13.0 / §13.6)  
15. `docker compose up --build` → http://localhost:8080  
16. Smoke test (dört rol + kayıt onayı + sınırlı rol atama + JWT + Dashboard + sayfalama + filtre)  

---

## Sorun giderme

| Belirti | Olası neden | Çözüm |
|---------|-------------|--------|
| Web DB’ye bağlanamıyor | SQL henüz hazır değil | Seed retry bekleyin; `docker compose logs sqlserver` |
| Connection error container içinde | Host `localhost` kullanılmış | Compose’da `Server=sqlserver,1433` |
| SA şifre reddedildi | Zayıf şifre politikası | En az 8 karakter, büyük/küçük/rakam/sembol |
| Migration CLI hata | SQL ayakta değil / factory connection | `docker compose up sqlserver -d` |
| `dotnet-ef` System.Runtime 10 hatası | EF tools 11 + net8 proje | `dotnet tool install -g dotnet-ef --version 8.0.11` |
| 401 API | Token yok / yanlış scheme / onaysız hesap | Bearer + JWT scheme; önce kayıt onayı |
| 403 MVC | Yanlış rol | Admin/Yonetici/Isletmen/Personel menü ve `[Authorize(Roles=…)]` |
| Login “henüz onaylanmadı” | `IsApproved=false` | Admin/Yonetici → Kayıt Onayları |

---

Bu rehber, repodaki mevcut implementasyonu sıfırdan yeniden üretecek sırayı tanımlar. Kodun güncel hali için ilgili path’lere bakın; uzun servis/view dosyalarının tamamı `src/` altında bulunur.
