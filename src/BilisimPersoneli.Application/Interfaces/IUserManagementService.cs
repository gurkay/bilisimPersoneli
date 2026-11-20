using BilisimPersoneli.Application.Common;
using BilisimPersoneli.Application.DTOs;

namespace BilisimPersoneli.Application.Interfaces;

public interface IUserManagementService
{
    Task<ServiceResult> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserListItemDto>> GetUsersAsync(
        bool? approved = null,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<UserListItemDto?> GetByIdAsync(string userId, CancellationToken cancellationToken = default);

    Task<ServiceResult> ApproveAsync(string userId, CancellationToken cancellationToken = default);

    Task<ServiceResult> RejectAsync(string userId, CancellationToken cancellationToken = default);

    Task<ServiceResult> UpdateRolesAsync(UserRolesUpdateDto dto, CancellationToken cancellationToken = default);
}
