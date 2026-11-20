using AutoMapper;
using BilisimPersoneli.Application.Common;
using BilisimPersoneli.Application.DTOs;
using BilisimPersoneli.Application.Interfaces;
using BilisimPersoneli.Domain.Entities;
using BilisimPersoneli.Infrastructure.Identity;
using BilisimPersoneli.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BilisimPersoneli.Infrastructure.Services;

public class PersonelService : IPersonelService
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;
    private readonly UserManager<ApplicationUser> _userManager;

    public PersonelService(AppDbContext db, IMapper mapper, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _mapper = mapper;
        _userManager = userManager;
    }

    public async Task<IReadOnlyList<PersonelDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _db.Personeller
            .AsNoTracking()
            .Include(x => x.Departman)
            .OrderBy(x => x.Ad)
            .ThenBy(x => x.Soyad)
            .ToListAsync(cancellationToken);
        return _mapper.Map<List<PersonelDto>>(list);
    }

    public async Task<PagedResult<PersonelDto>> GetPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        int? departmanId = null,
        CancellationToken cancellationToken = default)
    {
        var (normalizedPage, normalizedSize) = Pagination.Normalize(page, pageSize);
        var query = _db.Personeller
            .AsNoTracking()
            .Include(x => x.Departman)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.Ad.Contains(term) || x.Soyad.Contains(term));
        }

        if (departmanId.HasValue && departmanId.Value > 0)
            query = query.Where(x => x.DepartmanId == departmanId.Value);

        query = query.OrderBy(x => x.Ad).ThenBy(x => x.Soyad);

        var total = await query.CountAsync(cancellationToken);
        var list = await query
            .Skip((normalizedPage - 1) * normalizedSize)
            .Take(normalizedSize)
            .ToListAsync(cancellationToken);
        return PagedResult<PersonelDto>.Create(_mapper.Map<List<PersonelDto>>(list), normalizedPage, normalizedSize, total);
    }

    public async Task<PersonelDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Personeller
            .AsNoTracking()
            .Include(x => x.Departman)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity == null ? null : _mapper.Map<PersonelDto>(entity);
    }

    public async Task<PersonelDto?> GetByIdentityUserIdAsync(string identityUserId, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Personeller
            .AsNoTracking()
            .Include(x => x.Departman)
            .FirstOrDefaultAsync(x => x.IdentityUserId == identityUserId, cancellationToken);
        return entity == null ? null : _mapper.Map<PersonelDto>(entity);
    }

    public async Task<ServiceResult<PersonelDto>> CreateAsync(PersonelCreateDto dto, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateAsync(dto, null, requirePassword: true, cancellationToken);
        if (validation != null)
            return ServiceResult<PersonelDto>.Fail(validation);

        var userResult = await CreateIdentityUserAsync(dto.Email, dto.Sifre!);
        if (!userResult.Succeeded)
            return ServiceResult<PersonelDto>.Fail(userResult.Error!);

        var entity = _mapper.Map<Personel>(dto);
        entity.IdentityUserId = userResult.Data!;
        _db.Personeller.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        await _db.Entry(entity).Reference(x => x.Departman).LoadAsync(cancellationToken);
        return ServiceResult<PersonelDto>.Success(_mapper.Map<PersonelDto>(entity));
    }

    public async Task<ServiceResult<PersonelDto>> UpdateAsync(PersonelUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Personeller.FirstOrDefaultAsync(x => x.Id == dto.Id, cancellationToken);
        if (entity == null)
            return ServiceResult<PersonelDto>.Fail("Personel bulunamadı.");

        var requirePassword = string.IsNullOrWhiteSpace(entity.IdentityUserId);
        var validation = await ValidateAsync(dto, dto.Id, requirePassword, cancellationToken);
        if (validation != null)
            return ServiceResult<PersonelDto>.Fail(validation);

        var identityResult = await SyncIdentityUserAsync(entity, dto);
        if (!identityResult.Succeeded)
            return ServiceResult<PersonelDto>.Fail(identityResult.Error!);

        entity.Ad = dto.Ad;
        entity.Soyad = dto.Soyad;
        entity.Email = dto.Email;
        entity.Telefon = dto.Telefon;
        entity.Maas = dto.Maas;
        entity.IseGirisTarihi = dto.IseGirisTarihi;
        entity.DepartmanId = dto.DepartmanId;

        await _db.SaveChangesAsync(cancellationToken);
        await _db.Entry(entity).Reference(x => x.Departman).LoadAsync(cancellationToken);
        return ServiceResult<PersonelDto>.Success(_mapper.Map<PersonelDto>(entity));
    }

    public async Task<ServiceResult> SoftDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Personeller.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
            return ServiceResult.Fail("Personel bulunamadı.");

        var hasIzin = await _db.Izinler.AnyAsync(x => x.PersonelId == id, cancellationToken);
        if (hasIzin)
            return ServiceResult.Fail("Bu personele bağlı izin kaydı olduğu için silinemez.");

        if (!string.IsNullOrWhiteSpace(entity.IdentityUserId))
        {
            var lockResult = await SetIdentityAccessAsync(entity.IdentityUserId, enabled: false);
            if (!lockResult.Succeeded)
                return lockResult;
        }

        _db.Personeller.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> RestoreAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Personeller.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted, cancellationToken);
        if (entity == null)
            return ServiceResult.Fail("Silinmiş personel bulunamadı.");

        if (!string.IsNullOrWhiteSpace(entity.IdentityUserId))
        {
            var unlockResult = await SetIdentityAccessAsync(entity.IdentityUserId, enabled: true);
            if (!unlockResult.Succeeded)
                return unlockResult;
        }

        entity.IsDeleted = false;
        entity.DeletedAt = null;
        await _db.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    private async Task<ServiceResult> SetIdentityAccessAsync(string identityUserId, bool enabled)
    {
        var user = await _userManager.FindByIdAsync(identityUserId);
        if (user == null)
            return ServiceResult.Success();

        user.IsApproved = enabled;
        user.LockoutEnabled = true;
        user.LockoutEnd = enabled ? null : DateTimeOffset.MaxValue;

        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded
            ? ServiceResult.Success()
            : ServiceResult.Fail(string.Join(" ", result.Errors.Select(e => e.Description)));
    }

    private async Task<string?> ValidateAsync(
        PersonelCreateDto dto,
        int? excludeId,
        bool requirePassword,
        CancellationToken cancellationToken)
    {
        if (dto.Maas < 0)
            return "Maaş negatif olamaz.";

        if (!await _db.Departmanlar.AnyAsync(x => x.Id == dto.DepartmanId, cancellationToken))
            return "Seçilen departman bulunamadı.";

        var emailExists = await _db.Personeller.AnyAsync(
            x => x.Email == dto.Email && (!excludeId.HasValue || x.Id != excludeId.Value),
            cancellationToken);
        if (emailExists)
            return "Bu e-posta adresi zaten kayıtlı.";

        if (requirePassword && string.IsNullOrWhiteSpace(dto.Sifre))
            return "Şifre zorunludur.";

        if (!string.IsNullOrWhiteSpace(dto.Sifre) && dto.Sifre != dto.SifreTekrar)
            return "Şifreler eşleşmiyor.";

        return null;
    }

    private async Task<ServiceResult<string>> CreateIdentityUserAsync(string email, string password)
    {
        var existing = await _userManager.FindByEmailAsync(email);
        if (existing != null)
            return ServiceResult<string>.Fail("Bu e-posta ile zaten bir kullanıcı hesabı var.");

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            IsApproved = true
        };

        var createResult = await _userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
            return ServiceResult<string>.Fail(FormatIdentityErrors(createResult));

        var roleResult = await _userManager.AddToRoleAsync(user, AppRoles.Personel);
        if (!roleResult.Succeeded)
            return ServiceResult<string>.Fail(FormatIdentityErrors(roleResult));

        return ServiceResult<string>.Success(user.Id);
    }

    private async Task<ServiceResult> SyncIdentityUserAsync(Personel entity, PersonelUpdateDto dto)
    {
        if (string.IsNullOrWhiteSpace(entity.IdentityUserId))
        {
            var createResult = await CreateIdentityUserAsync(dto.Email, dto.Sifre!);
            if (!createResult.Succeeded)
                return ServiceResult.Fail(createResult.Error!);

            entity.IdentityUserId = createResult.Data;
            return ServiceResult.Success();
        }

        var user = await _userManager.FindByIdAsync(entity.IdentityUserId);
        if (user == null)
        {
            if (string.IsNullOrWhiteSpace(dto.Sifre))
                return ServiceResult.Fail("Bağlı kullanıcı bulunamadı; yeni hesap için şifre giriniz.");

            var recreate = await CreateIdentityUserAsync(dto.Email, dto.Sifre);
            if (!recreate.Succeeded)
                return ServiceResult.Fail(recreate.Error!);

            entity.IdentityUserId = recreate.Data;
            return ServiceResult.Success();
        }

        if (!string.Equals(user.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
        {
            var emailOwner = await _userManager.FindByEmailAsync(dto.Email);
            if (emailOwner != null && emailOwner.Id != user.Id)
                return ServiceResult.Fail("Bu e-posta ile zaten bir kullanıcı hesabı var.");

            user.Email = dto.Email;
            user.UserName = dto.Email;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                return ServiceResult.Fail(FormatIdentityErrors(updateResult));
        }

        if (!string.IsNullOrWhiteSpace(dto.Sifre))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, token, dto.Sifre);
            if (!resetResult.Succeeded)
                return ServiceResult.Fail(FormatIdentityErrors(resetResult));
        }

        return ServiceResult.Success();
    }

    private static string FormatIdentityErrors(IdentityResult result)
        => string.Join(" ", result.Errors.Select(e => e.Description));
}
