using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Ibr.Api.Controllers;
using Ibr.Application;
using Ibr.Infrastructure;
using Ibr.Web;
using Ibr.Web.Components;
using Ibr.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Radzen;

var builder = WebApplication.CreateBuilder(args);

var keyVaultUri = builder.Configuration["KeyVault:Uri"];
if (!string.IsNullOrWhiteSpace(keyVaultUri))
{
    builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), new DefaultAzureCredential());
}

var dotEnv = DotEnv.Read(builder.Environment.ContentRootPath);
var bukSecrets = new Dictionary<string, string?>();
if (dotEnv.TryGetValue("AUTH_TOKEN", out var authToken) && !string.IsNullOrWhiteSpace(authToken))
{
    bukSecrets["Buk:Token"] = authToken;
}

if (dotEnv.TryGetValue("BUK_BASE_URL", out var bukBaseUrl) && !string.IsNullOrWhiteSpace(bukBaseUrl))
{
    bukSecrets["Buk:BaseUrl"] = bukBaseUrl;
}

if (bukSecrets.Count > 0)
{
    builder.Configuration.AddInMemoryCollection(bukSecrets);
}

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.ContentRootPath);
builder.Services.AddHostedService<BukAutoSyncService>();
builder.Services.AddScoped<DialogService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<TooltipService>();
builder.Services.AddScoped<ContextMenuService>();

builder.Services
    .AddControllers()
    .AddApplicationPart(typeof(UsersController).Assembly);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
