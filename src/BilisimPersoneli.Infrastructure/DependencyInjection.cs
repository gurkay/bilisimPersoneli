using BilisimPersoneli.Application.Interfaces;
using BilisimPersoneli.Infrastructure.Identity;
using BilisimPersoneli.Infrastructure.Persistence;
using BilisimPersoneli.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BilisimPersoneli.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 6;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IDepartmanService, DepartmanService>();
        services.AddScoped<IPersonelService, PersonelService>();
        services.AddScoped<IIzinService, IzinService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IUserManagementService, UserManagementService>();

        return services;
    }
}
