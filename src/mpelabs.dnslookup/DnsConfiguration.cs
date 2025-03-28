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
        var serverStr = str.Split(';', StringSplitOptions.RemoveEmptyEntries);

        return new DnsConfiguration()
        {
            Servers = serverStr.SelectMany(DnsServerInfo.Load).ToList()
        };

    }
}