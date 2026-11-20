using AutoMapper;
using BilisimPersoneli.Application.Common;
using BilisimPersoneli.Application.DTOs;
using BilisimPersoneli.Application.Interfaces;
using BilisimPersoneli.Domain.Entities;
using BilisimPersoneli.Domain.Enums;
using BilisimPersoneli.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BilisimPersoneli.Infrastructure.Services;

public class IzinService : IIzinService
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;

    public IzinService(AppDbContext db, IMapper mapper, ICurrentUserService currentUser)
    {
        _db = db;
        _mapper = mapper;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<IzinDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await Query().OrderByDescending(x => x.BaslangicTarihi).ToListAsync(cancellationToken);
        return _mapper.Map<List<IzinDto>>(list);
    }

    public async Task<PagedResult<IzinDto>> GetPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        DateTime? baslangic = null,
        DateTime? bitis = null,
        IzinTuru? izinTuru = null,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFilters(Query(), search, baslangic, bitis, izinTuru)
            .OrderByDescending(x => x.BaslangicTarihi);
        return await ToPagedAsync(query, page, pageSize, cancellationToken);
    }

    public async Task<IReadOnlyList<IzinDto>> GetByPersonelIdAsync(int personelId, CancellationToken cancellationToken = default)
    {
        var list = await Query()
            .Where(x => x.PersonelId == personelId)
            .OrderByDescending(x => x.BaslangicTarihi)
            .ToListAsync(cancellationToken);
        return _mapper.Map<List<IzinDto>>(list);
    }

    public async Task<PagedResult<IzinDto>> GetPagedByPersonelIdAsync(
        int personelId,
        int page,
        int pageSize,
        string? search = null,
        DateTime? baslangic = null,
        DateTime? bitis = null,
        IzinTuru? izinTuru = null,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFilters(Query().Where(x => x.PersonelId == personelId), search, baslangic, bitis, izinTuru)
            .OrderByDescending(x => x.BaslangicTarihi);
        return await ToPagedAsync(query, page, pageSize, cancellationToken);
    }

    private static IQueryable<Izin> ApplyFilters(
        IQueryable<Izin> query,
        string? search,
        DateTime? baslangic,
        DateTime? bitis,
        IzinTuru? izinTuru)
    {
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                x.Personel != null &&
                (x.Personel.Ad.Contains(term) || x.Personel.Soyad.Contains(term)));
        }

        if (baslangic.HasValue)
            query = query.Where(x => x.BaslangicTarihi >= baslangic.Value.Date);

        if (bitis.HasValue)
            query = query.Where(x => x.BitisTarihi <= bitis.Value.Date);

        if (izinTuru.HasValue)
            query = query.Where(x => x.IzinTuru == izinTuru.Value);

        return query;
    }

    public async Task<IReadOnlyList<IzinDto>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        var list = await Query()
            .Where(x => x.Durum == IzinDurumu.Beklemede)
            .OrderBy(x => x.BaslangicTarihi)
            .ToListAsync(cancellationToken);
        return _mapper.Map<List<IzinDto>>(list);
    }

    public async Task<PagedResult<IzinDto>> GetPendingPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = Query()
            .Where(x => x.Durum == IzinDurumu.Beklemede)
            .OrderBy(x => x.BaslangicTarihi);
        return await ToPagedAsync(query, page, pageSize, cancellationToken);
    }

    private async Task<PagedResult<IzinDto>> ToPagedAsync(
        IQueryable<Izin> query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var (normalizedPage, normalizedSize) = Pagination.Normalize(page, pageSize);
        var total = await query.CountAsync(cancellationToken);
        var list = await query
            .Skip((normalizedPage - 1) * normalizedSize)
            .Take(normalizedSize)
            .ToListAsync(cancellationToken);
        return PagedResult<IzinDto>.Create(_mapper.Map<List<IzinDto>>(list), normalizedPage, normalizedSize, total);
    }

    public async Task<IzinDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await Query().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity == null ? null : _mapper.Map<IzinDto>(entity);
    }

    public async Task<ServiceResult<IzinDto>> CreateAsync(IzinCreateDto dto, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateAsync(dto, cancellationToken);
        if (validation != null)
            return ServiceResult<IzinDto>.Fail(validation);

        var entity = _mapper.Map<Izin>(dto);
        entity.Durum = IzinDurumu.Beklemede;
        _db.Izinler.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        await _db.Entry(entity).Reference(x => x.Personel).LoadAsync(cancellationToken);
        return ServiceResult<IzinDto>.Success(_mapper.Map<IzinDto>(entity));
    }

    public async Task<ServiceResult<IzinDto>> UpdateAsync(IzinUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Izinler.FirstOrDefaultAsync(x => x.Id == dto.Id, cancellationToken);
        if (entity == null)
            return ServiceResult<IzinDto>.Fail("İzin bulunamadı.");

        if (entity.Durum != IzinDurumu.Beklemede)
            return ServiceResult<IzinDto>.Fail("Onaylanmış veya reddedilmiş izin düzenlenemez.");

        var validation = await ValidateAsync(dto, cancellationToken);
        if (validation != null)
            return ServiceResult<IzinDto>.Fail(validation);

        entity.PersonelId = dto.PersonelId;
        entity.BaslangicTarihi = dto.BaslangicTarihi;
        entity.BitisTarihi = dto.BitisTarihi;
        entity.IzinTuru = dto.IzinTuru;
        entity.Aciklama = dto.Aciklama;

        await _db.SaveChangesAsync(cancellationToken);
        await _db.Entry(entity).Reference(x => x.Personel).LoadAsync(cancellationToken);
        return ServiceResult<IzinDto>.Success(_mapper.Map<IzinDto>(entity));
    }

    public async Task<ServiceResult> SoftDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Izinler.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
            return ServiceResult.Fail("İzin bulunamadı.");

        _db.Izinler.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult<IzinDto>> ApproveAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Izinler.Include(x => x.Personel).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
            return ServiceResult<IzinDto>.Fail("İzin bulunamadı.");
        if (entity.Durum != IzinDurumu.Beklemede)
            return ServiceResult<IzinDto>.Fail("Sadece beklemedeki izinler onaylanabilir.");

        entity.Durum = IzinDurumu.Onaylandi;
        entity.OnaylayanUserId = _currentUser.UserId;
        entity.OnayTarihi = DateTime.UtcNow;
        entity.OnayNotu = null;
        _db.AddManualAudit(nameof(Izin), entity.Id.ToString(), AuditAction.Approve, "İzin onaylandı");
        await _db.SaveChangesAsync(cancellationToken);
        return ServiceResult<IzinDto>.Success(_mapper.Map<IzinDto>(entity));
    }

    public async Task<ServiceResult<IzinDto>> RejectAsync(int id, string? onayNotu, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Izinler.Include(x => x.Personel).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
            return ServiceResult<IzinDto>.Fail("İzin bulunamadı.");
        if (entity.Durum != IzinDurumu.Beklemede)
            return ServiceResult<IzinDto>.Fail("Sadece beklemedeki izinler reddedilebilir.");

        entity.Durum = IzinDurumu.Reddedildi;
        entity.OnaylayanUserId = _currentUser.UserId;
        entity.OnayTarihi = DateTime.UtcNow;
        entity.OnayNotu = onayNotu;
        _db.AddManualAudit(nameof(Izin), entity.Id.ToString(), AuditAction.Reject, onayNotu ?? "İzin reddedildi");
        await _db.SaveChangesAsync(cancellationToken);
        return ServiceResult<IzinDto>.Success(_mapper.Map<IzinDto>(entity));
    }

    private IQueryable<Izin> Query() =>
        _db.Izinler.AsNoTracking().Include(x => x.Personel);

    private async Task<string?> ValidateAsync(IzinCreateDto dto, CancellationToken cancellationToken)
    {
        if (dto.BitisTarihi.Date < dto.BaslangicTarihi.Date)
            return "Bitiş tarihi başlangıç tarihinden önce olamaz.";

        if (!await _db.Personeller.AnyAsync(x => x.Id == dto.PersonelId, cancellationToken))
            return "Seçilen personel bulunamadı.";

        return null;
    }
}
