using BilisimPersoneli.Application.Common;
using BilisimPersoneli.Application.DTOs;

namespace BilisimPersoneli.Application.Interfaces;

public interface IPersonelService
{
    Task<IReadOnlyList<PersonelDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<PersonelDto>> GetPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        int? departmanId = null,
        CancellationToken cancellationToken = default);
    Task<PersonelDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PersonelDto?> GetByIdentityUserIdAsync(string identityUserId, CancellationToken cancellationToken = default);
    Task<ServiceResult<PersonelDto>> CreateAsync(PersonelCreateDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult<PersonelDto>> UpdateAsync(PersonelUpdateDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult> SoftDeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> RestoreAsync(int id, CancellationToken cancellationToken = default);
}
