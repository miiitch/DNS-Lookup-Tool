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


        return result;
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();


        _pageDnsConfiguration = DnsConfigurationFromApp;

        var urlConfiguration = GetConfigurationFromQuery();
        if (urlConfiguration.Servers.Count > 0)
        {
            _pageDnsConfiguration = _pageDnsConfiguration.Merge(urlConfiguration);    
        }

        


        _servers = _pageDnsConfiguration.Servers.Select(x => new DnsServerAndResult(x)).ToList();
        _queryTypes = Enum.GetValues(typeof(QueryType)).Cast<QueryType>().OrderBy(_ => _.ToString()).ToList();
    }


    public bool IsDomainValid => Uri.CheckHostName(_domainName) == UriHostNameType.Dns;

    private Task DomainChanged()
    {
        var b = IsDomainValid;

        return Task.CompletedTask;
    }

    private async Task ProcessDomain()
    {
        _loading = true;
        this.StateHasChanged();
        foreach (var server in _servers)
        {
            var result = await server.DnsServerInfo.Query(_domainName, _selectedQueryType);
            if (result.IsError)
            {
                server.DnsResult = null;
                server.Success = false;
                server.ErrorMessage = result.FirstError.Description;
                continue;
            }

            server.DnsResult = result.Value;
            server.Success = true;
        }

        _loading = false;
        this.StateHasChanged();
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