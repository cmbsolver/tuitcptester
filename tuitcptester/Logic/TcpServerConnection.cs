using System.Net;
using System.Net.Sockets;
using tuitcptester.Models;

namespace tuitcptester.Logic;

/// <summary>
/// Manages a TCP server connection that listens for incoming client connections.
/// </summary>
public class TcpServerConnection : TcpConnectionBase
{
    private readonly int _port;
    private TcpListener? _listener;
    private TcpClient? _currentClient;
    private CancellationTokenSource? _cts;
    private readonly Action<byte[], int> _onDataReceived;

    /// <summary>
    /// Initializes a new instance of the <see cref="TcpServerConnection"/> class.
    /// </summary>
    /// <param name="port">The port to listen on.</param>
    /// <param name="onDataReceived">Callback for received data from clients.</param>
    public TcpServerConnection(int port, Action<byte[], int> onDataReceived)
    {
        _port = port;
        _onDataReceived = onDataReceived;
    }

    /// <inheritdoc/>
    public override void Start()
    {
        _cts = new CancellationTokenSource();
        try
        {
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
            Status = ConnectionStatus.Listening;
            Log($"Listening on port {_port}...");

            Task.Run(() => AcceptClientsAsync(_cts.Token), _cts.Token);
        }
        catch (Exception ex)
        {
            Status = ConnectionStatus.Error;
            Error(ex.Message);
            Log($"Failed to start server: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Asynchronously accepts incoming client connections.
    /// </summary>
    /// <param name="token">Cancellation token to stop accepting clients.</param>
    private async Task AcceptClientsAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                if (_listener?.Pending() == true)
                {
                    var client = await _listener.AcceptTcpClientAsync(token);
                    Log($"Accepted connection from {client.Client.RemoteEndPoint}");
                    
                    _currentClient?.Close();
                    _currentClient = client;
                    Status = ConnectionStatus.Connected;

                    Task.Run(() => HandleIncomingData(_currentClient.GetStream(), token, _onDataReceived), token);
                }
                else
                {
                    await Task.Delay(100, token);
                }
            }
            catch (Exception ex) when (!token.IsCancellationRequested)
            {
                Log($"Server accept error: {ex.Message}");
            }
        }
    }

    /// <inheritdoc/>
    public override void Stop()
    {
        _cts?.Cancel();
        _currentClient?.Close();
        _listener?.Stop();
        Status = ConnectionStatus.Disconnected;
        Log("Server stopped.");
    }

    /// <inheritdoc/>
    public override void Send(Transaction tx)
    {
        if (_currentClient is { Connected: true })
        {
            SendInternal(tx, _currentClient.GetStream());
        }
        else
        {
            Log("Cannot send: No client connected.");
        }
    }

    /// <inheritdoc/>
    public override void Dispose()
    {
        Stop();
        _cts?.Dispose();
        _currentClient?.Dispose();
    }
}
