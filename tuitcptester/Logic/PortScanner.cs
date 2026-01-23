using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Threading;

namespace tuitcptester.Logic;

/// <summary>
/// Provides utility methods for scanning TCP ports.
/// </summary>
public class PortScanner
{
    /// <summary>
    /// Represents the result of a single port scan operation.
    /// </summary>
    /// <param name="Port">The port number that was scanned.</param>
    /// <param name="IsOpen">True if the port was found to be open; otherwise, false.</param>
    public record ScanResult(int Port, bool IsOpen);

    /// <summary>
    /// A dictionary mapping common TCP port numbers to their standard service descriptions.
    /// </summary>
    private static readonly Dictionary<int, string> CommonPorts = new()
    {
        { 20, "FTP (Data)" },
        { 21, "FTP (Control)" },
        { 22, "SSH" },
        { 23, "Telnet" },
        { 25, "SMTP" },
        { 53, "DNS" },
        { 67, "DHCP (Server)" },
        { 68, "DHCP (Client)" },
        { 69, "TFTP" },
        { 80, "HTTP" },
        { 110, "POP3" },
        { 123, "NTP" },
        { 137, "NetBIOS Name Service" },
        { 138, "NetBIOS Datagram Service" },
        { 139, "NetBIOS Session Service" },
        { 143, "IMAP" },
        { 161, "SNMP" },
        { 179, "BGP" },
        { 389, "LDAP" },
        { 443, "HTTPS" },
        { 445, "Microsoft-DS (SMB)" },
        { 465, "SMTPS" },
        { 514, "Syslog" },
        { 515, "LPD" },
        { 587, "SMTP (Submission)" },
        { 631, "IPP (CUPS)" },
        { 636, "LDAPS" },
        { 873, "Rsync" },
        { 993, "IMAPS" },
        { 995, "POP3S" },
        { 1080, "SOCKS Proxy" },
        { 1433, "MS SQL" },
        { 1521, "Oracle DB" },
        { 2049, "NFS" },
        { 3000, "Gitea / Node.js" },
        { 3306, "MySQL" },
        { 3389, "RDP" },
        { 5000, "Flask / Docker Registry" },
        { 5432, "PostgreSQL" },
        { 5900, "VNC" },
        { 6379, "Redis" },
        { 8000, "HTTP Alt" },
        { 8080, "HTTP Proxy / Tomcat" },
        { 8443, "HTTPS Alt" },
        { 9000, "Portainer / PHP-FPM" },
        { 9090, "Prometheus / Cockpit" },
        { 9200, "Elasticsearch" },
        { 27017, "MongoDB" }
    };

    /// <summary>
    /// Gets a human-readable description for a given port number, if known.
    /// </summary>
    /// <param name="port">The port number to look up.</param>
    /// <returns>A string describing the common service on that port, or "Unknown Service".</returns>
    public static string GetPortDescription(int port)
    {
        return CommonPorts.TryGetValue(port, out var description) ? description : "Unknown Service";
    }

    /// <summary>
    /// Asynchronously scans a range of ports on a specified host.
    /// </summary>
    /// <param name="host">The host address to scan.</param>
    /// <param name="startPort">The starting port of the range.</param>
    /// <param name="endPort">The ending port of the range.</param>
    /// <param name="timeoutMs">The timeout in milliseconds for each port scan attempt. Default is 200ms.</param>
    /// <param name="onProgress">Optional callback invoked for each scanned port with the port number.</param>
    /// <returns>A list of <see cref="ScanResult"/> objects for the scanned range.</returns>
    public static async Task<List<ScanResult>> ScanRangeAsync(string host, int startPort, int endPort, int timeoutMs = 200, Action<int>? onProgress = null)
    {
        var results = new ConcurrentBag<ScanResult>();
        var ports = Enumerable.Range(startPort, endPort - startPort + 1);

        using var semaphore = new SemaphoreSlim(100);
        
        var tasks = ports.Select(async port =>
        {
            await semaphore.WaitAsync();
            try
            {
                bool isOpen = await ScanPortAsync(host, port, timeoutMs);
                results.Add(new ScanResult(port, isOpen));
                onProgress?.Invoke(port);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        return results.OrderBy(r => r.Port).ToList();
    }

    /// <summary>
    /// Asynchronously attempts to connect to a single TCP port to determine if it is open.
    /// </summary>
    /// <param name="host">The host address to scan.</param>
    /// <param name="port">The port number to scan.</param>
    /// <param name="timeoutMs">The timeout in milliseconds for the connection attempt. Default is 200ms.</param>
    /// <returns>True if the connection was successful; otherwise, false.</returns>
    public static async Task<bool> ScanPortAsync(string host, int port, int timeoutMs = 200)
    {
        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(host, port);
            var delayTask = Task.Delay(timeoutMs);

            var completedTask = await Task.WhenAny(connectTask, delayTask);
            if (completedTask == connectTask && client.Connected)
            {
                return true;
            }
        }
        catch
        {
            // Ignore errors, assume port is closed
        }
        return false;
    }
}
