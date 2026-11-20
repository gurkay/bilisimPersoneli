using BilisimPersoneli.Application.Common;
using BilisimPersoneli.Application.DTOs;
using BilisimPersoneli.Application.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BilisimPersoneli.Web.Controllers.Api;

[ApiController]
[Route("api/personeller")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class PersonellerApiController : ControllerBase
{
    private readonly IPersonelService _service;

    public PersonellerApiController(IPersonelService service) => _service = service;

    [HttpGet]
    [Authorize(Roles = AppRoles.Staff)]
    public async Task<ActionResult<IReadOnlyList<PersonelDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _service.GetAllAsync(cancellationToken));

    [HttpGet("{id:int}")]
    [Authorize(Roles = AppRoles.Staff)]
    public async Task<ActionResult<PersonelDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _service.GetByIdAsync(id, cancellationToken);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpPost]
    [Authorize(Roles = AppRoles.Staff)]
    public async Task<ActionResult<PersonelDto>> Create([FromBody] PersonelCreateDto dto, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(dto.Sifre))
            ModelState.AddModelError(nameof(dto.Sifre), "Şifre zorunludur.");
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await _service.CreateAsync(dto, cancellationToken);
        return result.Succeeded ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Staff)]
    public async Task<ActionResult<PersonelDto>> Update(int id, [FromBody] PersonelUpdateDto dto, CancellationToken cancellationToken)
    {
        if (id != dto.Id) return BadRequest();
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await _service.UpdateAsync(dto, cancellationToken);
        return result.Succeeded ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _service.SoftDeleteAsync(id, cancellationToken);
        return result.Succeeded ? NoContent() : BadRequest(new { message = result.Error });
    }
}
