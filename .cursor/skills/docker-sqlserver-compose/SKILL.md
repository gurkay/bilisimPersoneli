---
name: docker-sqlserver-compose
description: >-
  Sets up Docker Compose with SQL Server and ASP.NET Core Web for
  BilisimPersoneli, including healthchecks, connection strings, JWT env vars,
  and multi-stage Dockerfile. Use when writing docker-compose.yml, Dockerfile,
  or fixing container startup/DB connection issues.
---

# Docker Compose + SQL Server

## Services

1. **sqlserver** — official Microsoft SQL Server image
2. **web** — build from `src/BilisimPersoneli.Web/Dockerfile`

## sqlserver essentials

- Accept EULA via env
- Strong `SA_PASSWORD` via env (not hardcoded in git if possible; document example)
- Named volume for data
- Healthcheck (`/opt/mssql-tools*/bin/sqlcmd` or equivalent for image version)
- Expose 1433 for local `dotnet run` against container DB

## web essentials

- `depends_on` with condition: service healthy
- Connection string env:
  `Server=sqlserver;Database=BilisimPersoneli;User Id=sa;Password=...;TrustServerCertificate=True`
- JWT key/issuer/audience via env
- Wait/retry on DB if race remains after healthcheck

## Multi-stage Dockerfile

```text
restore → build → publish → runtime (aspnet 8)
```

- Build context: solution root
- Copy `.sln` + all project files first for layer caching
- Expose 8080 (or 80); set `ASPNETCORE_URLS`

## Local vs Compose

| Mode | DB | App |
|------|----|-----|
| Full compose | sqlserver service | web service |
| Dev hybrid | `docker compose up sqlserver` | `dotnet run --project src/...Web` |

## README must document

- `docker compose up --build`
- Default users/roles/passwords
- JWT sample curl
- Connection string for local SDK run

## Common failures

- Web starts before SQL ready → healthcheck + retry
- Wrong server hostname (`localhost` inside container is wrong; use service name `sqlserver`)
- Password policy rejected by SQL image → stronger SA password
- EF migration fails on boot → fix connection / ensure migrate in Program.cs
