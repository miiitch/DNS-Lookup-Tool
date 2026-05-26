using System.Runtime.InteropServices;
using System.Text;
using System.Web;
using DnsClient;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;
using mpelabs.dnslookup.Components.Dialogs;

namespace mpelabs.dnslookup.Components.Pages;

public partial class Home
{
    // ── État principal ────────────────────────────────────────────────────────
    private List<QueryType> _queryTypes = Enum.GetValues(typeof(QueryType))
        .Cast<QueryType>().OrderBy(q => q.ToString()).ToList();
    private QueryType _selectedQueryType = QueryType.ANY;
    private string _domainName = "";
    private bool _loading = false;
    private bool _autoQuery = false;

    // ── Historique (feature 6) ────────────────────────────────────────────────
    private List<string> _history = [];
    private const int MaxHistory = 10;
    private bool _jsReady = false;

    // ── Ajout serveur dynamique (feature 9) ───────────────────────────────────
    private bool _showAddServer = false;
    private string _addServerInput = "";

    // ── Injections ────────────────────────────────────────────────────────────
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;

    private DnsConfiguration _pageDnsConfiguration = new DnsConfiguration();
    private List<DnsServerAndResult> _servers = new List<DnsServerAndResult>();

    // ── Initialisation ────────────────────────────────────────────────────────

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
                result.Servers.AddRange(serverConfig);
        }

        if (int.TryParse(queryCollection["timeout"], out var timeout) && timeout > 0)
            result.TimeoutMs = timeout;

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

        // Feature 5 — pré-remplissage depuis l'URL (?q=domain&type=A)
        var uri = new Uri(NavigationManager.Uri);
        var qs = HttpUtility.ParseQueryString(uri.Query);
        var q = qs["q"];
        if (!string.IsNullOrWhiteSpace(q))
        {
            _domainName = q;
            var typeStr = qs["type"];
            if (!string.IsNullOrWhiteSpace(typeStr) && Enum.TryParse<QueryType>(typeStr, true, out var parsedType))
                _selectedQueryType = parsedType;
            _autoQuery = true;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _jsReady = true;
            await LoadHistoryAsync();

            // Lancer automatiquement si pré-rempli depuis l'URL
            if (_autoQuery && IsDomainValid)
            {
                _autoQuery = false;
                await ProcessDomain();
            }
        }
    }

    // ── Historique (feature 6) ────────────────────────────────────────────────

    private async Task LoadHistoryAsync()
    {
        try
        {
            var history = await JS.InvokeAsync<string[]>("dnsLookup.getHistory");
            _history = history?.ToList() ?? [];
            StateHasChanged();
        }
        catch { /* pas de localStorage = pas d'historique */ }
    }

    private async Task SaveHistoryAsync(string domain)
    {
        _history.Remove(domain);
        _history.Insert(0, domain);
        if (_history.Count > MaxHistory)
            _history = _history.Take(MaxHistory).ToList();
        try
        {
            await JS.InvokeVoidAsync("dnsLookup.saveHistory", (object)_history.ToArray());
        }
        catch { }
    }

    private void SelectHistoryItem(string domain)
    {
        _domainName = domain;
        StateHasChanged();
    }

    private async Task ShowHistoryDialog()
    {
        if (_history.Count == 0) return;

        var dialog = await DialogService.ShowDialogAsync<HistoryDialog>(
            _history,
            new DialogParameters()
            {
                Height = "auto",
                Width = "420px",
                Title = "Recent searches",
                PreventDismissOnOverlayClick = false,
                PreventScroll = false,
                PrimaryAction = null,
                SecondaryAction = null
            });

        var result = await dialog.Result;
        if (!result.Cancelled && result.Data is string selected)
        {
            _domainName = selected;
            StateHasChanged();
        }
    }

    // ── Validation & saisie ────────────────────────────────────────────────────

    public bool IsDomainValid => Uri.CheckHostName(_domainName) == UriHostNameType.Dns;

    private Task DomainChanged() => Task.CompletedTask;

    // Feature 2 — touche Entrée
    private async Task OnDomainKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && IsDomainValid && !_loading)
            await ProcessDomain();
    }

    // Feature 9 — touche Entrée dans le champ "Add server"
    private void OnAddServerKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
            AddServer();
    }

    // ── Requête principale ────────────────────────────────────────────────────

    private async Task ProcessDomain()
    {
        _loading = true;
        foreach (var server in _servers)
        {
            server.IsQuerying = true;
            server.Success = null;
            server.DnsResult = null;
            server.ErrorMessage = null;
            server.IsDifferent = false;
        }
        StateHasChanged();

        var timeoutMs = _pageDnsConfiguration.EffectiveTimeoutMs;
        var retries = _pageDnsConfiguration.EffectiveRetries;

        // Toutes les requêtes en parallèle — UI mise à jour dès que chaque serveur répond
        var tasks = _servers.Select(server => Task.Run(async () =>
        {
            // CancellationToken global : timeout × (retries+1) + marge
            using var cts = new CancellationTokenSource(
                TimeSpan.FromMilliseconds(timeoutMs * (retries + 1) + 500));

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

            await InvokeAsync(StateHasChanged);
        }));

        await Task.WhenAll(tasks);

        // Feature 8 — comparaison inter-serveurs
        ComputeComparison();

        // Feature 6 — sauvegarder dans l'historique
        if (_jsReady)
            await SaveHistoryAsync(_domainName);

        _loading = false;
        StateHasChanged();
    }

    // ── Feature 8 — comparaison ───────────────────────────────────────────────

    private void ComputeComparison()
    {
        var successfulServers = _servers.Where(s => s.Success == true).ToList();
        if (successfulServers.Count < 2) return;

        var groups = successfulServers.GroupBy(s => s.ResultFingerprint).ToList();
        if (groups.Count <= 1) return; // tous identiques

        // Le groupe majoritaire définit la référence
        var majorityKey = groups.OrderByDescending(g => g.Count()).First().Key;

        foreach (var server in successfulServers)
        {
            if (server.ResultFingerprint != majorityKey)
                server.IsDifferent = true;
        }
    }

    // ── Feature 7 — copie des résultats ──────────────────────────────────────

    private async Task CopyServerResults(DnsServerAndResult dnsServer)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {dnsServer.DnsServerInfo.DisplayName}");
        sb.AppendLine($"# Query: {_domainName} ({_selectedQueryType})");

        if (dnsServer.DnsResult != null)
        {
            sb.AppendLine($"# Duration: {dnsServer.DnsResult.Duration.TotalMilliseconds:F0}ms");
            sb.AppendLine();
            foreach (var record in dnsServer.DnsResult.Result.Answers)
                sb.AppendLine(record.ToString());
        }
        else
        {
            sb.AppendLine($"# Error: {dnsServer.ErrorMessage}");
        }

        try
        {
            await JS.InvokeVoidAsync("dnsLookup.copyToClipboard", sb.ToString());
        }
        catch { }
    }

    // ── Feature 9 — ajout de serveur dynamique ───────────────────────────────

    private void AddServer()
    {
        var input = _addServerInput.Trim();
        if (string.IsNullOrWhiteSpace(input)) return;

        // Essayer en tant que keyword ou "name=IP"
        var servers = DnsServerInfo.Load(input);

        // Sinon, interpréter comme une adresse IP bare
        if (servers.Count == 0 && System.Net.IPAddress.TryParse(input, out _))
            servers = [new DnsServerInfo { Name = input, ServerIpAddress = input }];

        foreach (var srv in servers)
        {
            if (!_servers.Any(s => s.DnsServerInfo.ServerIpAddress == srv.ServerIpAddress))
            {
                _servers.Add(new DnsServerAndResult(srv));
                _pageDnsConfiguration.Servers.Add(srv);
            }
        }

        _addServerInput = "";
        _showAddServer = false;
        StateHasChanged();
    }

    // ── Audit trail ───────────────────────────────────────────────────────────

    private async Task ShowAuditTrail(DnsServerAndResult dnsServer)
    {
        if (dnsServer?.DnsResult?.Result == null) return;

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

        await dialog.Result;
    }
}