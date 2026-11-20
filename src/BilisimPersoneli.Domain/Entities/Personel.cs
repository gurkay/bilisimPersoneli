using BilisimPersoneli.Domain.Common;

namespace BilisimPersoneli.Domain.Entities;

public class Personel : BaseEntity
{
    public string Ad { get; set; } = string.Empty;
    public string Soyad { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefon { get; set; }
    public decimal Maas { get; set; }
    public DateTime IseGirisTarihi { get; set; }

    public int DepartmanId { get; set; }
    public Departman? Departman { get; set; }

    public string? IdentityUserId { get; set; }

    public ICollection<Izin> Izinler { get; set; } = new List<Izin>();
    public ICollection<PersonelTest> PersonelTestler { get; set; } = new List<PersonelTest>();
}
