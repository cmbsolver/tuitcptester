using System.Net;

namespace tuitcptester.Logic;

/// <summary>
/// Provides utility methods for DNS operations.
/// </summary>
public static class DnsHelper
{
    /// <summary>
    /// Asynchronously resolves a hostname to its IP addresses.
    /// </summary>
    /// <param name="hostName">The hostname to resolve.</param>
    /// <returns>A list of IP addresses associated with the hostname.</returns>
    public static async Task<List<IPAddress>> ResolveHostAsync(string hostName)
    {
        try
        {
            var addresses = await Dns.GetHostAddressesAsync(hostName);
            return addresses.ToList();
        }
        catch
        {
            return new List<IPAddress>();
        }
    }

    /// <summary>
    /// Asynchronously performs a reverse DNS lookup for an IP address.
    /// </summary>
    /// <param name="address">The IP address to look up.</param>
    /// <returns>The hostname associated with the IP address, or null if not found.</returns>
    public static async Task<string?> ReverseLookupAsync(string address)
    {
        try
        {
            var entry = await Dns.GetHostEntryAsync(address);
            return entry.HostName;
        }
        catch
        {
            return null;
        }
    }
}
