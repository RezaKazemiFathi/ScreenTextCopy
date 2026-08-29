using System.Net;
using System.Net.Http;
using ScreenTextCopy.Models;

namespace ScreenTextCopy.Services;

/// <summary>
/// An <see cref="IWebProxy"/> that reads the current <see cref="AppSettings"/> on
/// every request, so proxy changes take effect immediately (no HttpClient
/// rebuild / app restart). This is what lets users in restricted regions route
/// AI traffic through a local VPN/proxy — e.g. <c>socks5://127.0.0.1:10808</c> —
/// or force a direct connection when a stale system proxy is refusing requests
/// (the "target machine actively refused it" error).
/// </summary>
public sealed class SettingsWebProxy : IWebProxy
{
    private readonly SettingsService _settings;

    public SettingsWebProxy(SettingsService settings) => _settings = settings;

    /// <summary>Credentials are not used; auth-in-URL proxies carry their own.</summary>
    public ICredentials? Credentials { get; set; }

    public Uri? GetProxy(Uri destination)
    {
        AppSettings s = _settings.Current;
        return s.ProxyMode switch
        {
            NetworkProxyMode.None => null, // Direct connection.
            NetworkProxyMode.Manual => TryParseProxy(s.ProxyAddress),
            _ => HttpClient.DefaultProxy?.GetProxy(destination) // System proxy.
        };
    }

    public bool IsBypassed(Uri host)
    {
        AppSettings s = _settings.Current;
        return s.ProxyMode switch
        {
            // No proxy => every host is "bypassed" (connected to directly).
            NetworkProxyMode.None => true,
            // Manual: use the proxy for everything as long as it parses; if the
            // address is invalid, bypass so requests still go out directly.
            NetworkProxyMode.Manual => TryParseProxy(s.ProxyAddress) is null,
            _ => HttpClient.DefaultProxy?.IsBypassed(host) ?? true
        };
    }

    /// <summary>
    /// Parses a user-entered proxy address into a URI. Accepts a bare
    /// "host:port" (assumed http) as well as explicit http/https/socks4/socks5
    /// schemes. Returns null when empty or malformed.
    /// </summary>
    private static Uri? TryParseProxy(string? address)
    {
        if (string.IsNullOrWhiteSpace(address))
            return null;

        string value = address.Trim();
        if (!value.Contains("://", StringComparison.Ordinal))
            value = "http://" + value; // Bare "127.0.0.1:10809" => http proxy.

        return Uri.TryCreate(value, UriKind.Absolute, out Uri? uri) &&
               uri.Scheme is "http" or "https" or "socks4" or "socks4a" or "socks5"
            ? uri
            : null;
    }
}
