using BilisimPersoneli.Application.Common;
using BilisimPersoneli.Application.DTOs;
using BilisimPersoneli.Application.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BilisimPersoneli.Web.Controllers.Api;

[ApiController]
[Route("api/izinler")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class IzinlerApiController : ControllerBase
{
    private readonly IIzinService _service;
    private readonly IPersonelService _personelService;
    private readonly ICurrentUserService _currentUser;

    public IzinlerApiController(IIzinService service, IPersonelService personelService, ICurrentUserService currentUser)
    {
        _service = service;
        _personelService = personelService;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<IzinDto>>> GetAll(CancellationToken cancellationToken)
    {
        if (_currentUser.IsStaff)
            return Ok(await _service.GetAllAsync(cancellationToken));

        var me = await _personelService.GetByIdentityUserIdAsync(_currentUser.UserId!, cancellationToken);
        if (me == null) return Ok(Array.Empty<IzinDto>());
        return Ok(await _service.GetByPersonelIdAsync(me.Id, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<IzinDto>> Create([FromBody] IzinCreateDto dto, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsStaff)
        {
            var me = await _personelService.GetByIdentityUserIdAsync(_currentUser.UserId!, cancellationToken);
            if (me == null) return BadRequest(new { message = "Personel kaydı bulunamadı." });
            dto.PersonelId = me.Id;
        }

        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await _service.CreateAsync(dto, cancellationToken);
        return result.Succeeded ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    [HttpPost("{id:int}/onayla")]
    [Authorize(Roles = AppRoles.AdminAndYonetici)]
    public async Task<ActionResult<IzinDto>> Onayla(int id, CancellationToken cancellationToken)
    {
        var result = await _service.ApproveAsync(id, cancellationToken);
        return result.Succeeded ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    [HttpPost("{id:int}/reddet")]
    [Authorize(Roles = AppRoles.AdminAndYonetici)]
    public async Task<ActionResult<IzinDto>> Reddet(int id, [FromBody] IzinReddetDto? dto, CancellationToken cancellationToken)
    {
        var result = await _service.RejectAsync(id, dto?.OnayNotu, cancellationToken);
        return result.Succeeded ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }
}
