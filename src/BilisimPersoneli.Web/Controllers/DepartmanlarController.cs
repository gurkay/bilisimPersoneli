using BilisimPersoneli.Application.Common;
using BilisimPersoneli.Application.DTOs;
using BilisimPersoneli.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BilisimPersoneli.Web.Controllers;

[Authorize(Roles = AppRoles.AdminAndIsletmen)]
public class DepartmanlarController : Controller
{
    private readonly IDepartmanService _service;

    public DepartmanlarController(IDepartmanService service)
    {
        _service = service;
    }

    public async Task<IActionResult> Index(int page = 1, int pageSize = PaginationDefaults.DefaultPageSize, CancellationToken cancellationToken = default)
    {
        var result = await _service.GetPagedAsync(page, pageSize, cancellationToken);
        return View(result);
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var item = await _service.GetByIdAsync(id, cancellationToken);
        if (item == null) return NotFound();
        return View(item);
    }

    public IActionResult Create() => View(new DepartmanCreateDto());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DepartmanCreateDto model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _service.CreateAsync(model, cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        TempData["Success"] = "Departman oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var item = await _service.GetByIdAsync(id, cancellationToken);
        if (item == null) return NotFound();
        return View(new DepartmanUpdateDto { Id = item.Id, Ad = item.Ad, Aciklama = item.Aciklama });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DepartmanUpdateDto model, CancellationToken cancellationToken)
    {
        if (id != model.Id) return BadRequest();
        if (!ModelState.IsValid) return View(model);

        var result = await _service.UpdateAsync(model, cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        TempData["Success"] = "Departman güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var item = await _service.GetByIdAsync(id, cancellationToken);
        if (item == null) return NotFound();
        return View(item);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var result = await _service.SoftDeleteAsync(id, cancellationToken);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "Departman silindi." : result.Error;
        return RedirectToAction(nameof(Index));
    }
}
