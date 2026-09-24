using CvManagement.Application.Interfaces;
using CvManagement.Domain.Entities;
using CvManagement.Infrastructure;
using CvManagement.Infrastructure.Data;
using CvManagement.Infrastructure.Services;
using CvManagement.Web;
using CvManagement.Web.Components;
using CvManagement.Web.Services;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using MudBlazor.Services;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. Blazor Web App (.NET 10 style)
// ==========================================
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ==========================================
// 1b. MVC Controllers + Antiforgery support
// ==========================================
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// HttpContext accessor (for error page)
builder.Services.AddHttpContextAccessor();

// ==========================================
// 2. Infrastructure (EF Core + PostgreSQL)
// ==========================================
builder.Services.AddInfrastructure(builder.Configuration);

// ==========================================
// 3. ASP.NET Core Identity
// ==========================================
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 6;

    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.AccessDeniedPath = "/access-denied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// ==========================================
// 3b. External Authentication (Social Login)
// ==========================================
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
var facebookAppId = builder.Configuration["Authentication:Facebook:AppId"];
var facebookAppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];

var authBuilder = builder.Services.AddAuthentication();

// Google OAuth (only if real credentials configured)
if (!string.IsNullOrEmpty(googleClientId) &&
    !googleClientId.StartsWith("YOUR_") &&
    !string.IsNullOrEmpty(googleClientSecret))
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.CallbackPath = "/signin-google";
        options.SaveTokens = true;

        options.Events = new OAuthEvents
        {
            OnRemoteFailure = context =>
            {
                context.Response.Redirect("/login?error=Google+login+failed");
                context.HandleResponse();
                return Task.CompletedTask;
            }
        };
    });
}

// Facebook OAuth (only if real credentials configured)
if (!string.IsNullOrEmpty(facebookAppId) &&
    !facebookAppId.StartsWith("YOUR_") &&
    !string.IsNullOrEmpty(facebookAppSecret))
{
    authBuilder.AddFacebook(options =>
    {
        options.AppId = facebookAppId;
        options.AppSecret = facebookAppSecret;
        options.CallbackPath = "/signin-facebook";
        options.SaveTokens = true;

        options.Events = new OAuthEvents
        {
            OnRemoteFailure = context =>
            {
                context.Response.Redirect("/login?error=Facebook+login+failed");
                context.HandleResponse();
                return Task.CompletedTask;
            }
        };
    });
}

builder.Services.AddAuthorization();
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();

// ==========================================
// 4. Custom Auth State Provider
// ==========================================
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();

// ==========================================
// 5. UI Services
// ==========================================
builder.Services.AddMudServices();

// ==========================================
// 6. Browser Storage (built-in)
// ==========================================
builder.Services.AddScoped<ProtectedLocalStorage>();

// ==========================================
// 7. Localization (i18n) — English + Bangla
// ==========================================
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddScoped<LocalizationService>();

var supportedCultures = new[] { "en", "bn" };
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.SetDefaultCulture("en")
        .AddSupportedCultures(supportedCultures)
        .AddSupportedUICultures(supportedCultures);
});

// ==========================================
// 8. Custom Application Services
// ==========================================
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<LayoutService>();
builder.Services.AddScoped<IAttributeService, AttributeService>();
builder.Services.AddScoped<IPositionService, PositionService>();
builder.Services.AddScoped<ICvService, CvService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<AutoSaveService>();
builder.Services.AddScoped<IDiscussionService, DiscussionService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();

var app = builder.Build();

// ==========================================
// Migrate + Seed Database
// ==========================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        await DbSeeder.SeedAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error during database migration/seeding.");
        throw;
    }
}

// ==========================================
// Middleware Pipeline
// ==========================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseRequestLocalization();

app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();