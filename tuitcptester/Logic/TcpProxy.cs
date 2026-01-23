using System.Net;
using System.Net.Sockets;

namespace tuitcptester.Logic;

/// <summary>
/// Provides a TCP Proxy / Port Forwarder that listens on a local port and forwards traffic to a remote host.
/// </summary>
public class TcpProxy : IDisposable
{
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;
    private readonly string _remoteHost;
    private readonly int _remotePort;
    private readonly int _localPort;

    /// <summary>
    /// Event raised when a new log entry is available.
    /// </summary>
    public event Action<string>? OnLog;

    /// <summary>
    /// Gets the local port the proxy is listening on.
    /// </summary>
    public int LocalPort => _localPort;

    /// <summary>
    /// Gets the remote host the proxy is forwarding to.
    /// </summary>
    public string RemoteHost => _remoteHost;

    /// <summary>
    /// Gets the remote port the proxy is forwarding to.
    /// </summary>
    public int RemotePort => _remotePort;

    /// <summary>
    /// Initializes a new instance of the <see cref="TcpProxy"/> class.
    /// </summary>
    /// <param name="localPort">The local port to listen on.</param>
    /// <param name="remoteHost">The remote host to forward to.</param>
    /// <param name="remotePort">The remote port to forward to.</param>
    public TcpProxy(int localPort, string remoteHost, int remotePort)
    {
        _localPort = localPort;
        _remoteHost = remoteHost;
        _remotePort = remotePort;
    }

    /// <summary>
    /// Starts the proxy asynchronously.
    /// </summary>
    public void Start()
    {
        _cts = new CancellationTokenSource();
        _listener = new TcpListener(IPAddress.Any, _localPort);
        _listener.Start();

        Log($"Proxy started: Listening on {_localPort} -> {_remoteHost}:{_remotePort}");

        Task.Run(() => AcceptClientsAsync(_cts.Token));
    }

    private async Task AcceptClientsAsync(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                var client = await _listener!.AcceptTcpClientAsync(token);
                Log($"Accepted connection from {client.Client.RemoteEndPoint}");
                _ = HandleProxyAsync(client, token);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            Log($"Proxy Accept Error: {ex.Message}");
        }
    }

    private async Task HandleProxyAsync(TcpClient localClient, CancellationToken token)
    {
        using (localClient)
        using (var remoteClient = new TcpClient())
        {
            try
            {
                await remoteClient.ConnectAsync(_remoteHost, _remotePort, token);
                Log($"Connected to remote {_remoteHost}:{_remotePort} for client {localClient.Client.RemoteEndPoint}");

                using (var localStream = localClient.GetStream())
                using (var remoteStream = remoteClient.GetStream())
                {
                    var localToRemote = CopyStreamWithLoggingAsync(localStream, remoteStream, $"[{localClient.Client.RemoteEndPoint} -> Remote]", token);
                    var remoteToLocal = CopyStreamWithLoggingAsync(remoteStream, localStream, $"[Remote -> {localClient.Client.RemoteEndPoint}]", token);

                    await Task.WhenAny(localToRemote, remoteToLocal);
                }
            }
            catch (Exception ex)
            {
                Log($"Proxy Error ({localClient.Client.RemoteEndPoint}): {ex.Message}");
            }
            finally
            {
                Log($"Connection closed for {localClient.Client.RemoteEndPoint}");
            }
        }
    }

    private async Task CopyStreamWithLoggingAsync(NetworkStream source, NetworkStream destination, string prefix, CancellationToken token)
    {
        byte[] buffer = new byte[8192];
        int bytesRead;

        try
        {
            while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
            {
                await destination.WriteAsync(buffer, 0, bytesRead, token);
                string data = System.Text.Encoding.ASCII.GetString(buffer, 0, bytesRead).Replace("\r", "\\r").Replace("\n", "\\n");
                string hex = DataUtils.ToHexString(buffer, 0, bytesRead);
                Log($"{prefix} Forwarded {bytesRead} bytes: {data} (Hex: {hex})");
            }
        }
        catch (Exception)
        {
            // Stream closed or error
        }
    }

    private void Log(string message)
    {
        OnLog?.Invoke(message);
    }

    /// <summary>
    /// Stops the proxy and releases resources.
    /// </summary>
    public void Stop()
    {
        _cts?.Cancel();
        _listener?.Stop();
        Log("Proxy stopped.");
    }

    /// <summary>
    /// Disposes the proxy.
    /// </summary>
    public void Dispose()
    {
        Stop();
        _cts?.Dispose();
    }
}
