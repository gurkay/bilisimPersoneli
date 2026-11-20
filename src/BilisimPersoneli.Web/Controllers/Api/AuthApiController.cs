using BilisimPersoneli.Application.DTOs;
using BilisimPersoneli.Infrastructure.Identity;
using BilisimPersoneli.Web.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BilisimPersoneli.Web.Controllers.Api;

[ApiController]
[Route("api/auth")]
public class AuthApiController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtTokenService _jwtTokenService;

    public AuthApiController(UserManager<ApplicationUser> userManager, JwtTokenService jwtTokenService)
    {
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenResponseDto>> Login([FromBody] LoginDto model)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
            return Unauthorized(new { message = "Geçersiz e-posta veya şifre." });

        if (!user.IsApproved)
            return Unauthorized(new { message = "Hesabınız henüz onaylanmadı." });

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(_jwtTokenService.CreateToken(user, roles));
    }

    [HttpPost("change-password")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto model)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Unauthorized();

        var result = await _userManager.ChangePasswordAsync(user, model.MevcutSifre, model.YeniSifre);
        if (!result.Succeeded)
            return BadRequest(new { message = string.Join(" ", result.Errors.Select(e => e.Description)) });

        return Ok(new { message = "Şifre güncellendi." });
    }
}
