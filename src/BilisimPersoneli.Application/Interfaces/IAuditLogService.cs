using BilisimPersoneli.Application.Common;
using BilisimPersoneli.Application.DTOs;

namespace BilisimPersoneli.Application.Interfaces;

public interface IAuditLogService
{
    Task<IReadOnlyList<AuditLogDto>> GetAllAsync(
        string? entityName = null,
        string? userName = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default);

    Task<PagedResult<AuditLogDto>> GetPagedAsync(
        int page,
        int pageSize,
        string? entityName = null,
        string? userName = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default);
}
