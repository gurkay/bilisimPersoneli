using BilisimPersoneli.Application.DTOs;

namespace BilisimPersoneli.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardDto> GetDashboardAsync(
        int maasPage = 1,
        int maasPageSize = 10,
        int izinliPage = 1,
        int izinliPageSize = 10,
        int ortalamaPage = 1,
        int ortalamaPageSize = 10,
        CancellationToken cancellationToken = default);
}
