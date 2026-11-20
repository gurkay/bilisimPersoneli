namespace BilisimPersoneli.Application.Interfaces;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserName { get; }
    bool IsInRole(string role);
    bool IsAuthenticated { get; }

    /// <summary>Admin, Yönetici veya İşletmen — tüm personel/izin listelerini görebilir.</summary>
    bool IsStaff { get; }
}
