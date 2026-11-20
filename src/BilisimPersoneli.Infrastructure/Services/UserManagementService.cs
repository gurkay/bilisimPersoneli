using BilisimPersoneli.Application.Common;
using BilisimPersoneli.Application.DTOs;
using BilisimPersoneli.Application.Interfaces;
using BilisimPersoneli.Domain.Entities;
using BilisimPersoneli.Infrastructure.Identity;
using BilisimPersoneli.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BilisimPersoneli.Infrastructure.Services;

public class UserManagementService : IUserManagementService
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUserService _currentUser;

    public UserManagementService(
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        ICurrentUserService currentUser)
    {
        _db = db;
        _userManager = userManager;
        _currentUser = currentUser;
    }

    public async Task<ServiceResult> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default)
    {
        if (!await _db.Departmanlar.AnyAsync(x => x.Id == dto.DepartmanId, cancellationToken))
            return ServiceResult.Fail("Seçilen departman bulunamadı.");

        if (await _db.Personeller.AnyAsync(x => x.Email == dto.Email, cancellationToken))
            return ServiceResult.Fail("Bu e-posta adresi zaten kayıtlı.");

        if (await _userManager.FindByEmailAsync(dto.Email) != null)
            return ServiceResult.Fail("Bu e-posta ile zaten bir kullanıcı hesabı var.");

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            EmailConfirmed = true,
            IsApproved = false
        };

        var createResult = await _userManager.CreateAsync(user, dto.Sifre);
        if (!createResult.Succeeded)
            return ServiceResult.Fail(FormatIdentityErrors(createResult));

        var roleResult = await _userManager.AddToRoleAsync(user, AppRoles.Personel);
        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);
            return ServiceResult.Fail(FormatIdentityErrors(roleResult));
        }

        _db.Personeller.Add(new Personel
        {
            Ad = dto.Ad.Trim(),
            Soyad = dto.Soyad.Trim(),
            Email = dto.Email.Trim(),
            Telefon = dto.Telefon,
            Maas = 0m,
            IseGirisTarihi = DateTime.Today,
            DepartmanId = dto.DepartmanId,
            IdentityUserId = user.Id
        });

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await _userManager.DeleteAsync(user);
            throw;
        }

        return ServiceResult.Success();
    }

    public async Task<IReadOnlyList<UserListItemDto>> GetUsersAsync(
        bool? approved = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = _userManager.Users.AsNoTracking();

        if (approved.HasValue)
            query = query.Where(u => u.IsApproved == approved.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(u =>
                (u.Email != null && u.Email.Contains(term)) ||
                (u.UserName != null && u.UserName.Contains(term)));
        }

        var users = await query.OrderBy(u => u.Email).ToListAsync(cancellationToken);
        var personeller = await _db.Personeller
            .AsNoTracking()
            .Include(p => p.Departman)
            .Where(p => p.IdentityUserId != null)
            .ToListAsync(cancellationToken);
        var personelByUserId = personeller
            .Where(p => p.IdentityUserId != null)
            .ToDictionary(p => p.IdentityUserId!);

        var result = new List<UserListItemDto>();
        foreach (var user in users)
        {
            personelByUserId.TryGetValue(user.Id, out var personel);
            var roles = await _userManager.GetRolesAsync(user);
            result.Add(Map(user, roles, personel));
        }

        return result;
    }

    public async Task<UserListItemDto?> GetByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return null;

        var personel = await _db.Personeller
            .AsNoTracking()
            .Include(p => p.Departman)
            .FirstOrDefaultAsync(p => p.IdentityUserId == userId, cancellationToken);
        var roles = await _userManager.GetRolesAsync(user);
        return Map(user, roles, personel);
    }

    public async Task<ServiceResult> ApproveAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return ServiceResult.Fail("Kullanıcı bulunamadı.");

        if (user.IsApproved)
            return ServiceResult.Fail("Kullanıcı zaten onaylı.");

        user.IsApproved = true;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return ServiceResult.Fail(FormatIdentityErrors(result));

        _db.AddManualAudit("ApplicationUser", user.Id, Domain.Enums.AuditAction.Approve, $"Kayıt onaylandı: {user.Email}");
        await _db.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> RejectAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return ServiceResult.Fail("Kullanıcı bulunamadı.");

        if (user.IsApproved)
            return ServiceResult.Fail("Onaylı kullanıcı reddedilemez; rollerini düzenleyebilirsiniz.");

        var personel = await _db.Personeller.FirstOrDefaultAsync(p => p.IdentityUserId == userId, cancellationToken);
        if (personel != null)
        {
            var hasIzin = await _db.Izinler.AnyAsync(x => x.PersonelId == personel.Id, cancellationToken);
            if (hasIzin)
                return ServiceResult.Fail("Personele bağlı izin kaydı olduğu için red işlemi yapılamaz.");

            _db.Personeller.Remove(personel);
            await _db.SaveChangesAsync(cancellationToken);
        }

        var deleteResult = await _userManager.DeleteAsync(user);
        if (!deleteResult.Succeeded)
            return ServiceResult.Fail(FormatIdentityErrors(deleteResult));

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> UpdateRolesAsync(UserRolesUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(dto.UserId);
        if (user == null)
            return ServiceResult.Fail("Kullanıcı bulunamadı.");

        if (!user.IsApproved)
            return ServiceResult.Fail("Önce kullanıcı kaydını onaylayın.");

        var selected = (dto.SelectedRoles ?? Array.Empty<string>())
            .Where(r => AppRoles.All.Contains(r))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (selected.Count != 1)
            return ServiceResult.Fail("Tek bir birincil rol seçilmelidir.");

        var newRole = selected[0];
        var actorIsAdmin = _currentUser.IsInRole(AppRoles.Admin);
        var actorIsYonetici = _currentUser.IsInRole(AppRoles.Yonetici);

        if (!actorIsAdmin && !actorIsYonetici)
            return ServiceResult.Fail("Rol atama yetkiniz yok.");

        var current = await _userManager.GetRolesAsync(user);
        var targetIsAdmin = current.Contains(AppRoles.Admin, StringComparer.OrdinalIgnoreCase);

        if (!actorIsAdmin)
        {
            if (targetIsAdmin)
                return ServiceResult.Fail("Yönetici, Admin rolündeki kullanıcının rolünü değiştiremez.");

            if (!AppRoles.AssignableByYonetici.Contains(newRole, StringComparer.OrdinalIgnoreCase))
                return ServiceResult.Fail("Yönetici yalnızca Personel, İşletmen veya Yönetici rolü atayabilir.");
        }

        if (targetIsAdmin &&
            !newRole.Equals(AppRoles.Admin, StringComparison.OrdinalIgnoreCase))
        {
            var admins = await _userManager.GetUsersInRoleAsync(AppRoles.Admin);
            if (admins.Count <= 1)
                return ServiceResult.Fail("Son Admin rolü kaldırılamaz.");
        }

        var desired = new[] { newRole };
        var toRemove = current.Except(desired, StringComparer.OrdinalIgnoreCase).ToList();
        var toAdd = desired.Except(current, StringComparer.OrdinalIgnoreCase).ToList();

        if (toRemove.Count > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, toRemove);
            if (!removeResult.Succeeded)
                return ServiceResult.Fail(FormatIdentityErrors(removeResult));
        }

        if (toAdd.Count > 0)
        {
            var addResult = await _userManager.AddToRolesAsync(user, toAdd);
            if (!addResult.Succeeded)
                return ServiceResult.Fail(FormatIdentityErrors(addResult));
        }

        _db.AddManualAudit(
            "ApplicationUser",
            user.Id,
            Domain.Enums.AuditAction.Update,
            $"Birincil rol güncellendi: {newRole}");
        await _db.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    private static UserListItemDto Map(ApplicationUser user, IList<string> roles, Personel? personel) => new()
    {
        Id = user.Id,
        Email = user.Email ?? string.Empty,
        UserName = user.UserName,
        IsApproved = user.IsApproved,
        Roles = roles.ToList(),
        PersonelId = personel?.Id,
        PersonelAdSoyad = personel == null ? null : $"{personel.Ad} {personel.Soyad}",
        DepartmanAd = personel?.Departman?.Ad
    };

    private static string FormatIdentityErrors(IdentityResult result)
        => string.Join(" ", result.Errors.Select(e => e.Description));
}
