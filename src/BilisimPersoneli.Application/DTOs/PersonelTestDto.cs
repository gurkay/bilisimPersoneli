using System.ComponentModel.DataAnnotations;

namespace BilisimPersoneli.Application.DTOs;

public class PersonelTestDto
{
    public int Id { get; set; }
    public string? AdSoyad { get; set; } = string.Empty;
    public int PersonelId { get; set; }
}

public class CreatePersonelTestDto
{
    [Required(ErrorMessage = "Ad alanı gereklidir.")]
    [StringLength(100, ErrorMessage = "Ad alanı en fazla 100 karakter olabilir.")]
    [Display(Name = "Ad")]
    public string? Ad { get; set; } = string.Empty;

    [Required(ErrorMessage = "Soyad alanı gereklidir.")]
    [StringLength(100, ErrorMessage = "Soyad alanı en fazla 100 karakter olabilir.")]
    [Display(Name = "Soyad")]
    public string? Soyad { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Personel alanı gereklidir.")]
    [Display(Name = "Personel")]
    public int PersonelId { get; set; }
}

public class UpdatePersonelTestDto: CreatePersonelTestDto
{
    public int Id { get; set; }
}