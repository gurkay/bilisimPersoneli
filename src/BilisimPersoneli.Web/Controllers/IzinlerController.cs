using BilisimPersoneli.Application.Common;
using BilisimPersoneli.Application.DTOs;
using BilisimPersoneli.Application.Interfaces;
using BilisimPersoneli.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BilisimPersoneli.Web.Controllers;

[Authorize]
public class IzinlerController : Controller
{
    private readonly IIzinService _service;
    private readonly IPersonelService _personelService;
    private readonly ICurrentUserService _currentUser;

    public IzinlerController(IIzinService service, IPersonelService personelService, ICurrentUserService currentUser)
    {
        _service = service;
        _personelService = personelService;
        _currentUser = currentUser;
    }

    public async Task<IActionResult> Index(
        string? search = null,
        DateTime? baslangic = null,
        DateTime? bitis = null,
        IzinTuru? izinTuru = null,
        int page = 1,
        int pageSize = PaginationDefaults.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        ViewBag.Search = search;
        ViewBag.Baslangic = baslangic?.ToString("yyyy-MM-dd");
        ViewBag.Bitis = bitis?.ToString("yyyy-MM-dd");
        ViewBag.IzinTuru = izinTuru;
        ViewBag.IzinTurleri = new SelectList(
            Enum.GetValues<IzinTuru>().Select(t => new { Id = (int)t, Ad = t.ToString() }),
            "Id",
            "Ad",
            izinTuru.HasValue ? (int?)izinTuru.Value : null);
        ViewBag.ShowPersonelSearch = _currentUser.IsStaff;

        if (!_currentUser.IsStaff)
        {
            var me = await _personelService.GetByIdentityUserIdAsync(_currentUser.UserId!, cancellationToken);
            if (me == null)
            {
                TempData["Error"] = "Personel kaydınız bulunamadı.";
                return View(PagedResult<IzinDto>.Create(Array.Empty<IzinDto>(), page, pageSize, 0));
            }

            return View(await _service.GetPagedByPersonelIdAsync(
                me.Id, page, pageSize, search, baslangic, bitis, izinTuru, cancellationToken));
        }

        return View(await _service.GetPagedAsync(
            page, pageSize, search, baslangic, bitis, izinTuru, cancellationToken));
    }

    [Authorize(Roles = AppRoles.AdminAndYonetici)]
    public async Task<IActionResult> Onaylar(int page = 1, int pageSize = PaginationDefaults.DefaultPageSize, CancellationToken cancellationToken = default)
    {
        return View(await _service.GetPendingPagedAsync(page, pageSize, cancellationToken));
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var item = await _service.GetByIdAsync(id, cancellationToken);
        if (item == null) return NotFound();
        if (!await CanAccessAsync(item, cancellationToken)) return Forbid();
        return View(item);
    }

    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        await LoadLookupsAsync(cancellationToken);
        var model = new IzinCreateDto();

        if (!_currentUser.IsStaff)
        {
            var me = await _personelService.GetByIdentityUserIdAsync(_currentUser.UserId!, cancellationToken);
            if (me != null) model.PersonelId = me.Id;
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(IzinCreateDto model, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsStaff)
        {
            var me = await _personelService.GetByIdentityUserIdAsync(_currentUser.UserId!, cancellationToken);
            if (me == null)
            {
                ModelState.AddModelError(string.Empty, "Personel kaydınız bulunamadı.");
                await LoadLookupsAsync(cancellationToken);
                return View(model);
            }
            model.PersonelId = me.Id;
        }

        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync(cancellationToken);
            return View(model);
        }

        var result = await _service.CreateAsync(model, cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            await LoadLookupsAsync(cancellationToken);
            return View(model);
        }

        TempData["Success"] = "İzin talebi oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var item = await _service.GetByIdAsync(id, cancellationToken);
        if (item == null) return NotFound();
        if (!await CanAccessAsync(item, cancellationToken)) return Forbid();
        if (item.Durum != IzinDurumu.Beklemede)
        {
            TempData["Error"] = "Onaylanmış veya reddedilmiş izin düzenlenemez.";
            return RedirectToAction(nameof(Details), new { id });
        }

        await LoadLookupsAsync(cancellationToken);
        return View(new IzinUpdateDto
        {
            Id = item.Id,
            PersonelId = item.PersonelId,
            BaslangicTarihi = item.BaslangicTarihi,
            BitisTarihi = item.BitisTarihi,
            IzinTuru = item.IzinTuru,
            Aciklama = item.Aciklama
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, IzinUpdateDto model, CancellationToken cancellationToken)
    {
        if (id != model.Id) return BadRequest();

        if (!_currentUser.IsStaff)
        {
            var me = await _personelService.GetByIdentityUserIdAsync(_currentUser.UserId!, cancellationToken);
            if (me == null) return Forbid();
            model.PersonelId = me.Id;
        }

        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync(cancellationToken);
            return View(model);
        }

        var result = await _service.UpdateAsync(model, cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            await LoadLookupsAsync(cancellationToken);
            return View(model);
        }

        TempData["Success"] = "İzin güncellendi.";
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
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "İzin silindi." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.AdminAndYonetici)]
    public async Task<IActionResult> Onayla(int id, CancellationToken cancellationToken)
    {
        var result = await _service.ApproveAsync(id, cancellationToken);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "İzin onaylandı." : result.Error;
        return RedirectToAction(nameof(Onaylar));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.AdminAndYonetici)]
    public async Task<IActionResult> Reddet(int id, string? onayNotu, CancellationToken cancellationToken)
    {
        var result = await _service.RejectAsync(id, onayNotu, cancellationToken);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "İzin reddedildi." : result.Error;
        return RedirectToAction(nameof(Onaylar));
    }

    private async Task LoadLookupsAsync(CancellationToken cancellationToken)
    {
        var personeller = await _personelService.GetAllAsync(cancellationToken);
        ViewBag.Personeller = new SelectList(
            personeller.Select(p => new { p.Id, Ad = $"{p.Ad} {p.Soyad}" }),
            "Id", "Ad");
        ViewBag.IzinTurleri = new SelectList(Enum.GetValues<IzinTuru>());
    }

    private async Task<bool> CanAccessAsync(IzinDto item, CancellationToken cancellationToken)
    {
        if (_currentUser.IsStaff)
            return true;

        var me = await _personelService.GetByIdentityUserIdAsync(_currentUser.UserId!, cancellationToken);
        return me != null && me.Id == item.PersonelId;
    }
}
