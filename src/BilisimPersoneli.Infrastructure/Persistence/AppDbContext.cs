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
    public DbSet<PersonelTest> PersonelTests => Set<PersonelTest>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(e =>
        {
            e.Property(x => x.IsApproved).HasDefaultValue(false);
        });

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
            e.HasMany(x => x.PersonelTestler)
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

        builder.Entity<PersonelTest>(e =>
        {
            e.Property(x => x.Ad).HasMaxLength(100).IsRequired();
            e.Property(x => x.Soyad).HasMaxLength(100).IsRequired();
            e.HasQueryFilter(x => !x.IsDeleted);
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
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAt = now;
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedBy = userName;
                auditEntries.Add(CreateAudit(entry, AuditAction.SoftDelete, userId, userName, now));
            }
        }

        if (auditEntries.Count > 0)
        {
            AuditLogs.AddRange(auditEntries);
        }
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
