namespace BilisimPersoneli.Application.Common;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Yonetici = "Yonetici";
    public const string Isletmen = "Isletmen";
    public const string Personel = "Personel";

    public static readonly string[] All =
    {
        Admin,
        Yonetici,
        Isletmen,
        Personel
    };

    /// <summary>Yönetici'nin atayabileceği roller (Admin hariç).</summary>
    public static readonly string[] AssignableByYonetici =
    {
        Yonetici,
        Isletmen,
        Personel
    };

    /// <summary>Tüm personel / izin listelerini görebilen roller.</summary>
    public const string Staff = $"{Admin},{Yonetici},{Isletmen}";

    public const string AdminAndYonetici = $"{Admin},{Yonetici}";

    public const string AdminAndIsletmen = $"{Admin},{Isletmen}";

    public static bool IsStaff(IEnumerable<string> roles) =>
        roles.Any(r => r is Admin or Yonetici or Isletmen);

    /// <summary>Tek birincil rol seçimi için öncelik sırası.</summary>
    public static string PickPrimaryRole(IEnumerable<string> roles)
    {
        foreach (var role in All)
        {
            if (roles.Contains(role, StringComparer.OrdinalIgnoreCase))
                return role;
        }

        return Personel;
    }

    public static string[] AssignableRolesFor(bool isAdmin) =>
        isAdmin ? All : AssignableByYonetici;
}
