using System.ComponentModel.DataAnnotations;

namespace BilisimPersoneli.Application.DTOs;

public class LoginDto
{
    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre zorunludur.")]
    [DataType(DataType.Password)]
    [Display(Name = "Şifre")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Beni hatırla")]
    public bool RememberMe { get; set; }
}

public class TokenResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string Email { get; set; } = string.Empty;
    public IList<string> Roles { get; set; } = new List<string>();
}

public class ChangePasswordDto
{
    [Required(ErrorMessage = "Mevcut şifre zorunludur.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mevcut Şifre")]
    public string MevcutSifre { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yeni şifre zorunludur.")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
    [Display(Name = "Yeni Şifre")]
    public string YeniSifre { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yeni şifre tekrarı zorunludur.")]
    [DataType(DataType.Password)]
    [Display(Name = "Yeni Şifre (Tekrar)")]
    [Compare(nameof(YeniSifre), ErrorMessage = "Şifreler eşleşmiyor.")]
    public string YeniSifreTekrar { get; set; } = string.Empty;
}
