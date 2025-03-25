using Microsoft.FluentUI.AspNetCore.Components;
using mpelabs.dnslookup;
using mpelabs.dnslookup.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddFluentUIComponents();

var test = new DnsConfiguration()
{
    Servers =
    [
        new DnsServerInfo()
        {
            Name = "Google 1",
            ServerIpAddress = "8.8.4.4"
        },
        new DnsServerInfo()
        {
            Name = "Google 2",
            ServerIpAddress = "8.8.8.8"
        },
        new DnsServerInfo()
        {
            Name = "Default Server",
            ServerIpAddress = ""
        }
    ]
};

builder.Services.AddSingleton(test);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();