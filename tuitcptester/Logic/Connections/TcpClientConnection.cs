using System.Net.Sockets;
using tuitcptester.Models;

namespace tuitcptester.Logic;

/// <summary>
/// Manages a TCP client connection.
/// </summary>
public class TcpClientConnection : TcpConnectionBase
{
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(5);
    private readonly string _host;
    private readonly int _port;
    private TcpClient? _client;
    private CancellationTokenSource? _cts;
    private readonly Action<byte[], int> _onDataReceived;

    /// <summary>
    /// Initializes a new instance of the <see cref="TcpClientConnection"/> class.
    /// </summary>
    /// <param name="host">The host to connect to.</param>
    /// <param name="port">The port to connect to.</param>
    /// <param name="onDataReceived">Callback for received data.</param>
    public TcpClientConnection(string host, int port, Action<byte[], int> onDataReceived)
    {
        _host = host;
        _port = port;
        _onDataReceived = onDataReceived;
    }

    /// <inheritdoc/>
    public override void Start()
    {
        _cts = new CancellationTokenSource();
        try
        {
            _client = new TcpClient();
            using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
            connectCts.CancelAfter(ConnectTimeout);
            _client.ConnectAsync(_host, _port, connectCts.Token).GetAwaiter().GetResult();
            Status = ConnectionStatus.Connected;
            Log("Connected.");

            _ = Task.Run(() => HandleIncomingDataAsync(_client.GetStream(), _cts.Token, _onDataReceived), _cts.Token);
        }
        catch (OperationCanceledException) when (_cts is { IsCancellationRequested: false })
        {
            Status = ConnectionStatus.Error;
            var message = $"Connect timed out after {ConnectTimeout.TotalSeconds:0} seconds.";
            Error(message);
            Log(message);
            throw new TimeoutException(message);
        }
        catch (Exception ex)
        {
            Status = ConnectionStatus.Error;
            Error(ex.Message);
            Log($"Failed to connect: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public override void Stop()
    {
        _cts?.Cancel();
        _client?.Close();
        Status = ConnectionStatus.Disconnected;
        Log("Disconnected.");
    }

    /// <inheritdoc/>
    public override void Send(Transaction tx)
    {
        if (_client is { Connected: true })
        {
            SendInternal(tx, _client.GetStream());
        }
        else
        {
            Log("Cannot send: Not connected.");
        }
    }

    /// <inheritdoc/>
    public override void Dispose()
    {
        Stop();
        _cts?.Dispose();
        _client?.Dispose();
    }
}
