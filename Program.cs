using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using SkillBridge.Web.Features.Administration;
using SkillBridge.Web.Features.Community;
using SkillBridge.Web.Features.Courses;
using SkillBridge.Web.Features.Lessons;
using SkillBridge.Web.Features.Quizzes;
using SkillBridge.Web.Infrastructure.Authentication;
using SkillBridge.Web.Infrastructure.Administration;
using SkillBridge.Web.Infrastructure.Community;
using SkillBridge.Web.Infrastructure.Courses;
using SkillBridge.Web.Infrastructure.Content;
using SkillBridge.Web.Infrastructure.Quizzes;

var isAdminBootstrap = args.Contains("--create-admin", StringComparer.Ordinal);
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args.Where(argument => argument != "--create-admin").ToArray(),
    WebRootPath = "Frontend/wwwroot"
});

builder.Services.AddAccountFoundation(builder.Configuration);
builder.Services.AddScoped<ICourseReader, MySqlCourseReader>();
builder.Services.AddScoped<IForumReader, MySqlForumReader>();
builder.Services.AddScoped<IDashboardReader, MySqlDashboardReader>();
builder.Services.AddScoped<ILessonReader, MySqlLessonReader>();
builder.Services.AddScoped<IQuizReader, MySqlQuizReader>();
builder.Services.AddRazorPages(options =>
{
    options.RootDirectory = "/Frontend/Pages";
    string[] owners = ["JianYi", "ChangZhe", "Darren", "Timothy", "Hamzah"];
    foreach (var owner in owners)
        options.Conventions.AuthorizeFolder($"/{owner}/Admin", "Admin");
    options.Conventions.AuthorizeFolder("/Darren/Lessons");
    options.Conventions.AuthorizeFolder("/Darren/Resources");
    options.Conventions.AuthorizeFolder("/Timothy/Quizzes");
    options.Conventions.AddFolderRouteModelConvention("/", page =>
    {
        foreach (var selector in page.Selectors)
        {
            var template = selector.AttributeRouteModel?.Template;
            if (template is null)
                continue;

            var owner = template.Split('/', 2)[0];
            if (!owners.Contains(owner, StringComparer.Ordinal))
                continue;

            selector.AttributeRouteModel!.Template = template.Length == owner.Length ? string.Empty : template[(owner.Length + 1)..];
        }
    });
});
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
});
builder.Services.PostConfigure<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var isCredentialPost = HttpMethods.IsPost(context.Request.Method)
            && (context.Request.Path.Equals("/Account/Login", StringComparison.OrdinalIgnoreCase)
                || context.Request.Path.Equals("/Account/Register", StringComparison.OrdinalIgnoreCase));
        if (!isCredentialPost)
        {
            return RateLimitPartition.GetNoLimiter("other");
        }

        var address = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(address, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });
});

await using var app = builder.Build();
if (isAdminBootstrap)
{
    Environment.ExitCode = await AdminBootstrap.RunAsync(app.Services, app.Lifetime.ApplicationStopping);
    return;
}

app.UseExceptionHandler("/Error");
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.Use(async (context, next) =>
{
    context.Response.Headers.ContentSecurityPolicy = "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; font-src 'self'; media-src 'self'; object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'";
    context.Response.Headers.XContentTypeOptions = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next(context);
});
app.UseStatusCodePagesWithReExecute("/Status/{0}");
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();
app.MapGet("/health", () => Results.Ok(new { status = "alive" })).AllowAnonymous();
app.MapRazorPages();
await app.RunAsync();

public partial class Program;
