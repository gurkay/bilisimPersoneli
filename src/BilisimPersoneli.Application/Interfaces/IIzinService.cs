using BilisimPersoneli.Application.Common;
using BilisimPersoneli.Application.DTOs;
using BilisimPersoneli.Domain.Enums;

namespace BilisimPersoneli.Application.Interfaces;

public interface IIzinService
{
    Task<IReadOnlyList<IzinDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<IzinDto>> GetPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        DateTime? baslangic = null,
        DateTime? bitis = null,
        IzinTuru? izinTuru = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IzinDto>> GetByPersonelIdAsync(int personelId, CancellationToken cancellationToken = default);
    Task<PagedResult<IzinDto>> GetPagedByPersonelIdAsync(
        int personelId,
        int page,
        int pageSize,
        string? search = null,
        DateTime? baslangic = null,
        DateTime? bitis = null,
        IzinTuru? izinTuru = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IzinDto>> GetPendingAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<IzinDto>> GetPendingPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IzinDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<IzinDto>> CreateAsync(IzinCreateDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult<IzinDto>> UpdateAsync(IzinUpdateDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult> SoftDeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<IzinDto>> ApproveAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<IzinDto>> RejectAsync(int id, string? onayNotu, CancellationToken cancellationToken = default);
}
