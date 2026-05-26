namespace mpelabs.dnslookup;

public class DnsConfiguration
{
    public List<DnsServerInfo> Servers { get; set; } = new List<DnsServerInfo>();

    /// <summary>Timeout en millisecondes. null = utiliser la valeur par défaut (2000 ms).</summary>
    public int? TimeoutMs { get; set; } = null;

    /// <summary>Nombre de retries. null = utiliser la valeur par défaut (1).</summary>
    public int? Retries { get; set; } = null;

    public int EffectiveTimeoutMs => TimeoutMs ?? 2000;
    public int EffectiveRetries => Retries ?? 1;

    public static DnsConfiguration Load(string str)
    {
        try
        {
            var serverStr = str.Split(';', StringSplitOptions.RemoveEmptyEntries);
            return new DnsConfiguration()
            {
                Servers = serverStr.SelectMany(DnsServerInfo.Load).ToList()
            };
        }
        catch
        {
            return new DnsConfiguration();
        }
    }

    public DnsConfiguration Merge(DnsConfiguration other)
    {
        var newServers = Servers.ToList();
        foreach (var server in other.Servers)
        {
            var existing = newServers.Any(x => x.ServerIpAddress == server.ServerIpAddress);
            if (!existing)
            {
                newServers.Add(server);
            }
        }

        return new DnsConfiguration()
        {
            Servers = newServers,
            // URL (other) a la priorité si explicitement défini, sinon on garde this
            TimeoutMs = other.TimeoutMs ?? TimeoutMs,
            Retries = other.Retries ?? Retries,
        };
    }
}

public class Customization
{
    public string? PageTitle
    {
        get
        {
            var title = Environment.GetEnvironmentVariable("CUSTOMIZATION_PAGE_TITLE");
            return title;
        }
    }

}