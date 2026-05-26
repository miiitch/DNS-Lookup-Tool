using System.Runtime.InteropServices;
using System.Web;
using DnsClient;
using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;
using mpelabs.dnslookup.Components.Dialogs;

namespace mpelabs.dnslookup.Components.Pages;

public partial class Home
{
    private List<QueryType> _queryTypes = [];
    private QueryType _selectedQueryType = QueryType.ANY;
    private string _domainName = "";

    private bool _loading = false;

    [Inject] private NavigationManager NavigationManager { get; set; } = null!;

    private DnsConfiguration _pageDnsConfiguration = new DnsConfiguration();
    private List<DnsServerAndResult> _servers = new List<DnsServerAndResult>();

    private DnsConfiguration GetConfigurationFromQuery()
    {
        var uri = new Uri(NavigationManager.Uri);
        var queryCollection = HttpUtility.ParseQueryString(uri.Query);
        var result = new DnsConfiguration();

        var servers = queryCollection.GetValues("server") ?? [];
        foreach (var server in servers)
        {
            var serverConfig = DnsServerInfo.Load(server);
            if (serverConfig != null)
            {
                result.Servers.AddRange(serverConfig);
            }
        }

        // timeout en millisecondes (paramètre URL: ?timeout=2000)
        if (int.TryParse(queryCollection["timeout"], out var timeout) && timeout > 0)
            result.TimeoutMs = timeout;

        // nombre de retries (paramètre URL: ?retries=1)
        if (int.TryParse(queryCollection["retries"], out var retriesVal) && retriesVal >= 0)
            result.Retries = retriesVal;

        return result;
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        _pageDnsConfiguration = DnsConfigurationFromApp;

        var urlConfiguration = GetConfigurationFromQuery();
        _pageDnsConfiguration = _pageDnsConfiguration.Merge(urlConfiguration);

        _servers = _pageDnsConfiguration.Servers.Select(x => new DnsServerAndResult(x)).ToList();
        _queryTypes = Enum.GetValues(typeof(QueryType)).Cast<QueryType>().OrderBy(_ => _.ToString()).ToList();
    }

    public bool IsDomainValid => Uri.CheckHostName(_domainName) == UriHostNameType.Dns;

    private Task DomainChanged()
    {
        return Task.CompletedTask;
    }

    private async Task ProcessDomain()
    {
        _loading = true;

        // Réinitialiser l'état de chaque serveur et afficher les spinners immédiatement
        foreach (var server in _servers)
        {
            server.IsQuerying = true;
            server.Success = null;
            server.DnsResult = null;
            server.ErrorMessage = null;
        }
        StateHasChanged();

        var timeoutMs = _pageDnsConfiguration.EffectiveTimeoutMs;
        var retries = _pageDnsConfiguration.EffectiveRetries;

        // Lancer toutes les requêtes en parallèle — chaque tâche met à jour l'UI dès qu'elle se termine
        var tasks = _servers.Select(server => Task.Run(async () =>
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(timeoutMs));
            var result = await server.DnsServerInfo.Query(
                _domainName, _selectedQueryType, timeoutMs, retries, cts.Token);

            server.IsQuerying = false;

            if (result.IsError)
            {
                server.DnsResult = null;
                server.Success = false;
                server.ErrorMessage = result.FirstError.Description;
            }
            else
            {
                server.DnsResult = result.Value;
                server.Success = true;
            }

            // Mettre à jour l'UI dès que ce serveur a répondu
            await InvokeAsync(StateHasChanged);
        }));

        await Task.WhenAll(tasks);

        _loading = false;
        StateHasChanged();
    }

    private async Task ShowAuditTrail(DnsServerAndResult dnsServer)
    {
        if (dnsServer?.DnsResult?.Result == null)
        {
            return;
        }

        var dialog = await DialogService.ShowDialogAsync<AuditTrailDialog>(dnsServer.DnsResult?.Result,
            new DialogParameters()
            {
                Height = "500px",
                Width = "700px",
                Title = $"{dnsServer.DnsServerInfo.Name} - audit trail",
                PreventDismissOnOverlayClick = true,
                PreventScroll = true,
                SecondaryAction = null
            });

        var result = await dialog.Result;
    }
}