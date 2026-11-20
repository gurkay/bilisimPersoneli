using BilisimPersoneli.Application.Common;
using BilisimPersoneli.Application.DTOs;
using BilisimPersoneli.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BilisimPersoneli.Web.Controllers;

[Authorize(Roles = AppRoles.AdminAndYonetici)]
public class UsersController : Controller
{
    private readonly IUserManagementService _users;

    public UsersController(IUserManagementService users) => _users = users;

    [Authorize(Roles = AppRoles.AdminAndYonetici)]
    public async Task<IActionResult> Index(string? search = null, bool? approved = null, CancellationToken cancellationToken = default)
    {
        ViewBag.Search = search;
        ViewBag.Approved = approved;
        var list = await _users.GetUsersAsync(approved, search, cancellationToken);
        return View(list);
    }

    [Authorize(Roles = AppRoles.AdminAndYonetici)]
    public async Task<IActionResult> Pending(CancellationToken cancellationToken = default)
    {
        var list = await _users.GetUsersAsync(approved: false, cancellationToken: cancellationToken);
        return View(list);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.AdminAndYonetici)]
    public async Task<IActionResult> Approve(string id, string? returnAction = null, CancellationToken cancellationToken = default)
    {
        var result = await _users.ApproveAsync(id, cancellationToken);
        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Kullanıcı kaydı onaylandı." : result.Error;

        return RedirectToReturn(returnAction);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.AdminAndYonetici)]
    public async Task<IActionResult> Reject(string id, string? returnAction = null, CancellationToken cancellationToken = default)
    {
        var result = await _users.RejectAsync(id, cancellationToken);
        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded ? "Kayıt reddedildi ve silindi." : result.Error;

        return RedirectToReturn(returnAction);
    }

    [Authorize(Roles = AppRoles.AdminAndYonetici)]
    public async Task<IActionResult> Roles(string id, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(id, cancellationToken);
        if (user == null) return NotFound();
        if (!user.IsApproved)
        {
            TempData["Error"] = "Rol atamadan önce kaydı onaylayın.";
            return RedirectToAction(nameof(Index));
        }

        if (!CanActorEditTarget(user))
        {
            TempData["Error"] = "Bu kullanıcının rolünü değiştirme yetkiniz yok.";
            return RedirectToAction(nameof(Index));
        }

        SetAssignableRoles();
        return View(new UserRolesUpdateDto
        {
            UserId = user.Id,
            SelectedRoles = new List<string> { AppRoles.PickPrimaryRole(user.Roles) }
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.AdminAndYonetici)]
    public async Task<IActionResult> Roles(UserRolesUpdateDto model, CancellationToken cancellationToken = default)
    {
        SetAssignableRoles();

        if (model.SelectedRoles == null || model.SelectedRoles.Count != 1)
            ModelState.AddModelError(nameof(model.SelectedRoles), "Tek bir rol seçilmelidir.");

        if (!ModelState.IsValid)
            return View(model);

        var result = await _users.UpdateRolesAsync(model, cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(model);
        }

        TempData["Success"] = "Rol güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    private void SetAssignableRoles()
    {
        ViewBag.AllRoles = AppRoles.AssignableRolesFor(User.IsInRole(AppRoles.Admin));
    }

    private bool CanActorEditTarget(UserListItemDto user)
    {
        if (User.IsInRole(AppRoles.Admin))
            return true;

        return !user.Roles.Contains(AppRoles.Admin, StringComparer.OrdinalIgnoreCase);
    }

    private IActionResult RedirectToReturn(string? returnAction)
    {
        if (string.Equals(returnAction, nameof(Pending), StringComparison.OrdinalIgnoreCase))
            return RedirectToAction(nameof(Pending));
        return RedirectToAction(nameof(Index));
    }
}
