using DnsClient;

namespace mpelabs.dnslookup;

public class DnsServerAndResult(DnsServerInfo dnsServerInfo)
{
    public DnsServerInfo DnsServerInfo { get; } = dnsServerInfo;
    public DnsResult? DnsResult { get; set; } = null;
    public bool? Success { get; set; }=  null;
    
    public string? ErrorMessage { get; set; } = null;

 
}

public class DnsResult(IDnsQueryResponse result, TimeSpan duration)
{
    public IDnsQueryResponse Result { get; } = result;
    public TimeSpan Duration { get; } = duration;
}