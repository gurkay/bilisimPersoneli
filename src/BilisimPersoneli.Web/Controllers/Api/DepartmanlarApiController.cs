using BilisimPersoneli.Application.Common;
using BilisimPersoneli.Application.DTOs;
using BilisimPersoneli.Application.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BilisimPersoneli.Web.Controllers.Api;

[ApiController]
[Route("api/departmanlar")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = AppRoles.AdminAndIsletmen)]
public class DepartmanlarApiController : ControllerBase
{
    private readonly IDepartmanService _service;

    public DepartmanlarApiController(IDepartmanService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DepartmanDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _service.GetAllAsync(cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DepartmanDto>> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await _service.GetByIdAsync(id, cancellationToken);
        return item == null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<DepartmanDto>> Create([FromBody] DepartmanCreateDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var result = await _service.CreateAsync(dto, cancellationToken);
        return result.Succeeded ? Ok(result.Data) : BadRequest(new { message = result.Error });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<DepartmanDto>> Update(int id, [FromBody] DepartmanUpdateDto dto, CancellationToken cancellationToken)
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
