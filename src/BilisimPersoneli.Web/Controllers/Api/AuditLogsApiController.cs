using BilisimPersoneli.Application.Common;
using BilisimPersoneli.Application.DTOs;
using BilisimPersoneli.Application.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BilisimPersoneli.Web.Controllers.Api;

[ApiController]
[Route("api/audit-logs")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = AppRoles.Admin)]
public class AuditLogsApiController : ControllerBase
{
    private readonly IAuditLogService _service;

    public AuditLogsApiController(IAuditLogService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AuditLogDto>>> GetAll(
        [FromQuery] string? entityName,
        [FromQuery] string? userName,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
        => Ok(await _service.GetAllAsync(entityName, userName, from, to, cancellationToken));
}
