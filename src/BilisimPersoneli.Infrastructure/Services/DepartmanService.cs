using AutoMapper;
using BilisimPersoneli.Application.Common;
using BilisimPersoneli.Application.DTOs;
using BilisimPersoneli.Application.Interfaces;
using BilisimPersoneli.Domain.Entities;
using BilisimPersoneli.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BilisimPersoneli.Infrastructure.Services;

public class DepartmanService : IDepartmanService
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;

    public DepartmanService(AppDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<DepartmanDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _db.Departmanlar.AsNoTracking().OrderBy(x => x.Ad).ToListAsync(cancellationToken);
        return _mapper.Map<List<DepartmanDto>>(list);
    }

    public async Task<PagedResult<DepartmanDto>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (normalizedPage, normalizedSize) = Pagination.Normalize(page, pageSize);
        var query = _db.Departmanlar.AsNoTracking().OrderBy(x => x.Ad);
        var total = await query.CountAsync(cancellationToken);
        var list = await query
            .Skip((normalizedPage - 1) * normalizedSize)
            .Take(normalizedSize)
            .ToListAsync(cancellationToken);
        return PagedResult<DepartmanDto>.Create(_mapper.Map<List<DepartmanDto>>(list), normalizedPage, normalizedSize, total);
    }

    public async Task<DepartmanDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Departmanlar.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity == null ? null : _mapper.Map<DepartmanDto>(entity);
    }

    public async Task<ServiceResult<DepartmanDto>> CreateAsync(DepartmanCreateDto dto, CancellationToken cancellationToken = default)
    {
        if (await _db.Departmanlar.AnyAsync(x => x.Ad == dto.Ad, cancellationToken))
            return ServiceResult<DepartmanDto>.Fail("Bu departman adı zaten kayıtlı.");

        var entity = _mapper.Map<Departman>(dto);
        _db.Departmanlar.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return ServiceResult<DepartmanDto>.Success(_mapper.Map<DepartmanDto>(entity));
    }

    public async Task<ServiceResult<DepartmanDto>> UpdateAsync(DepartmanUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Departmanlar.FirstOrDefaultAsync(x => x.Id == dto.Id, cancellationToken);
        if (entity == null)
            return ServiceResult<DepartmanDto>.Fail("Departman bulunamadı.");

        if (await _db.Departmanlar.AnyAsync(x => x.Ad == dto.Ad && x.Id != dto.Id, cancellationToken))
            return ServiceResult<DepartmanDto>.Fail("Bu departman adı zaten kayıtlı.");

        entity.Ad = dto.Ad;
        entity.Aciklama = dto.Aciklama;
        await _db.SaveChangesAsync(cancellationToken);
        return ServiceResult<DepartmanDto>.Success(_mapper.Map<DepartmanDto>(entity));
    }

    public async Task<ServiceResult> SoftDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Departmanlar.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null) return ServiceResult.Fail("Departman bulunamadı.");

        var hasPersonel = await _db.Personeller.AnyAsync(x => x.DepartmanId == id, cancellationToken);
        if (hasPersonel) return ServiceResult.Fail("Bu departmana bağlı personel olduğu için silinemez.");

        _db.Departmanlar.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> RestoreAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.Departmanlar.IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Id == id && x.IsDeleted, cancellationToken);
        if (entity == null)
            return ServiceResult.Fail("Silinmiş departman bulunamadı.");

        entity.IsDeleted = false;
        entity.DeletedAt = null;
        await _db.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success();
    }
}
