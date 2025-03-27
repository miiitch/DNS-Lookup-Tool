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
    
    
    public static List<DnsServerInfo> Load(string str)
    {
        var parts = str.Split(':', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 1)
        {
            var srvName = parts[0];
            switch (srvName.ToLowerInvariant())
            {
                case "azure":
                    return
                    [
                        new DnsServerInfo()
                        {
                            Name = "Azure DNS",
                            ServerIpAddress = "168.63.129.16"
                        }
                    ];
                case "google":
                    return
                    [
                        new DnsServerInfo()
                        {
                            Name = "Google DNS 1",
                            ServerIpAddress = "8.8.4.4"
                        },
                        new DnsServerInfo()
                        {
                            Name = "Google DNS 2",
                            ServerIpAddress = "8.8.8.8"
                        },
                    ];
                case "default":
                    return [];
            }
        }

        if (parts.Length == 2)
        {
            return
            [
                new DnsServerInfo()
                {
                    Name = parts[0],
                    ServerIpAddress = parts[1]
                }
            ];
        }

        return [];
    }
}

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