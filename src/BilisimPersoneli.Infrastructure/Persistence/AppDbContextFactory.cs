using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BilisimPersoneli.Infrastructure.Persistence;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=localhost,1433;Database=BilisimPersoneli;User Id=sa;Password=Your_strong_Password123;TrustServerCertificate=True;");
        return new AppDbContext(optionsBuilder.Options);
    }
}
