using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuizApp.Data;
using QuizApp.Services;

namespace QuizApp.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddQuizServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllersWithViews();

        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=quiz.db";
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString));

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.AccessDeniedPath = "/Account/Login";
            });

        services.AddAuthorization();
        services.AddMemoryCache();
        services.AddHttpContextAccessor();

        services.AddScoped<TestFileService>();
        services.AddScoped<ITestFileService>(sp => new CachedTestFileService(
            sp.GetRequiredService<TestFileService>(),
            sp.GetRequiredService<IMemoryCache>()));

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITestImportService, TestImportService>();

        return services;
    }
}
