---
name: aspnet-auth-roles-jwt
description: >-
  Configures ASP.NET Core Identity with cookie auth for MVC, JWT Bearer for
  API, and multi-role authorization (Admin, Yonetici, Personel) plus leave
  approval workflow. Use when implementing login, roles, Authorize attributes,
  JWT tokens, izin onay, or Identity seed users.
---

# ASP.NET Identity, Roles & JWT

## Auth model

| Client | Scheme | Use |
|--------|--------|-----|
| Razor MVC | Cookie | Browser login/logout |
| REST API | JWT Bearer | `POST /api/auth/login` → token |

Both share the same Identity users/roles store.

## Roles

| Role | Capabilities |
|------|----------------|
| `Admin` | Full CRUD, soft delete/restore, audit log, approve/reject |
| `Yonetici` | View personel; approve/reject pending leaves |
| `Personel` | Create/list own leave requests only |

## Seed users (document in README)

- `admin@local` → Admin
- `yonetici@local` → Yonetici
- `personel@local` → Personel

## MVC authorization

```csharp
[Authorize(Roles = "Admin")]
public class DepartmanlarController : Controller { }

[Authorize(Roles = "Admin,Yonetici")]
public class ... // read-only personel for Yonetici

[Authorize(Roles = "Personel,Yonetici,Admin")]
public class IzinlerController : Controller { }
```

- Unauthorized → redirect to Login (cookie)
- Forbidden role → 403
- Navbar menus filtered by role

## Leave approval workflow

1. Personel creates `Izin` → `Durum = Beklemede`
2. Yonetici/Admin **Onayla** → `Onaylandi` + `OnaylayanUserId`, `OnayTarihi`
3. Yonetici/Admin **Reddet** → `Reddedildi` + optional `OnayNotu`
4. Only `Beklemede` can be approved/rejected
5. Personel cannot edit approved/rejected leaves
6. Write `AuditLog` on approve/reject

Optional: link `Personel.IdentityUserId` so Personel role sees only own records.

## JWT setup (Web)

1. Add `Microsoft.AspNetCore.Authentication.JwtBearer`
2. Configure `Jwt:Issuer`, `Jwt:Audience`, `Jwt:Key`, `Jwt:ExpireMinutes` (env override in Docker)
3. `AddAuthentication().AddCookie(...).AddJwtBearer(...)`
4. API controllers use JWT scheme (or dual-scheme policy)
5. Login endpoint validates password via `SignInManager`/`UserManager`, returns JWT claims: `sub`, `email`, `role`

## API auth checklist

- [ ] `POST /api/auth/login` returns token
- [ ] Protected endpoints reject missing/invalid token
- [ ] Role checks match MVC rules
- [ ] README includes sample curl with `Authorization: Bearer`

## Security defaults

- Never commit real secrets; Docker env for JWT key and SQL password
- Use HTTPS in production notes; Development may use HTTP in compose
- Passwords only via Identity hasher — never store plain text
