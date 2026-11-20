using AutoMapper;
using BilisimPersoneli.Application.Common;
using BilisimPersoneli.Application.DTOs;
using BilisimPersoneli.Application.Interfaces;
using BilisimPersoneli.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BilisimPersoneli.Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;

    public AuditLogService(AppDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<AuditLogDto>> GetAllAsync(
        string? entityName = null,
        string? userName = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var list = await BuildQuery(entityName, userName, from, to)
            .OrderByDescending(x => x.Timestamp)
            .Take(500)
            .ToListAsync(cancellationToken);
        return _mapper.Map<List<AuditLogDto>>(list);
    }

    public async Task<PagedResult<AuditLogDto>> GetPagedAsync(
        int page,
        int pageSize,
        string? entityName = null,
        string? userName = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var (normalizedPage, normalizedSize) = Pagination.Normalize(page, pageSize);
        var query = BuildQuery(entityName, userName, from, to).OrderByDescending(x => x.Timestamp);
        var total = await query.CountAsync(cancellationToken);
        var list = await query
            .Skip((normalizedPage - 1) * normalizedSize)
            .Take(normalizedSize)
            .ToListAsync(cancellationToken);
        return PagedResult<AuditLogDto>.Create(_mapper.Map<List<AuditLogDto>>(list), normalizedPage, normalizedSize, total);
    }

    private IQueryable<Domain.Entities.AuditLog> BuildQuery(
        string? entityName,
        string? userName,
        DateTime? from,
        DateTime? to)
    {
        var query = _db.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityName))
            query = query.Where(x => x.EntityName.Contains(entityName));

        if (!string.IsNullOrWhiteSpace(userName))
            query = query.Where(x => x.UserName != null && x.UserName.Contains(userName));

        if (from.HasValue)
            query = query.Where(x => x.Timestamp >= from.Value);

        if (to.HasValue)
            query = query.Where(x => x.Timestamp <= to.Value);

        return query;
    }
}
