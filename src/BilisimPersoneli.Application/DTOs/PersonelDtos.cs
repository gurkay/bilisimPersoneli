using System.ComponentModel.DataAnnotations;

namespace BilisimPersoneli.Application.DTOs;

public class PersonelDto
{
    public int Id { get; set; }
    public string Ad { get; set; } = string.Empty;
    public string Soyad { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefon { get; set; }
    public decimal Maas { get; set; }
    public DateTime IseGirisTarihi { get; set; }
    public int DepartmanId { get; set; }
    public string? DepartmanAd { get; set; }
    public string? IdentityUserId { get; set; }
}

public class PersonelCreateDto
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

    [Required(ErrorMessage = "Maaş zorunludur.")]
    [Range(0, double.MaxValue, ErrorMessage = "Maaş negatif olamaz.")]
    [Display(Name = "Maaş")]
    public decimal Maas { get; set; }

    [Required(ErrorMessage = "İşe giriş tarihi zorunludur.")]
    [DataType(DataType.Date)]
    [Display(Name = "İşe Giriş Tarihi")]
    public DateTime IseGirisTarihi { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Departman seçiniz.")]
    [Display(Name = "Departman")]
    public int DepartmanId { get; set; }

    public string? IdentityUserId { get; set; }

    /// <summary>Oluşturmada zorunlu; güncellemede boş bırakılırsa şifre değişmez.</summary>
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
    [Display(Name = "Şifre")]
    public string? Sifre { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Şifre (Tekrar)")]
    [Compare(nameof(Sifre), ErrorMessage = "Şifreler eşleşmiyor.")]
    public string? SifreTekrar { get; set; }
}

public class PersonelUpdateDto : PersonelCreateDto
{
    public int Id { get; set; }
}
