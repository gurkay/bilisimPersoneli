using Microsoft.AspNetCore.Identity;

namespace BilisimPersoneli.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    /// <summary>Kayıt Admin/Yönetici tarafından onaylanmadan giriş yapılamaz.</summary>
    public bool IsApproved { get; set; }
}
