namespace mpelabs.dnslookup;

public class DnsConfiguration
{
    public List<DnsServerInfo> Servers { get; set; }

    public DnsConfiguration()
    {
        Servers = new List<DnsServerInfo>();
    }
    
    

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
            Servers = newServers
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