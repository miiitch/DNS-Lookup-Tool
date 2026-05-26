using Microsoft.FluentUI.AspNetCore.Components;
using mpelabs.dnslookup;
using mpelabs.dnslookup.Components;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddFluentUIComponents();

var dnsFromEnv = Environment.GetEnvironmentVariable("DNS_SERVERS");
var dnsConfiguration = string.IsNullOrWhiteSpace(dnsFromEnv)
    ? new DnsConfiguration()
    : DnsConfiguration.Load(dnsFromEnv);

// Timeout en millisecondes (DNS_TIMEOUT_MS, défaut 2000)
var timeoutMsEnv = Environment.GetEnvironmentVariable("DNS_TIMEOUT_MS");
if (int.TryParse(timeoutMsEnv, out var timeoutMs) && timeoutMs > 0)
    dnsConfiguration.TimeoutMs = timeoutMs;

// Nombre de retries (DNS_RETRIES, défaut 1)
var retriesEnv = Environment.GetEnvironmentVariable("DNS_RETRIES");
if (int.TryParse(retriesEnv, out var retries) && retries >= 0)
    dnsConfiguration.Retries = retries;

Console.WriteLine($"{dnsConfiguration.Servers.Count} servers loaded from environment variable: '{dnsFromEnv}'");
Console.WriteLine($"Timeout: {dnsConfiguration.EffectiveTimeoutMs}ms | Retries: {dnsConfiguration.EffectiveRetries}");

builder.Services.AddSingleton(dnsConfiguration);
builder.Services.AddSingleton<Customization>();
builder.Services.AddAntiforgery(options => { options.Cookie.Expiration = TimeSpan.Zero;});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    //app.UseHsts();
}

//app.UseHttpsRedirection();

app.UseStaticFiles();


app.MapRazorComponents<App>()
    .DisableAntiforgery()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();


app.Run();