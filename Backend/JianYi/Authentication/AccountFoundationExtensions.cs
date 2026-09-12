using Microsoft.AspNetCore.Authentication.Cookies;
using SkillBridge.Web.Features.Accounts;
using SkillBridge.Web.Infrastructure.Data;

namespace SkillBridge.Web.Infrastructure.Authentication;

public static class AccountFoundationExtensions
{
    public static IServiceCollection AddAccountFoundation(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(new MySqlConnectionFactory(configuration));
        services.AddScoped<IAccountRepository, MySqlAccountRepository>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<AccountService>();
        services.AddScoped<AccountCookieEvents>();
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.LogoutPath = "/Account/Logout";
            options.Cookie.Name = "SkillBridge.Session";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
            options.SlidingExpiration = true;
            options.EventsType = typeof(AccountCookieEvents);
        });
        services.AddAuthorization(options => options.AddPolicy("Admin", policy => policy.RequireRole(UserRole.Admin.ToString())));
        return services;
    }
}
