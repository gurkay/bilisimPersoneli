using BilisimPersoneli.Application.Common;
using BilisimPersoneli.Application.DTOs;

namespace BilisimPersoneli.Application.Interfaces;

public interface IDepartmanService
{
    Task<IReadOnlyList<DepartmanDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<DepartmanDto>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<DepartmanDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult<DepartmanDto>> CreateAsync(DepartmanCreateDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult<DepartmanDto>> UpdateAsync(DepartmanUpdateDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult> SoftDeleteAsync(int id, CancellationToken cancellationToken = default);
    Task<ServiceResult> RestoreAsync(int id, CancellationToken cancellationToken = default);
}
