---
name: bilisim-personeli-workflow
description: >-
  Orchestrates implementation of the BilisimPersoneli exam project from
  TODO_LIST.md as an expert fullstack .NET developer. Use when starting
  development, implementing features in order, marking TODO items, scaffolding
  the app, or when the user asks to build/kodla/geliştir the project.
---

# Bilisim Personeli — Expert Fullstack Workflow

## Role

Act as a senior fullstack .NET developer: clean, exam-friendly code; prefer clarity over clever abstractions.

## Source of truth

Follow [TODO_LIST.md](../../../TODO_LIST.md). Check items off as completed. Do not invent MediatR/CQRS/file upload unless the user asks.

## Implementation order

1. Solution + 4 projects + references + packages  
2. Domain (`BaseEntity`, entities, enums, `Maas`)  
3. Infrastructure (DbContext, soft delete, audit, Identity, migration, multi-role seed)  
4. Application (DTOs, AutoMapper, services)  
5. Web MVC (cookie auth, role UI, CRUD, onay screens, audit view)  
6. API + JWT  
7. Docker Compose + Dockerfile  
8. README + smoke test  

## Apply specialized skills

| Task | Skill |
|------|--------|
| Layers / references / DI | [dotnet-clean-architecture](../dotnet-clean-architecture/SKILL.md) |
| Login, roles, JWT, onay | [aspnet-auth-roles-jwt](../aspnet-auth-roles-jwt/SKILL.md) |
| EF, soft delete, audit, Maas | [efcore-soft-delete-audit](../efcore-soft-delete-audit/SKILL.md) |
| MVC/API/AutoMapper CRUD | [mvc-api-automapper-crud](../mvc-api-automapper-crud/SKILL.md) |
| Compose / Dockerfile | [docker-sqlserver-compose](../docker-sqlserver-compose/SKILL.md) |

## Quality bar

- Turkish UI labels
- Thin controllers, logic in services
- Soft delete everywhere (no hard delete for business entities)
- Audit on Create/Update/SoftDelete/Restore/Approve/Reject
- `Personel.Maas` required, `>= 0`, shown in UI/API
- Same rules on MVC and API
- Readable names; no drive-by refactors outside the task

## Out of scope (unless asked)

- MediatR / CQRS
- File uploads
- Unit test project (first delivery)

## Done means

Smoke tests in TODO_LIST §11 pass: compose up, three roles login, CRUD + Maas, leave approve/reject, JWT API, soft delete hidden from lists, unauthorized blocked.
