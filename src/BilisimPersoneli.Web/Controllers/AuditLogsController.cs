using BilisimPersoneli.Application.Common;
using BilisimPersoneli.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BilisimPersoneli.Web.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class AuditLogsController : Controller
{
    private readonly IAuditLogService _service;

    public AuditLogsController(IAuditLogService service)
    {
        _service = service;
    }

    public async Task<IActionResult> Index(
        string? entityName,
        string? userName,
        DateTime? from,
        DateTime? to,
        int page = 1,
        int pageSize = PaginationDefaults.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        ViewBag.EntityName = entityName;
        ViewBag.UserName = userName;
        ViewBag.From = from?.ToString("yyyy-MM-dd");
        ViewBag.To = to?.ToString("yyyy-MM-dd");
        var list = await _service.GetPagedAsync(page, pageSize, entityName, userName, from, to, cancellationToken);
        return View(list);
    }
}
