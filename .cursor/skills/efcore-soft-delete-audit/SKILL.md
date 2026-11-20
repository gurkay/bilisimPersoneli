---
name: efcore-soft-delete-audit
description: >-
  Implements EF Core with SQL Server, soft delete global filters, SaveChanges
  overrides, and AuditLog writing for BilisimPersoneli entities. Use when
  creating DbContext, migrations, soft delete, audit trail, Maas column,
  query filters, or seed data.
---

# EF Core: Soft Delete & Audit Log

## DbContext

- `AppDbContext : IdentityDbContext<IdentityUser>`
- Sets: `Departmanlar`, `Personeller`, `Izinler`, `AuditLogs`
- Provider: SQL Server
- On startup: `Database.Migrate()` then seed

## BaseEntity

```text
IsDeleted, DeletedAt
CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
```

`Departman`, `Personel`, `Izin` inherit this. `AuditLog` is separate (do not soft-delete audit rows).

## Personel.Maas

- Type: `decimal(18,2)`, required
- Rule: `Maas >= 0`
- Show in list/detail/API DTOs

## Soft delete

1. Global query filter: `e => !e.IsDeleted`
2. On `SaveChanges`: convert `EntityState.Deleted` → set `IsDeleted=true`, `DeletedAt=UtcNow`, state → Modified
3. Lists hide deleted by default
4. Admin optional Restore: clear `IsDeleted`/`DeletedAt`
5. Block soft delete when active children exist (departman→personel, personel→izin) — show Turkish error message

## AuditLog entity

| Field | Purpose |
|-------|---------|
| EntityName, EntityId | What changed |
| Action | Create / Update / SoftDelete / Restore / Approve / Reject |
| UserId, UserName | Who |
| Timestamp | When (UTC) |
| Details | Short summary or compact JSON |

Write audit entries in the same `SaveChanges` path (or thin interceptor). Keep it simple — one row per action.

## Fluent config essentials

- Unique: `Departman.Ad`, `Personel.Email`
- FK: Restrict delete
- String max lengths (Ad 100, Email 256, etc.)
- Indexes as needed for list filters

## Migrations

- Keep migrations in Infrastructure
- Design-time factory if `dotnet ef` needs it
- First migration: `InitialCreate`

## Seed order

1. Roles: Admin, Yonetici, Personel
2. Users + role assignment
3. Sample Departman(s)
4. Sample Personel with `Maas`
5. Optional sample Beklemede Izin

## Query tips

- Use `Include` for Departman name on Personel lists, Personel name on Izin lists
- For Admin “show deleted”: `IgnoreQueryFilters()` + `Where(e => e.IsDeleted)`
