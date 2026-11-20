using System.ComponentModel.DataAnnotations;
using BilisimPersoneli.Domain.Enums;

namespace BilisimPersoneli.Application.DTOs;

public class IzinDto
{
    public int Id { get; set; }
    public int PersonelId { get; set; }
    public string? PersonelAdSoyad { get; set; }
    public DateTime BaslangicTarihi { get; set; }
    public DateTime BitisTarihi { get; set; }
    public IzinTuru IzinTuru { get; set; }
    public IzinDurumu Durum { get; set; }
    public string? Aciklama { get; set; }
    public string? OnaylayanUserId { get; set; }
    public DateTime? OnayTarihi { get; set; }
    public string? OnayNotu { get; set; }
}

public class IzinCreateDto
{
    [Required(ErrorMessage = "Personel seçiniz.")]
    [Display(Name = "Personel")]
    public int PersonelId { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Başlangıç Tarihi")]
    public DateTime BaslangicTarihi { get; set; } = DateTime.Today;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Bitiş Tarihi")]
    public DateTime BitisTarihi { get; set; } = DateTime.Today;

    [Required]
    [Display(Name = "İzin Türü")]
    public IzinTuru IzinTuru { get; set; } = IzinTuru.Yillik;

    [StringLength(500)]
    [Display(Name = "Açıklama")]
    public string? Aciklama { get; set; }
}

public class IzinUpdateDto : IzinCreateDto
{
    public int Id { get; set; }
}

public class IzinReddetDto
{
    [StringLength(500)]
    [Display(Name = "Onay Notu")]
    public string? OnayNotu { get; set; }
}
