using tuitcptester.Logic;
using tuitcptester.Models;

namespace tuitcptester.Tests;

public class TcpInstanceTests
{
    [Fact]
    public void ToString_ReturnsConfigurationName()
    {
        var instance = new TcpInstance(new TcpConfiguration { Name = "Conn1", Type = ConnectionType.Server, Port = 0 });

        Assert.Equal("Conn1", instance.ToString());
    }

    [Fact]
    public void SendManual_WhenDisconnected_LogsCannotSendMessage()
    {
        var instance = new TcpInstance(new TcpConfiguration { Name = "Conn1", Type = ConnectionType.Server, Port = 0 });
        var logs = new List<LogEntry>();
        instance.OnLog += logs.Add;

        instance.SendManual(new Transaction { Data = "abc" });

        Assert.Contains(logs, l => l.Message.Contains("Cannot send: Not connected."));
    }

    [Fact]
    public void Start_ProxyWithoutRemote_ThrowsAndSetsLastError()
    {
        var config = new TcpConfiguration
        {
            Name = "BrokenProxy",
            Type = ConnectionType.Proxy,
            Host = "127.0.0.1",
            Port = 9000
        };
        var instance = new TcpInstance(config);

        var ex = Assert.Throws<InvalidOperationException>(() => instance.Start());

        Assert.Contains("Proxy requires RemoteHost and RemotePort", ex.Message);
        Assert.Equal(ex.Message, instance.LastError);
    }

    [Fact]
    public void Start_ProxyWithoutRemote_RaisesStatusChanged()
    {
        var config = new TcpConfiguration
        {
            Name = "BrokenProxy",
            Type = ConnectionType.Proxy,
            Host = "127.0.0.1",
            Port = 9001
        };
        var instance = new TcpInstance(config);
        var statusChanges = 0;
        instance.OnStatusChanged += () => statusChanges++;

        Assert.Throws<InvalidOperationException>(() => instance.Start());

        Assert.Equal(1, statusChanges);
    }

    [Fact]
    public void UpdateAutoTransactions_UpdatesConfigurationAndResetsIndex()
    {
        var instance = new TcpInstance(new TcpConfiguration { Name = "Conn1", Type = ConnectionType.Server, Port = 0 });
        var transactions = new List<Transaction>
        {
            new() { Data = "A", Encoding = TransactionEncoding.Ascii },
            new() { Data = "B", Encoding = TransactionEncoding.Hex }
        };

        instance.UpdateAutoTransactions(transactions, 1000, 10, 20);

        Assert.Equal(2, instance.Config.AutoTransactions.Count);
        Assert.Equal(1000, instance.Config.IntervalMs);
        Assert.Equal(10, instance.Config.JitterMinMs);
        Assert.Equal(20, instance.Config.JitterMaxMs);
        Assert.Equal(0, instance.AutoTxIndex);
    }
}
