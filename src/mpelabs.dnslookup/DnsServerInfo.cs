using System.Diagnostics;
using System.Net;
using DnsClient;
using mpelabs.dnslookup.Components;
using ErrorOr;

namespace mpelabs.dnslookup;

public class DnsServerInfo
{
    public string Name { get; set; }

    public string ServerIpAddress { get; set; }
    
    public string DisplayName => $"{Name}{(string.IsNullOrWhiteSpace(ServerIpAddress)
        ? ""
        : $" - {ServerIpAddress}")}";

    public async Task<ErrorOr<DnsResult>> Query(string domainName, QueryType queryType)
    {
        LookupClientOptions options;

        if (string.IsNullOrWhiteSpace(ServerIpAddress))
        {
            options = new LookupClientOptions()
            {
                UseCache = false,
                Recursion = true,
                UseTcpFallback = false,
                UseTcpOnly = false,
                Retries = 5,
                Timeout = TimeSpan.FromSeconds(5),
                EnableAuditTrail = true,
            };
        }
        else
        {
            options = new LookupClientOptions(new NameServer(IPAddress.Parse(ServerIpAddress)))
            {
                UseCache = false,
                Recursion = true,
                UseTcpFallback = false,
                UseTcpOnly = false,
                Retries = 5,
                Timeout = TimeSpan.FromSeconds(5),
                EnableAuditTrail = true
            };
        }
        var client = new LookupClient(options);
        var sw = new Stopwatch();
        var now = DateTimeOffset.UtcNow;
        try
        {
            sw.Start();
            var dnsQueryResponse = await client.QueryAsync(domainName, queryType);
            sw.Stop();
            return new DnsResult(dnsQueryResponse, sw.Elapsed);
        }
        catch (DnsResponseException dnsException)
        {
            return Error.Unexpected(dnsException.DnsError);
        }
        catch (Exception _)
        {
            return Error.Unexpected(_.Message);
        }
    }
}

public class DnsConfiguration
{
    public List<DnsServerInfo> Servers { get; set; }

    public DnsConfiguration()
    {
        Servers = new List<DnsServerInfo>();
    }
}