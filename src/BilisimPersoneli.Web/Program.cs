using System.Text;
using BilisimPersoneli.Application.Mapping;
using BilisimPersoneli.Infrastructure;
using BilisimPersoneli.Infrastructure.Persistence;
using BilisimPersoneli.Web.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddControllers();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAutoMapper(cfg => { }, typeof(MappingProfile).Assembly);
builder.Services.AddScoped<JwtTokenService>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
});

var jwtSection = builder.Configuration.GetSection("Jwt");
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!));

builder.Services.AddAuthentication()
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

await SeedWithRetryAsync(app.Services);

static async Task SeedWithRetryAsync(IServiceProvider services)
{
    const int maxAttempts = 15;
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await DbSeeder.SeedAsync(services);
            return;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            Console.WriteLine($"DB henüz hazır değil (deneme {attempt}/{maxAttempts}): {ex.Message}");
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.Run();
