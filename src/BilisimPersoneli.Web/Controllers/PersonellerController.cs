using BilisimPersoneli.Application.Common;
using BilisimPersoneli.Application.DTOs;
using BilisimPersoneli.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BilisimPersoneli.Web.Controllers;

[Authorize(Roles = AppRoles.Staff)]
public class PersonellerController : Controller
{
    private readonly IPersonelService _service;
    private readonly IDepartmanService _departmanService;

    public PersonellerController(IPersonelService service, IDepartmanService departmanService)
    {
        _service = service;
        _departmanService = departmanService;
    }

    public async Task<IActionResult> Index(
        string? search = null,
        int? departmanId = null,
        int page = 1,
        int pageSize = PaginationDefaults.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        ViewBag.Search = search;
        ViewBag.DepartmanId = departmanId;
        await LoadDepartmanlarAsync(cancellationToken, departmanId);
        return View(await _service.GetPagedAsync(page, pageSize, search, departmanId, cancellationToken));
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var item = await _service.GetByIdAsync(id, cancellationToken);
        if (item == null) return NotFound();
        return View(item);
    }

    [HttpGet]
    public async Task<IActionResult> DetailsPartial(int id, CancellationToken cancellationToken)
    {
        var item = await _service.GetByIdAsync(id, cancellationToken);
        if (item == null) return NotFound();
        return PartialView("_DetailsPartial", item);
    }

    [Authorize(Roles = AppRoles.Staff)]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        await LoadDepartmanlarAsync(cancellationToken);
        ViewData["PasswordOptional"] = false;
        return View(new PersonelCreateDto());
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.Staff)]
    public async Task<IActionResult> CreatePartial(CancellationToken cancellationToken)
    {
        await LoadDepartmanlarAsync(cancellationToken);
        ViewData["PasswordOptional"] = false;
        return PartialView("_FormPartial", new PersonelCreateDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Staff)]
    public async Task<IActionResult> Create(PersonelCreateDto model, CancellationToken cancellationToken)
    {
        ViewData["PasswordOptional"] = false;
        if (string.IsNullOrWhiteSpace(model.Sifre))
            ModelState.AddModelError(nameof(model.Sifre), "Şifre zorunludur.");

        if (!ModelState.IsValid)
        {
            if (IsAjaxRequest())
                return AjaxValidationError();

            await LoadDepartmanlarAsync(cancellationToken);
            return View(model);
        }

        var result = await _service.CreateAsync(model, cancellationToken);
        if (!result.Succeeded)
        {
            if (IsAjaxRequest())
                return BadRequest(new { success = false, message = result.Error });

            ModelState.AddModelError(string.Empty, result.Error!);
            await LoadDepartmanlarAsync(cancellationToken);
            return View(model);
        }

        if (IsAjaxRequest())
            return Json(new { success = true, message = "Personel oluşturuldu; giriş hesabı tanımlandı." });

        TempData["Success"] = "Personel oluşturuldu; giriş hesabı tanımlandı.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = AppRoles.Staff)]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var item = await _service.GetByIdAsync(id, cancellationToken);
        if (item == null) return NotFound();

        await LoadDepartmanlarAsync(cancellationToken);
        ViewData["PasswordOptional"] = !string.IsNullOrWhiteSpace(item.IdentityUserId);
        return View(ToUpdateDto(item));
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.Staff)]
    public async Task<IActionResult> EditPartial(int id, CancellationToken cancellationToken)
    {
        var item = await _service.GetByIdAsync(id, cancellationToken);
        if (item == null) return NotFound();

        await LoadDepartmanlarAsync(cancellationToken);
        ViewData["PasswordOptional"] = !string.IsNullOrWhiteSpace(item.IdentityUserId);
        return PartialView("_FormPartial", ToUpdateDto(item));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Staff)]
    public async Task<IActionResult> Edit(int id, PersonelUpdateDto model, CancellationToken cancellationToken)
    {
        if (id != model.Id) return BadRequest();
        ViewData["PasswordOptional"] = !string.IsNullOrWhiteSpace(model.IdentityUserId);

        if (string.IsNullOrWhiteSpace(model.IdentityUserId) && string.IsNullOrWhiteSpace(model.Sifre))
            ModelState.AddModelError(nameof(model.Sifre), "Giriş hesabı yok; şifre zorunludur.");

        if (!ModelState.IsValid)
        {
            if (IsAjaxRequest())
                return AjaxValidationError();

            await LoadDepartmanlarAsync(cancellationToken);
            return View(model);
        }

        var result = await _service.UpdateAsync(model, cancellationToken);
        if (!result.Succeeded)
        {
            if (IsAjaxRequest())
                return BadRequest(new { success = false, message = result.Error });

            ModelState.AddModelError(string.Empty, result.Error!);
            await LoadDepartmanlarAsync(cancellationToken);
            return View(model);
        }

        if (IsAjaxRequest())
            return Json(new { success = true, message = "Personel güncellendi." });

        TempData["Success"] = "Personel güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var item = await _service.GetByIdAsync(id, cancellationToken);
        if (item == null) return NotFound();
        return View(item);
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> DeletePartial(int id, CancellationToken cancellationToken)
    {
        var item = await _service.GetByIdAsync(id, cancellationToken);
        if (item == null) return NotFound();
        return PartialView("_DeletePartial", item);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var result = await _service.SoftDeleteAsync(id, cancellationToken);

        if (IsAjaxRequest())
        {
            if (!result.Succeeded)
                return BadRequest(new { success = false, message = result.Error });

            return Json(new { success = true, message = "Personel silindi." });
        }

        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "Personel silindi." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadDepartmanlarAsync(CancellationToken cancellationToken, int? selectedDepartmanId = null)
    {
        var departmanlar = await _departmanService.GetAllAsync(cancellationToken);
        ViewBag.Departmanlar = new SelectList(departmanlar, "Id", "Ad", selectedDepartmanId);
    }

    private static PersonelUpdateDto ToUpdateDto(PersonelDto item) => new()
    {
        Id = item.Id,
        Ad = item.Ad,
        Soyad = item.Soyad,
        Email = item.Email,
        Telefon = item.Telefon,
        Maas = item.Maas,
        IseGirisTarihi = item.IseGirisTarihi,
        DepartmanId = item.DepartmanId,
        IdentityUserId = item.IdentityUserId
    };

    private bool IsAjaxRequest() =>
        string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);

    private IActionResult AjaxValidationError()
    {
        var errors = ModelState
            .Where(x => x.Value?.ValidationState == ModelValidationState.Invalid)
            .ToDictionary(
                k => k.Key,
                v => v.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

        var message = string.Join(" ", errors.SelectMany(x => x.Value).Where(m => !string.IsNullOrWhiteSpace(m)));
        return BadRequest(new { success = false, message, errors });
    }
}
