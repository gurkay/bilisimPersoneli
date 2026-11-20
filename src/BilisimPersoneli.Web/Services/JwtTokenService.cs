using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BilisimPersoneli.Application.DTOs;
using BilisimPersoneli.Infrastructure.Identity;
using Microsoft.IdentityModel.Tokens;

namespace BilisimPersoneli.Web.Services;

public class JwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public TokenResponseDto CreateToken(ApplicationUser user, IList<string> roles)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expireMinutes = int.Parse(jwtSection["ExpireMinutes"] ?? "60");
        var expires = DateTime.UtcNow.AddMinutes(expireMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(
            issuer: jwtSection["Issuer"],
            audience: jwtSection["Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return new TokenResponseDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAt = expires,
            Email = user.Email ?? string.Empty,
            Roles = roles
        };
    }
}
