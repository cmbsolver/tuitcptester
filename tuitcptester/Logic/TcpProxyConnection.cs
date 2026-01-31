using tuitcptester.Models;

namespace tuitcptester.Logic;

/// <summary>
/// A TCP connection implementation that acts as a proxy, forwarding traffic between a local port and a remote host.
/// </summary>
public class TcpProxyConnection : TcpConnectionBase
{
    private readonly int _localPort;
    private readonly string _remoteHost;
    private readonly int _remotePort;
    private TcpProxy? _proxy;

    /// <summary>
    /// Initializes a new instance of the <see cref="TcpProxyConnection"/> class.
    /// </summary>
    /// <param name="localPort">The local port to listen on.</param>
    /// <param name="remoteHost">The remote host to forward to.</param>
    /// <param name="remotePort">The remote port to forward to.</param>
    public TcpProxyConnection(int localPort, string remoteHost, int remotePort)
    {
        _localPort = localPort;
        _remoteHost = remoteHost;
        _remotePort = remotePort;
    }

    /// <inheritdoc/>
    public override void Start()
    {
        _proxy = new TcpProxy(_localPort, _remoteHost, _remotePort);
        _proxy.OnLog += (msg) => Log(msg);
        _proxy.Start();
        Status = ConnectionStatus.Listening;
    }

    /// <inheritdoc/>
    public override void Stop()
    {
        _proxy?.Stop();
        Status = ConnectionStatus.Disconnected;
    }

    /// <inheritdoc/>
    public override void Send(Transaction tx)
    {
        Log("Manual send not supported in Proxy mode.");
    }

    /// <inheritdoc/>
    public override void Dispose()
    {
        _proxy?.Dispose();
    }
}
