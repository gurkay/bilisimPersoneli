using BilisimPersoneli.Domain.Common;

namespace BilisimPersoneli.Domain.Entities;

public class PersonelTest : BaseEntity
{
    public string Ad { get; set; } = string.Empty;
    public string Soyad { get; set; } = string.Empty;
    public int PersonelId { get; set; }
    public Personel? Personel { get; set; }
}