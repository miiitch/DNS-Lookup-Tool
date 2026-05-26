using DnsClient;

namespace mpelabs.dnslookup;

public class DnsServerAndResult(DnsServerInfo dnsServerInfo)
{
    public DnsServerInfo DnsServerInfo { get; } = dnsServerInfo;
    public DnsResult? DnsResult { get; set; } = null;
    public bool? Success { get; set; } = null;
    public bool IsQuerying { get; set; } = false;
    public bool IsDifferent { get; set; } = false;
    public string? ErrorMessage { get; set; } = null;

    /// <summary>Empreinte normalisée des réponses pour comparaison entre serveurs.</summary>
    public string? ResultFingerprint =>
        DnsResult == null
            ? Success == false ? $"ERROR:{ErrorMessage}" : null
            : string.Join("|", DnsResult.Result.Answers
                .Select(a => a.ToString() ?? "")
                .OrderBy(s => s));
}

public class DnsResult(IDnsQueryResponse result, TimeSpan duration)
{
    public IDnsQueryResponse Result { get; } = result;
    public TimeSpan Duration { get; } = duration;
}