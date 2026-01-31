using tuitcptester.Models;

namespace tuitcptester.Logic;

public class TcpProxyConnection : TcpConnectionBase
{
    private readonly int _localPort;
    private readonly string _remoteHost;
    private readonly int _remotePort;
    private TcpProxy? _proxy;

    public TcpProxyConnection(int localPort, string remoteHost, int remotePort)
    {
        _localPort = localPort;
        _remoteHost = remoteHost;
        _remotePort = remotePort;
    }

    public override void Start()
    {
        _proxy = new TcpProxy(_localPort, _remoteHost, _remotePort);
        _proxy.OnLog += (msg) => Log(msg);
        _proxy.Start();
        Status = ConnectionStatus.Listening;
    }

    public override void Stop()
    {
        _proxy?.Stop();
        Status = ConnectionStatus.Disconnected;
    }

    public override void Send(Transaction tx)
    {
        Log("Manual send not supported in Proxy mode.");
    }

    public override void Dispose()
    {
        _proxy?.Dispose();
    }
}
