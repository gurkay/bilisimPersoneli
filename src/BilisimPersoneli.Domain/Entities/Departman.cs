using BilisimPersoneli.Domain.Common;

namespace BilisimPersoneli.Domain.Entities;

public class Departman : BaseEntity
{
    public string Ad { get; set; } = string.Empty;
    public string? Aciklama { get; set; }

    public ICollection<Personel> Personeller { get; set; } = new List<Personel>();
}
