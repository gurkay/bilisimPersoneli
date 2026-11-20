using BilisimPersoneli.Application.Common;
using BilisimPersoneli.Application.DTOs;
using BilisimPersoneli.Application.Interfaces;
using BilisimPersoneli.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BilisimPersoneli.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;

    public DashboardService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<DashboardDto> GetDashboardAsync(
        int maasPage = 1,
        int maasPageSize = 10,
        int izinliPage = 1,
        int izinliPageSize = 10,
        int ortalamaPage = 1,
        int ortalamaPageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var maasQuery = _db.Departmanlar
            .AsNoTracking()
            .OrderBy(d => d.Ad)
            .Select(d => new DepartmanMaasOzetiDto
            {
                DepartmanAd = d.Ad,
                PersonelSayisi = d.Personeller.Count(),
                ToplamMaas = d.Personeller.Sum(p => (decimal?)p.Maas) ?? 0m,
                OrtalamaMaas = d.Personeller.Average(p => (decimal?)p.Maas) ?? 0m
            });

        var izinliQuery = _db.Personeller
            .AsNoTracking()
            .Where(p => p.Izinler.Any())
            .OrderBy(p => p.Ad)
            .ThenBy(p => p.Soyad)
            .Select(p => new IzinliPersonelOzetDto
            {
                Ad = p.Ad,
                Soyad = p.Soyad,
                DepartmanAd = p.Departman != null ? p.Departman.Ad : string.Empty,
                IzinKayitSayisi = p.Izinler.Count()
            });

        var ortalamaQuery = _db.Personeller
            .AsNoTracking()
            .Where(p => p.Departman != null)
            .Where(p => p.Maas > p.Departman!.Personeller.Average(x => x.Maas))
            .OrderBy(p => p.Departman!.Ad)
            .ThenBy(p => p.Ad)
            .ThenBy(p => p.Soyad)
            .Select(p => new OrtalamaUstuPersonelDto
            {
                Ad = p.Ad,
                Soyad = p.Soyad,
                DepartmanAd = p.Departman!.Ad,
                Maas = p.Maas,
                DepartmanOrtalamaMaas = p.Departman!.Personeller.Average(x => x.Maas)
            });

        return new DashboardDto
        {
            DepartmanMaasOzetleri = await ToPagedAsync(maasQuery, maasPage, maasPageSize, cancellationToken),
            IzinliPersoneller = await ToPagedAsync(izinliQuery, izinliPage, izinliPageSize, cancellationToken),
            OrtalamaUstuPersoneller = await ToPagedAsync(ortalamaQuery, ortalamaPage, ortalamaPageSize, cancellationToken)
        };
    }

    private static async Task<PagedResult<T>> ToPagedAsync<T>(
        IQueryable<T> query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var (normalizedPage, normalizedSize) = Pagination.Normalize(page, pageSize);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((normalizedPage - 1) * normalizedSize)
            .Take(normalizedSize)
            .ToListAsync(cancellationToken);

        return PagedResult<T>.Create(items, normalizedPage, normalizedSize, total);
    }
}
