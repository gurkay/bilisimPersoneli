using BilisimPersoneli.Application.DTOs;
using BilisimPersoneli.Application.Interfaces;
using BilisimPersoneli.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BilisimPersoneli.Web.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserManagementService _userManagement;
    private readonly IDepartmanService _departmanService;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IUserManagementService userManagement,
        IDepartmanService departmanService)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _userManagement = userManagement;
        _departmanService = departmanService;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginDto());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginDto model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Geçersiz e-posta veya şifre.");
            return View(model);
        }

        if (!user.IsApproved)
        {
            ModelState.AddModelError(string.Empty, "Hesabınız henüz onaylanmadı. Yönetici veya admin onayını bekleyiniz.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Geçersiz e-posta veya şifre.");
            return View(model);
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Register(CancellationToken cancellationToken)
    {
        await LoadDepartmanlarAsync(cancellationToken);
        return View(new RegisterDto());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterDto model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadDepartmanlarAsync(cancellationToken);
            return View(model);
        }

        var result = await _userManagement.RegisterAsync(model, cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            await LoadDepartmanlarAsync(cancellationToken);
            return View(model);
        }

        TempData["Success"] = "Kaydınız alındı. Yönetici veya admin onayından sonra giriş yapabilirsiniz.";
        return RedirectToAction(nameof(Login));
    }

    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword() => View(new ChangePasswordDto());

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        var result = await _userManager.ChangePasswordAsync(user, model.MevcutSifre, model.YeniSifre);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        await _signInManager.RefreshSignInAsync(user);
        TempData["Success"] = "Şifreniz güncellendi.";
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    private async Task LoadDepartmanlarAsync(CancellationToken cancellationToken)
    {
        var departmanlar = await _departmanService.GetAllAsync(cancellationToken);
        ViewBag.Departmanlar = new SelectList(departmanlar, "Id", "Ad");
    }
}
