# mpelabs.dnslookup
DNS Lookup Diagnostic tool embedded in a container

![Screenshot](doc\images\screencap.png)

## Description
This tool is a web application that allows executing DNS queries inside a container to test DNS resolution as a client workload.

## Configuration

### How to install

The application is distributed as a container image. To run the application, you need to have Docker installed on your system or use a container runtime that supports Docker images.
the image is available on Docker Hub here: https://hub.docker.com/r/miiitch/mpelabs.dnslookup.

### Image configuration

The application in a .net blazor app. you can configure the server to run on a specific port by setting the environment variable `ASPNETCORE_HTTP_PORTS` to the desired port.

The documentation is here: https://learn.microsoft.com/en-us/dotnet/core/containers/publish-configuration#containerport

### DNS Servers

By default, no DNS Servers are configured. You must set a list of server from the environment variable `DNS_SERVERS`. The list must be separated by `;`.

A server is either a preconfigured set of servers or a custom server. The pre-configured servers are:
- `google`: `8.8.8.8` & `8.8.4.4`
- `cloudflare`: `1.1.1.1` & `1.0.0.1`
- `opendns` `208.67.222.222` & `208.67.220.220` (may not work in some countries)
- `azure`: `168.63.129.16`, the default internal DNS server of Azure
- `default`: the default DNS server of the host

Custom servers are defined by the IP address of the server:
- MYSERVER=1.2.4.4

Exemple of configuration:
```
DNS_SERVERS=google;MYSERVER=4.2.5.5
```


