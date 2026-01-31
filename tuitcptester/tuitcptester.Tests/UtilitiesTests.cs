using System.Net;
using System.Net.Sockets;
using tuitcptester.Logic;

namespace tuitcptester.Tests;

public class UtilitiesTests
{
    [Fact]
    public void ToHexString_FormatsRequestedSlice()
    {
        var bytes = new byte[] { 0x00, 0xAB, 0xCD, 0xEF };

        var result = DataUtils.ToHexString(bytes, 1, 2);

        Assert.Equal("ab cd", result);
    }

    [Fact]
    public void HexToBytes_ParsesWhitespaceAndSeparators()
    {
        var bytes = DataUtils.HexToBytes("AA-BB cc\nDD");

        Assert.Equal(new byte[] { 0xAA, 0xBB, 0xCC, 0xDD }, bytes);
    }

    [Fact]
    public void HexToBytes_OddLength_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => DataUtils.HexToBytes("ABC"));
    }

    [Fact]
    public void ToHexDump_ContainsOffsetHexAndAsciiSections()
    {
        var data = new byte[] { 0x41, 0x42, 0x00, 0x7F };

        var dump = DataUtils.ToHexDump(data, 0, data.Length);

        Assert.Contains("00000000", dump);
        Assert.Contains("41 42 00 7f", dump);
        Assert.Contains("|AB..|", dump);
    }

    [Fact]
    public async Task ResolveHostAsync_InvalidHost_ReturnsEmptyList()
    {
        var result = await DnsHelper.ResolveHostAsync("nonexistent-hostname-for-test.invalid");

        Assert.Empty(result);
    }

    [Fact]
    public async Task ReverseLookupAsync_InvalidAddress_ReturnsNull()
    {
        var result = await DnsHelper.ReverseLookupAsync("not-an-ip-address");

        Assert.Null(result);
    }

    [Fact]
    public async Task PacketGenerator_InvalidHex_LogsErrorAndReturns()
    {
        var logs = new List<string>();

        await PacketGenerator.RunAsync("127.0.0.1", 1, "XYZ", 1, 0, logs.Add);

        Assert.Contains(logs, l => l.Contains("Invalid hex data", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GetPortDescription_KnownAndUnknown()
    {
        Assert.Equal("HTTP", PortScanner.GetPortDescription(80));
        Assert.Equal("Unknown Service", PortScanner.GetPortDescription(65530));
    }

    [Fact]
    public async Task ScanPortAsync_OpenPort_ReturnsTrue()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        try
        {
            var acceptedTask = listener.AcceptTcpClientAsync();
            var isOpen = await PortScanner.ScanPortAsync("127.0.0.1", port, 500);
            using var accepted = await acceptedTask;
            Assert.True(isOpen);
        }
        finally
        {
            listener.Stop();
        }
    }

    [Fact]
    public async Task ScanRangeAsync_ReturnsOrderedResults_AndInvokesProgress()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var openPort = ((IPEndPoint)listener.LocalEndpoint).Port;

        var closedPort = openPort + 1;
        var progress = new List<int>();

        try
        {
            using var _ = listener.AcceptTcpClientAsync();
            var results = await PortScanner.ScanRangeAsync("127.0.0.1", openPort, closedPort, 200, p => progress.Add(p));

            Assert.Equal(2, results.Count);
            Assert.True(results[0].Port < results[1].Port);
            Assert.Equal(new[] { openPort, closedPort }, progress.OrderBy(x => x).ToArray());
            Assert.Contains(results, r => r is { Port: var p, IsOpen: true } && p == openPort);
        }
        finally
        {
            listener.Stop();
        }
    }
}
