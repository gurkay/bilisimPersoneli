using System.ComponentModel.DataAnnotations;

namespace BilisimPersoneli.Application.DTOs;

public class RegisterDto
{
    [Required(ErrorMessage = "Ad zorunludur.")]
    [StringLength(100)]
    [Display(Name = "Ad")]
    public string Ad { get; set; } = string.Empty;

    [Required(ErrorMessage = "Soyad zorunludur.")]
    [StringLength(100)]
    [Display(Name = "Soyad")]
    public string Soyad { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta giriniz.")]
    [StringLength(256)]
    [Display(Name = "E-posta")]
    public string Email { get; set; } = string.Empty;

    [StringLength(30)]
    [Display(Name = "Telefon")]
    public string? Telefon { get; set; }

    [Required(ErrorMessage = "Departman seçiniz.")]
    [Display(Name = "Departman")]
    public int DepartmanId { get; set; }

    [Required(ErrorMessage = "Şifre zorunludur.")]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
    [Display(Name = "Şifre")]
    public string Sifre { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre tekrarı zorunludur.")]
    [DataType(DataType.Password)]
    [Display(Name = "Şifre (Tekrar)")]
    [Compare(nameof(Sifre), ErrorMessage = "Şifreler eşleşmiyor.")]
    public string SifreTekrar { get; set; } = string.Empty;
}

public class UserListItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public bool IsApproved { get; set; }
    public IList<string> Roles { get; set; } = new List<string>();
    public int? PersonelId { get; set; }
    public string? PersonelAdSoyad { get; set; }
    public string? DepartmanAd { get; set; }
}

public class UserRolesUpdateDto
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>Tek birincil rol (radio). Liste tek eleman beklenir.</summary>
    [MinLength(1, ErrorMessage = "Bir rol seçilmelidir.")]
    public IList<string> SelectedRoles { get; set; } = new List<string>();
}
