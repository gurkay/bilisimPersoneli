using BilisimPersoneli.Domain.Common;
using BilisimPersoneli.Domain.Enums;

namespace BilisimPersoneli.Domain.Entities;

public class Izin : BaseEntity
{
    public int PersonelId { get; set; }
    public Personel? Personel { get; set; }

    public DateTime BaslangicTarihi { get; set; }
    public DateTime BitisTarihi { get; set; }
    public IzinTuru IzinTuru { get; set; }
    public IzinDurumu Durum { get; set; } = IzinDurumu.Beklemede;
    public string? Aciklama { get; set; }

    public string? OnaylayanUserId { get; set; }
    public DateTime? OnayTarihi { get; set; }
    public string? OnayNotu { get; set; }
}
