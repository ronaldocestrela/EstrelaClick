using System.Security.Claims;
using EstrelaClick.Frontend.AdminDashboard.Components;
using EstrelaClick.Frontend.AdminDashboard.Services;
using EstrelaClick.Shared.SharedKernel.Admin;
using EstrelaClick.Shared.SharedKernel.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

// Carrega variáveis de ambiente do arquivo .env (se presente)
DotEnv.Load();

var builder = WebApplication.CreateBuilder(args);

// Configuração de segurança do administrador master via .env / appsettings
builder.Services.Configure<AdminSecurityOptions>(builder.Configuration.GetSection(AdminSecurityOptions.SectionName));
builder.Services.AddSingleton<IAdminAuthService, AdminAuthService>();

// Autenticação baseada em Cookie seguro para Blazor Server
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = ".EstrelaClick.AdminAuth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.LoginPath = "/login";
        options.LogoutPath = "/auth/admin-logout";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(480);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var apiBaseUrl = builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5180";
var apiKey = builder.Configuration["Api:ApiKey"] ?? "dev-factory-secret-key-2026";

builder.Services.AddHttpClient<IAdminApiClient, AdminApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddTypedClient<IAdminApiClient>((httpClient, sp) => new AdminApiClient(httpClient, apiKey));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();

// Endpoints de autenticação para o AdminDashboard
app.MapPost("/auth/admin-login", async (
    HttpContext httpContext,
    [FromForm] string? usuario,
    [FromForm] string? senha,
    [FromForm] string? returnUrl,
    [FromServices] IAdminAuthService authService,
    [FromServices] IOptions<AdminSecurityOptions> secOptions) =>
{
    var request = new AdminLoginRequest(usuario ?? string.Empty, senha ?? string.Empty);
    var resultado = authService.ValidarCredenciais(request);

    if (resultado.IsFailure)
    {
        var errorCode = resultado.Error.Code switch
        {
            "admin.auth.campos_obrigatorios" => "campos_obrigatorios",
            "admin.auth.configuracao_invalida" => "configuracao_invalida",
            _ => "credenciais_invalidas"
        };

        var safeReturn = !string.IsNullOrWhiteSpace(returnUrl) && returnUrl.StartsWith('/') && !returnUrl.StartsWith("//")
            ? Uri.EscapeDataString(returnUrl)
            : string.Empty;

        var redirectTarget = string.IsNullOrEmpty(safeReturn)
            ? $"/login?Error={errorCode}"
            : $"/login?Error={errorCode}&ReturnUrl={safeReturn}";

        return Results.Redirect(redirectTarget);
    }

    var claims = new List<Claim>
    {
        new(ClaimTypes.Name, resultado.Value!.Usuario),
        new(ClaimTypes.Role, resultado.Value.Role),
        new("AuthenticatedAt", resultado.Value.AutenticadoEmUtc.ToString("o"))
    };

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);
    var timeout = TimeSpan.FromMinutes(Math.Max(15, secOptions.Value.SessionTimeoutMinutes));

    var authProperties = new AuthenticationProperties
    {
        IsPersistent = true,
        ExpiresUtc = DateTimeOffset.UtcNow.Add(timeout)
    };

    await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

    var destino = !string.IsNullOrWhiteSpace(returnUrl) && returnUrl.StartsWith('/') && !returnUrl.StartsWith("//")
        ? returnUrl
        : "/";

    return Results.Redirect(destino);
}).DisableAntiforgery();

app.MapPost("/auth/admin-logout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
}).DisableAntiforgery();

app.MapGet("/auth/admin-logout", async (HttpContext httpContext) =>
{
    await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
