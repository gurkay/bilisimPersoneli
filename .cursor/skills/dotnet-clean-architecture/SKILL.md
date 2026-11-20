---
name: dotnet-clean-architecture
description: >-
  Implements simplified Clean Architecture for ASP.NET Core 8 (Domain,
  Application, Infrastructure, Web). Use when creating the solution structure,
  project references, layer boundaries, DI registration, or when the user
  mentions Clean Architecture, katmanlar, Domain/Application/Infrastructure.
---

# .NET Clean Architecture (Simplified)

## Stack defaults

- .NET 8 / ASP.NET Core 8 MVC
- No MediatR, no CQRS
- Entity services + interfaces (no mandatory generic repository)
- Follow [TODO_LIST.md](../../../TODO_LIST.md)

## Solution layout

```text
src/
  BilisimPersoneli.Domain/
  BilisimPersoneli.Application/
  BilisimPersoneli.Infrastructure/
  BilisimPersoneli.Web/
```

## Dependency rule

```text
Web → Application, Infrastructure
Infrastructure → Application, Domain
Application → Domain
Domain → (nothing)
```

Never reference `Web` or `Infrastructure` from `Domain` / `Application`.

## What belongs where

| Layer | Put here | Do not put here |
|-------|----------|-----------------|
| Domain | Entities, enums, `BaseEntity` | EF attributes requiring infra packages, UI |
| Application | Interfaces, DTOs, AutoMapper profiles, validation contracts | DbContext, Identity, Controllers |
| Infrastructure | EF Core, Identity, JWT helpers, service implementations | Razor views, MVC controllers |
| Web | Controllers, Views, `Program.cs` DI, API controllers | Business rules duplicated from services |

## Domain entities (this project)

- `Departman`, `Personel` (includes `Maas` decimal), `Izin`, `AuditLog`
- Enums: `IzinTuru`, `IzinDurumu`
- Soft delete + audit fields on `BaseEntity`

## DI registration

Register everything in `Web/Program.cs` (or `DependencyInjection` extension in Infrastructure called from Web):

1. DbContext + SQL Server
2. Identity + Cookie + JWT
3. AutoMapper
4. Application service implementations
5. `Migrate()` + seed on startup

## Checklist when scaffolding

- [ ] Four projects + correct references
- [ ] Domain has zero NuGet infra deps
- [ ] Application has AutoMapper package only (as needed)
- [ ] Infrastructure implements Application interfaces
- [ ] Web has no business logic beyond HTTP/UI mapping

## Anti-patterns

- Controllers talking directly to `DbContext`
- Domain depending on EF Core
- Fat controllers with approval/soft-delete rules
- Adding MediatR/CQRS “for cleanliness”
---

# Additional resources

- Project checklist: [TODO_LIST.md](../../../TODO_LIST.md)
- Auth skill: [aspnet-auth-roles-jwt](../aspnet-auth-roles-jwt/SKILL.md)
- EF skill: [efcore-soft-delete-audit](../efcore-soft-delete-audit/SKILL.md)
