using System.ComponentModel.DataAnnotations;

namespace BilisimPersoneli.Application.DTOs;

public class DepartmanDto
{
    public int Id { get; set; }
    public string Ad { get; set; } = string.Empty;
    public string? Aciklama { get; set; }
}

public class DepartmanCreateDto
{
    [Required(ErrorMessage = "Departman adı zorunludur.")]
    [StringLength(100, ErrorMessage = "Departman adı en fazla 100 karakter olabilir.")]
    [Display(Name = "Ad")]
    public string Ad { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Açıklama")]
    public string? Aciklama { get; set; }
}

public class DepartmanUpdateDto : DepartmanCreateDto
{
    public int Id { get; set; }
}
