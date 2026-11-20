using System.Security.Claims;
using BilisimPersoneli.Application.Common;
using BilisimPersoneli.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace BilisimPersoneli.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string? UserId => User?.FindFirstValue(ClaimTypes.NameIdentifier);
    public string? UserName => User?.Identity?.Name;
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(string role) => User?.IsInRole(role) ?? false;

    public bool IsStaff =>
        IsInRole(AppRoles.Admin) || IsInRole(AppRoles.Yonetici) || IsInRole(AppRoles.Isletmen);
}
