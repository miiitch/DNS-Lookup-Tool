# DNS Lookup tool
DNS Lookup Diagnostic tool embedded in a container

![Screenshot](doc/images/screencap.png)

## Description

This tool is a web application that allows executing DNS queries inside a container to test DNS resolution as a client workload.

Key features:
- **Multi-server parallel queries** — all configured servers are queried simultaneously; each result appears as soon as it arrives
- **Structured responses** — DNS records displayed in a readable table (Name / TTL / Type / Value) for all common types (A, AAAA, MX, CNAME, TXT, SOA, SRV, CAA…)
- **Visual status per server** — spinner while querying, green ✓ on success, red ✗ on error, response time badge
- **Cross-server comparison** — servers returning different records are automatically highlighted in orange
- **Search history** — last 10 queries saved in the browser's localStorage, shown as quick-access chips
- **Copy to clipboard** — one-click copy of any server's result as plain text
- **Dynamic server management** — add a server directly from the UI without restarting
- **Shareable URLs** — encode servers, record type, domain and settings in the URL for bookmarks or sharing

---

## Local development with Aspire

The solution includes an Aspire AppHost that orchestrates the DNS Lookup app and forwards telemetry.

Run locally from the repository root:

```powershell
dotnet restore .\src\mpelabs.dnslookup.sln
dotnet run --project .\src\mpelabs.dnslookup.AppHost
```

## OpenTelemetry logs

The web app uses Aspire ServiceDefaults and exports OpenTelemetry data when an OTLP endpoint is configured.

- With Aspire AppHost, the endpoint is configured automatically for local runs.
- Outside Aspire, define `OTEL_EXPORTER_OTLP_ENDPOINT` (example: `http://localhost:4317`).

---

## Configuration

### How to install

The application is distributed as a container image. To run the application, you need Docker or any compatible container runtime.

The image is available on Docker Hub: https://hub.docker.com/r/miiitch/mpelabs.dnslookup.

```bash
docker run -p 8080:8080 \
  -e DNS_SERVERS=google;cloudflare \
  miiitch/mpelabs.dnslookup
```

### Image configuration

The application is a .NET Blazor Server app. You can configure the listening port via `ASPNETCORE_HTTP_PORTS`.

Documentation: https://learn.microsoft.com/en-us/dotnet/core/containers/publish-configuration#containerport

### DNS Servers

By default, no DNS servers are configured. You can add them in three ways:

**1 — Environment variable `DNS_SERVERS`** (semicolon-separated, applied at startup):
```
DNS_SERVERS=google;cloudflare;MyServer=10.0.0.53
```

**2 — URL query string `server`** (overrides / extends env at page load, repeatable):
```
https://your-tool/?server=google&server=cloudflare&server=MyServer=10.0.0.53
```

**3 — "Add server" button in the UI** (adds a server for the current session without reloading):
- Type a keyword (`google`, `cloudflare`, `azure`, `opendns`, `default`)
- Or a bare IP address (`10.0.0.53`)
- Or a custom entry (`MyServer=10.0.0.53`)

#### Pre-configured server sets

| Keyword | Servers |
|---------|---------|
| `google` | `8.8.8.8` (Primary) & `8.8.4.4` (Secondary) |
| `cloudflare` | `1.1.1.1` (Primary) & `1.0.0.1` (Secondary) |
| `opendns` | `208.67.222.222` & `208.67.220.220` *(may be blocked in some countries)* |
| `azure` | `168.63.129.16` — Azure's internal resolver |
| `default` | The host's default DNS server |

Custom servers use the syntax `Name=IP`:
```
DNS_SERVERS=google;MyCorpDNS=10.0.0.53
```

### Query timeout and retries

Configurable via environment variables or URL query parameters. **URL parameters override environment variables**.

| Parameter | Environment variable | URL query parameter | Default |
|-----------|---------------------|---------------------|---------|
| Timeout   | `DNS_TIMEOUT_MS`    | `timeout`           | `2000` ms |
| Retries   | `DNS_RETRIES`       | `retries`           | `1`     |

> ⚠️ The timeout applies **per attempt**. Total max wait = `timeout × (retries + 1)`.  
> With defaults: `2000 × 2 = 4 000 ms` maximum per server.

```bash
# Example: aggressive settings for fast networks
docker run -e DNS_SERVERS=google -e DNS_TIMEOUT_MS=500 -e DNS_RETRIES=0 ...

# Example: lenient settings for slow/distant servers
docker run -e DNS_SERVERS=google -e DNS_TIMEOUT_MS=5000 -e DNS_RETRIES=3 ...
```

### URL parameters reference

All parameters can be combined freely:

| Parameter | Description | Example |
|-----------|-------------|---------|
| `server`  | Add a DNS server (repeatable) | `server=google&server=cloudflare` |
| `timeout` | Timeout in ms (overrides `DNS_TIMEOUT_MS`) | `timeout=3000` |
| `retries` | Retry count (overrides `DNS_RETRIES`) | `retries=0` |
| `q`       | Pre-fill the domain field and **auto-run the query** on page load | `q=google.com` |
| `type`    | DNS record type for the auto-run query (default: `ANY`) | `type=MX` |

**Example — shareable query URL:**
```
https://your-tool/?q=example.com&type=MX&server=google&server=cloudflare&timeout=3000
```
Opening this URL will immediately query `example.com MX` against Google and Cloudflare with a 3-second timeout.

### Query behaviour

- All configured servers are queried **in parallel**.
- Each server card updates **independently** as soon as its response arrives.
- After all queries complete, results are **compared**: if a server returns different records from the majority, its card is highlighted in orange and a warning is shown.
- Each query is saved to the **browser history** (up to 10 entries), accessible as chips below the search bar.

### Customization

You can add a supplemental title to the page by setting the environment variable `CUSTOMIZATION_PAGE_TITLE`:

```bash
docker run -e CUSTOMIZATION_PAGE_TITLE="Production - West EU" ...
```
