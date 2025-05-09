﻿using System.Diagnostics;
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
        var parts = str.Split('=', StringSplitOptions.RemoveEmptyEntries);

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
                            Name = "Google Primary",
                            ServerIpAddress = "8.8.4.4"
                        },
                        new DnsServerInfo()
                        {
                            Name = "Google Secondary",
                            ServerIpAddress = "8.8.8.8"
                        },
                    ];
                case "cloudflare":
                    return
                    [
                        new DnsServerInfo()
                        {
                            Name = "Cloudflare Primary",
                            ServerIpAddress = "1.1.1.1"
                        },
                        new DnsServerInfo()
                        {
                            Name = "Cloudflare Secondary",
                            ServerIpAddress = "1.0.0.1"
                        },
                    ];
                case "opendns": 
                    return
                    [
                        new DnsServerInfo()
                        {
                            Name = "OpenDNS Primary",
                            ServerIpAddress = "208.67.222.222"
                        },
                        new DnsServerInfo()
                        {
                            Name = "OpenDNS Secondary",
                            ServerIpAddress = "208.67.220.220"
                        },
                    ];
                case "default":
                    return [ new DnsServerInfo()
                    {
                        Name = "Default Server",
                        ServerIpAddress = ""
                    },];
                default:
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