using BilisimPersoneli.Application.Common;

namespace BilisimPersoneli.Application.DTOs;

public class DepartmanMaasOzetiDto
{
    public string DepartmanAd { get; set; } = string.Empty;
    public int PersonelSayisi { get; set; }
    public decimal OrtalamaMaas { get; set; }
    public decimal ToplamMaas { get; set; }
}

public class IzinliPersonelOzetDto
{
    public string Ad { get; set; } = string.Empty;
    public string Soyad { get; set; } = string.Empty;
    public string DepartmanAd { get; set; } = string.Empty;
    public int IzinKayitSayisi { get; set; }
}

public class OrtalamaUstuPersonelDto
{
    public string Ad { get; set; } = string.Empty;
    public string Soyad { get; set; } = string.Empty;
    public string DepartmanAd { get; set; } = string.Empty;
    public decimal Maas { get; set; }
    public decimal DepartmanOrtalamaMaas { get; set; }
}

public class DashboardDto
{
    public PagedResult<DepartmanMaasOzetiDto> DepartmanMaasOzetleri { get; set; } =
        PagedResult<DepartmanMaasOzetiDto>.Create(Array.Empty<DepartmanMaasOzetiDto>(), 1, PaginationDefaults.DefaultPageSize, 0);

    public PagedResult<IzinliPersonelOzetDto> IzinliPersoneller { get; set; } =
        PagedResult<IzinliPersonelOzetDto>.Create(Array.Empty<IzinliPersonelOzetDto>(), 1, PaginationDefaults.DefaultPageSize, 0);

    public PagedResult<OrtalamaUstuPersonelDto> OrtalamaUstuPersoneller { get; set; } =
        PagedResult<OrtalamaUstuPersonelDto>.Create(Array.Empty<OrtalamaUstuPersonelDto>(), 1, PaginationDefaults.DefaultPageSize, 0);
}
