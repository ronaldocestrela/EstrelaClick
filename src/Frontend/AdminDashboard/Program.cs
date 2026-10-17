using EstrelaClick.Frontend.AdminDashboard.Components;
using EstrelaClick.Frontend.AdminDashboard.Services;
using EstrelaClick.Shared.SharedKernel.Configuration;

// Carrega variáveis de ambiente do arquivo .env (se presente)
DotEnv.Load();

var builder = WebApplication.CreateBuilder(args);

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

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
