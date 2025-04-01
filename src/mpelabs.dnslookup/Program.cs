using Microsoft.FluentUI.AspNetCore.Components;
using mpelabs.dnslookup;
using mpelabs.dnslookup.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddFluentUIComponents();

var dnsFromEnv = Environment.GetEnvironmentVariable("DNS_SERVERS");
var dnsConfiguration = string.IsNullOrWhiteSpace(dnsFromEnv)
    ? new DnsConfiguration()
    : DnsConfiguration.Load(dnsFromEnv);

Console.WriteLine($"{dnsConfiguration.Servers.Count} servers loaded from environment variable: '{dnsFromEnv}'");

builder.Services.AddSingleton(dnsConfiguration);

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
    .AddInteractiveServerRenderMode()
    .DisableAntiforgery();

app.Run();