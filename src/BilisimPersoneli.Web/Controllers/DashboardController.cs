using BilisimPersoneli.Application.Common;
using BilisimPersoneli.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BilisimPersoneli.Web.Controllers;

[Authorize(Roles = AppRoles.Staff)]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    public async Task<IActionResult> Index(
        int maasPage = 1,
        int maasPageSize = PaginationDefaults.DefaultPageSize,
        int izinliPage = 1,
        int izinliPageSize = PaginationDefaults.DefaultPageSize,
        int ortalamaPage = 1,
        int ortalamaPageSize = PaginationDefaults.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var model = await _dashboardService.GetDashboardAsync(
            maasPage,
            maasPageSize,
            izinliPage,
            izinliPageSize,
            ortalamaPage,
            ortalamaPageSize,
            cancellationToken);
        return View(model);
    }
}
