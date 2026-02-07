using OpenPrinterWeb.Components;
using OpenPrinterWeb.Services;
using MudBlazor.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Localization;
using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

builder.Services.AddControllers();
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddSingleton<IPrintService, CupsPrintService>();
builder.Services.AddSingleton<ISharpIppClientWrapper, SharpIppClientAdapter>();
builder.Services.AddSingleton<IProcessExecutor, ProcessExecutor>();
builder.Services.AddSingleton<IFileSystem, PhysicalFileSystem>();
builder.Services.AddSingleton<IPdfConverter, LibreOfficePdfConverter>();
builder.Services.AddScoped<IFileUploadService, FileUploadService>();
builder.Services.AddHostedService<PrinterStatusBackgroundService>();
builder.Services.AddHostedService<FileCleanupBackgroundService>();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, JwtAuthenticationStateProvider>();
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

var dataProtectionPath = Path.Combine(builder.Environment.ContentRootPath, "data", "keys");
if (!Directory.Exists(dataProtectionPath))
{
    Directory.CreateDirectory(dataProtectionPath);
}
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
    .SetApplicationName("OpenPrinterWeb");

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secret = jwtSettings["Secret"] ?? "OpenPrinterWeb_Super_Secret_Key_2025_Secure";
var key = Encoding.UTF8.GetBytes(secret);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };

    // Allow JWT in cookies for static file protection (optional, but requested for wwwroot)
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/printerhub"))
            {
                context.Token = accessToken;
            }
            else
            {
                // Also check cookie for static files
                context.Request.Cookies.TryGetValue("authToken", out var token);
                if (!string.IsNullOrEmpty(token))
                {
                    context.Token = token;
                }
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

var supportedCultures = new[] { "en-US", "zh-Hans", "ja", "ru", "de", "es", "it", "ko", "fr" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture("zh-Hans")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);
localizationOptions.RequestCultureProviders = new IRequestCultureProvider[]
{
    new CookieRequestCultureProvider(),
    new QueryStringRequestCultureProvider()
};

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseRequestLocalization(localizationOptions);
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Protect specific static files (uploads and others)
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value;
    if (path != null && path.StartsWith("/uploads", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.Headers["Access-Control-Allow-Origin"] = "https://mozilla.github.io";
        await next();
        return;
    }

    if (path != null &&
        !path.StartsWith("/login", StringComparison.OrdinalIgnoreCase) &&
        !path.StartsWith("/api/auth", StringComparison.OrdinalIgnoreCase) &&
        !path.StartsWith("/_framework", StringComparison.OrdinalIgnoreCase) &&
        !path.StartsWith("/_content", StringComparison.OrdinalIgnoreCase) &&
        !path.StartsWith("/_blazor", StringComparison.OrdinalIgnoreCase) &&
        !path.Equals("/favicon.png", StringComparison.OrdinalIgnoreCase) &&
        !path.EndsWith(".css", StringComparison.OrdinalIgnoreCase) &&
        !path.EndsWith(".js", StringComparison.OrdinalIgnoreCase) &&
        !path.Equals("/", StringComparison.OrdinalIgnoreCase) &&
        !path.Equals("/not-found", StringComparison.OrdinalIgnoreCase))
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            // Only redirect to login for GET requests that don't look like static assets
            bool isPageRequest = context.Request.Method == "GET" && !path.Contains('.');
            
            if (isPageRequest)
            {
                context.Response.Redirect($"/login?returnUrl={Uri.EscapeDataString(path)}");
            }
            else
            {
                context.Response.StatusCode = 401;
            }
            return;
        }
    }
    await next();
});

// Serve files from the persistent uploads directory
var webRootPath = app.Environment.WebRootPath;
var uploadsPath = Path.Combine(webRootPath, "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

app.UseStaticFiles();

app.MapStaticAssets();

app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
