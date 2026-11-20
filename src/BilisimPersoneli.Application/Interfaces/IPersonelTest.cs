


using BilisimPersoneli.Application.Common;
using BilisimPersoneli.Application.DTOs;

namespace BilisimPersoneli.Application.Interfaces;

public interface IPersonelTest
{
    Task<ServiceResult<PersonelTestDto>> CreateAsync(CreatePersonelTestDto dto, CancellationToken cancellationToken = default);
    Task<PagedResult<PersonelTestDto>> GetPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        int? departmanId = null,
        CancellationToken cancellationToken = default);
    Task<ServiceResult<PersonelTestDto>> UpdateAsync(UpdatePersonelTestDto dto, CancellationToken cancellationToken = default);
    Task<ServiceResult> SoftDeleteAsync(int id, CancellationToken cancellationToken = default);  
}