---
name: mvc-api-automapper-crud
description: >-
  Builds Razor Views + Bootstrap 5 MVC CRUD and REST API endpoints using
  AutoMapper DTOs for Departman, Personel (Maas), and Izin. Use when creating
  controllers, views, API routes, ViewModels/DTOs, mapping profiles, or Turkish
  UI forms.
---

# MVC + API CRUD with AutoMapper

## Principle

MVC controllers and API controllers call the **same Application services**. Controllers stay thin; mapping via AutoMapper.

## AutoMapper

- Package on Application: `AutoMapper.Extensions.Microsoft.DependencyInjection`
- Profiles under `Application/Mapping`
- Map Entity ↔ DTO / CreateDto / UpdateDto
- Register: `builder.Services.AddAutoMapper(typeof(AssemblyMarker))`
- Never map Identity password hashes or secrets into DTOs

## Services

| Interface | Responsibilities |
|-----------|------------------|
| `IDepartmanService` | List/Get/Create/Update/SoftDelete |
| `IPersonelService` | Same + Maas validation + Departman include |
| `IIzinService` | CRUD + Onayla/Reddet + role-scoped queries |
| `IAuditLogService` | List/filter (Admin) |

Validation: Data Annotations on DTOs + service checks (`Bitis >= Baslangic`, unique email/ad, `Maas >= 0`).

## MVC controllers

- `DepartmanlarController`, `PersonellerController`, `IzinlerController`, `AuditLogsController`, `AccountController`
- Actions: Index, Details, Create, Edit, Delete (soft)
- Izin: Onayla / Reddet POST actions for Yonetici/Admin
- TempData flash messages (Turkish)
- Anti-forgery on POSTs

## Views / UI

- Bootstrap 5 layout + role-based navbar
- Turkish labels
- Personel forms: include **Maas** (format `N2`)
- Dropdowns: Departman, Personel, IzinTuru; Durum mostly workflow-driven
- Validation summary + field errors
- No over-designed dashboards — clean CRUD screens

## API routes (JWT)

```text
POST   /api/auth/login
GET|POST|PUT|DELETE  /api/departmanlar
GET|POST|PUT|DELETE  /api/personeller
GET|POST             /api/izinler
POST   /api/izinler/{id}/onayla
POST   /api/izinler/{id}/reddet
GET    /api/audit-logs
```

DELETE = soft delete. Return proper status codes (400 validation, 401/403 auth, 404 not found).

## CRUD checklist per module

- [ ] Service + DTO + Profile
- [ ] MVC views work authenticated
- [ ] API returns mapped DTOs
- [ ] Soft delete + audit fired
- [ ] Role restrictions enforced
