using BilisimPersoneli.Application.Common;
using BilisimPersoneli.Domain.Entities;
using BilisimPersoneli.Domain.Enums;
using BilisimPersoneli.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BilisimPersoneli.Infrastructure.Persistence;

public static class DbSeeder
{
    public const string DefaultPassword = "Sifre123!";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");
        var context = sp.GetRequiredService<AppDbContext>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();

        await context.Database.MigrateAsync();

        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var admin = await EnsureUserAsync(userManager, "admin@local", AppRoles.Admin);
        var yonetici = await EnsureUserAsync(userManager, "yonetici@local", AppRoles.Yonetici);
        var isletmen = await EnsureUserAsync(userManager, "isletmen@local", AppRoles.Isletmen);
        var personelUser = await EnsureUserAsync(userManager, "personel@local", AppRoles.Personel);

        if (!await context.Departmanlar.IgnoreQueryFilters().AnyAsync())
        {
            var bilisim = new Departman { Ad = "Bilişim", Aciklama = "Bilgi işlem birimi", CreatedAt = DateTime.UtcNow, CreatedBy = "seed" };
            var ik = new Departman { Ad = "İnsan Kaynakları", Aciklama = "İK birimi", CreatedAt = DateTime.UtcNow, CreatedBy = "seed" };
            context.Departmanlar.AddRange(bilisim, ik);
            await context.SaveChangesAsync();

            var personel = new Personel
            {
                Ad = "Ali",
                Soyad = "Yılmaz",
                Email = "personel@local",
                Telefon = "5551112233",
                Maas = 45000m,
                IseGirisTarihi = new DateTime(2022, 1, 15),
                DepartmanId = bilisim.Id,
                IdentityUserId = personelUser.Id,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "seed"
            };
            context.Personeller.Add(personel);
            await context.SaveChangesAsync();

            context.Izinler.Add(new Izin
            {
                PersonelId = personel.Id,
                BaslangicTarihi = DateTime.Today.AddDays(7),
                BitisTarihi = DateTime.Today.AddDays(10),
                IzinTuru = IzinTuru.Yillik,
                Durum = IzinDurumu.Beklemede,
                Aciklama = "Örnek izin talebi",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "seed"
            });
            await context.SaveChangesAsync();

            context.PersonelTests.Add(new PersonelTest
            {
                PersonelId = personel.Id,
                Ad = personel.Ad,
                Soyad = personel.Soyad,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "seed"
            });
            await context.SaveChangesAsync();
        }
        else
        {
            var existing = await context.Personeller
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Email == "personel@local");
            if (existing != null && string.IsNullOrEmpty(existing.IdentityUserId))
            {
                existing.IdentityUserId = personelUser.Id;
                await context.SaveChangesAsync();
            }
        }

        logger.LogInformation(
            "Seed completed. Users: {Admin}, {Yonetici}, {Isletmen}, {Personel}",
            admin.Email, yonetici.Email, isletmen.Email, personelUser.Email);
    }

    private static async Task<ApplicationUser> EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                IsApproved = true
            };
            var result = await userManager.CreateAsync(user, DefaultPassword);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"User seed failed for {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
        else if (!user.IsApproved)
        {
            user.IsApproved = true;
            await userManager.UpdateAsync(user);
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            await userManager.AddToRoleAsync(user, role);
        }

        return user;
    }
}
